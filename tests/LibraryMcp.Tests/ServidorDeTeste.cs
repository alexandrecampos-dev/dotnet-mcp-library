using System.IO.Pipelines;
using System.Text.Json;
using LibraryMcp.Core.Dados;
using LibraryMcp.Tools;
using LibraryMcp.Tools.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LibraryMcp.Tests;

/// <summary>
/// Sobe o servidor MCP de verdade (mesmas ferramentas, filtros e banco SQLite) e conecta um cliente MCP
/// por um par de pipes em memória. O que passa aqui passa pelo protocolo inteiro, como num cliente real.
/// </summary>
public sealed class ServidorDeTeste : IAsyncDisposable
{
    private readonly IHost _host;
    private readonly string _arquivoDoBanco;

    private ServidorDeTeste(IHost host, McpClient cliente, FakeTimeProvider relogio, string arquivoDoBanco)
    {
        _host = host;
        Cliente = cliente;
        Relogio = relogio;
        _arquivoDoBanco = arquivoDoBanco;
    }

    public McpClient Cliente { get; }
    public FakeTimeProvider Relogio { get; }

    /// <param name="login">Leitor conectado; null simula um servidor sem identidade configurada.</param>
    public static async Task<ServidorDeTeste> IniciarAsync(string? login)
    {
        var arquivo = Path.Combine(Path.GetTempPath(), $"biblioteca-teste-{Guid.NewGuid():N}.db");
        var conexao = $"Data Source={arquivo};Pooling=False";
        var relogio = new FakeTimeProvider(new DateTimeOffset(2026, 11, 2, 12, 0, 0, TimeSpan.Zero));

        // Prepara o banco e descobre o leitor, como o host stdio faz na inicialização.
        var preparo = new ServiceCollection().AddBiblioteca(conexao).BuildServiceProvider();
        await BancoDaBiblioteca.PrepararAsync(preparo);
        System.Security.Claims.ClaimsPrincipal? principal = null;
        if (login is not null)
        {
            await using var scope = preparo.CreateAsyncScope();
            var leitor = await scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>()
                .Leitores.AsNoTracking().SingleAsync(l => l.Login == login);
            principal = IdentidadeDaBiblioteca.CriarPrincipal(leitor, "Teste");
        }
        await preparo.DisposeAsync();

        Pipe clienteParaServidor = new(), servidorParaCliente = new();

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddBiblioteca(conexao);
        builder.Services.AddSingleton<TimeProvider>(relogio);
        builder.Services
            .AddMcpServer()
            .WithStreamServerTransport(clienteParaServidor.Reader.AsStream(), servidorParaCliente.Writer.AsStream())
            .WithIdentidadeDoProcesso(principal)
            .WithBibliotecaTools();

        var host = builder.Build();
        await host.StartAsync();

        var cliente = await McpClient.CreateAsync(
            new StreamClientTransport(clienteParaServidor.Writer.AsStream(), servidorParaCliente.Reader.AsStream()));

        return new ServidorDeTeste(host, cliente, relogio, arquivo);
    }

    public async Task<CallToolResult> ChamarAsync(string ferramenta, Dictionary<string, object?>? argumentos = null) =>
        await Cliente.CallToolAsync(ferramenta, argumentos ?? [], cancellationToken: TestContext.Current.CancellationToken);

    public static string Texto(CallToolResult resultado) =>
        string.Join("\n", resultado.Content.OfType<TextContentBlock>().Select(t => t.Text));

    public static JsonElement Json(CallToolResult resultado)
    {
        Assert.NotEqual(true, resultado.IsError);
        return JsonDocument.Parse(Texto(resultado)).RootElement;
    }

    public async ValueTask DisposeAsync()
    {
        await Cliente.DisposeAsync();
        await _host.StopAsync();
        _host.Dispose();
        try { File.Delete(_arquivoDoBanco); } catch (IOException) { }
    }
}

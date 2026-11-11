using System.Security.Claims;
using LibraryMcp.Core.Dados;
using LibraryMcp.Tools;
using LibraryMcp.Tools.Identidade;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Servidor MCP por stdio, o transporte que Claude Code e Claude Desktop usam para servidores locais.
// stdout é exclusivo do protocolo (JSON-RPC); todo log vai para stderr.

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

builder.Services.AddBiblioteca(builder.Configuration.GetConnectionString("Biblioteca"));

// Identidade: quem configura o servidor diz quem é o leitor (variável BIBLIOTECA_USUARIO).
// O papel (leitor ou bibliotecário) vem do cadastro, não da variável.
var principal = await ResolverLeitorAsync(builder);

builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "biblioteca", Version = "2.0.0" })
    .WithStdioServerTransport()
    .WithIdentidadeDoProcesso(principal)
    .WithBibliotecaTools();

await builder.Build().RunAsync();

static async Task<ClaimsPrincipal?> ResolverLeitorAsync(HostApplicationBuilder builder)
{
    using var provider = builder.Services.BuildServiceProvider();
    var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("LibraryMcp.Stdio");
    await BancoDaBiblioteca.PrepararAsync(provider);

    var login = builder.Configuration["BIBLIOTECA_USUARIO"]?.Trim().ToLowerInvariant();
    if (string.IsNullOrEmpty(login))
    {
        logger.LogWarning("BIBLIOTECA_USUARIO não definido: o servidor sobe, mas toda ferramenta vai recusar a chamada.");
        return null;
    }

    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
    var leitor = await db.Leitores.AsNoTracking().FirstOrDefaultAsync(l => l.Login == login);
    if (leitor is null)
    {
        logger.LogWarning("Leitor '{Login}' não cadastrado: toda ferramenta vai recusar a chamada.", login);
        return null;
    }

    logger.LogInformation("Conectado como {Nome} ({Papel}).", leitor.Nome, leitor.Papel);
    return IdentidadeDaBiblioteca.CriarPrincipal(leitor, IdentidadeDaBiblioteca.TipoDeAutenticacaoStdio);
}

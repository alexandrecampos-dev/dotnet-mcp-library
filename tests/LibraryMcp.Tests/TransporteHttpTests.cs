using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace LibraryMcp.Tests;

/// <summary>
/// Transporte HTTP de ponta a ponta, em memória: autenticação JWT na frente do endpoint MCP
/// e a identidade do token chegando à ferramenta.
/// </summary>
public sealed class TransporteHttpTests : IClassFixture<TransporteHttpTests.Servidor>
{
    private const string ChaveDeTeste = "chave-de-teste-com-mais-de-trinta-e-dois-caracteres-0123";
    private readonly Servidor _servidor;

    public TransporteHttpTests(Servidor servidor) => _servidor = servidor;

    public sealed class Servidor : WebApplicationFactory<Program>
    {
        private readonly string _banco = Path.Combine(Path.GetTempPath(), $"biblioteca-http-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Biblioteca", $"Data Source={_banco};Pooling=False");
            builder.UseSetting("Jwt:ChaveDeAssinatura", ChaveDeTeste);
        }

        public static string Token(string login) => TokenDeDesenvolvimento.Emitir(
            new OpcoesJwt { ChaveDeAssinatura = ChaveDeTeste }, login);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try { File.Delete(_banco); } catch (IOException) { }
        }
    }

    private static HttpRequestMessage Pedido(string json, string? token = null)
    {
        var pedido = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        pedido.Headers.Accept.ParseAdd("application/json");
        pedido.Headers.Accept.ParseAdd("text/event-stream");
        if (token is not null)
            pedido.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return pedido;
    }

    private const string ListarFerramentas = """{"jsonrpc":"2.0","id":1,"method":"tools/list"}""";

    [Fact]
    public async Task Sem_token_responde_401_e_encerra_a_resposta()
    {
        using var cliente = _servidor.CreateClient();

        using var resposta = await cliente.SendAsync(Pedido(ListarFerramentas), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
        Assert.Equal("Bearer", resposta.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Empty(await resposta.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Token_com_assinatura_errada_e_recusado()
    {
        using var cliente = _servidor.CreateClient();
        var forjado = TokenDeDesenvolvimento.Emitir(
            new OpcoesJwt { ChaveDeAssinatura = "outra-chave-qualquer-com-mais-de-32-caracteres!!" }, "carla");

        using var resposta = await cliente.SendAsync(Pedido(ListarFerramentas, forjado), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Token_de_login_inexistente_e_recusado()
    {
        using var cliente = _servidor.CreateClient();

        using var resposta = await cliente.SendAsync(Pedido(ListarFerramentas, Servidor.Token("intruso")), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    [Fact]
    public async Task Papel_vem_do_cadastro_e_nao_do_token()
    {
        using var cliente = _servidor.CreateClient();
        const string cadastrar = """
            {"jsonrpc":"2.0","id":2,"method":"tools/call",
             "params":{"name":"cadastrar_exemplares","arguments":{"isbn":"9780132350884","quantidade":1}}}
            """;

        using var deAna = await cliente.SendAsync(Pedido(cadastrar, Servidor.Token("ana")), TestContext.Current.CancellationToken);
        using var deCarla = await cliente.SendAsync(Pedido(cadastrar, Servidor.Token("carla")), TestContext.Current.CancellationToken);

        var textoAna = await deAna.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var textoCarla = await deCarla.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"isError\":true", textoAna, StringComparison.Ordinal);
        Assert.Contains("bibliotec", textoAna, StringComparison.Ordinal);
        Assert.DoesNotContain("\"isError\":true", textoCarla, StringComparison.Ordinal);
        Assert.Contains("exemplares", textoCarla, StringComparison.Ordinal);
    }
}

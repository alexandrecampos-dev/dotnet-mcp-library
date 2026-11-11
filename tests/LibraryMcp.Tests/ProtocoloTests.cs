using System.Text.Json;

namespace LibraryMcp.Tests;

/// <summary>O que o cliente MCP enxerga: nome, descrição, esquema e anotações de cada ferramenta.</summary>
public sealed class ProtocoloTests
{
    private static readonly string[] FerramentasEsperadas =
        ["buscar_livros", "cadastrar_exemplares", "consultar_meus_emprestimos", "devolver_livro", "emprestar_livro"];

    [Fact]
    public async Task Expoe_cinco_ferramentas_por_intencao()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var ferramentas = await servidor.Cliente.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(FerramentasEsperadas, ferramentas.Select(f => f.Name).Order());
        Assert.All(ferramentas, f => Assert.True(f.Description?.Length > 120,
            $"A descrição de {f.Name} é o prompt que o modelo lê; precisa dizer quando usar e quando não usar."));
    }

    [Fact]
    public async Task Nenhuma_ferramenta_aceita_identidade_vinda_do_modelo()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var ferramentas = await servidor.Cliente.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        foreach (var ferramenta in ferramentas)
        {
            var propriedades = ferramenta.JsonSchema.TryGetProperty("properties", out var p)
                ? p.EnumerateObject().Select(x => x.Name.ToLowerInvariant()).ToList()
                : [];
            Assert.DoesNotContain(propriedades, nome =>
                nome.Contains("usuario", StringComparison.Ordinal) ||
                nome.Contains("leitor", StringComparison.Ordinal) ||
                nome.EndsWith("id", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Emprestar_recebe_so_a_referencia_do_livro()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var ferramentas = await servidor.Cliente.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        var emprestar = ferramentas.Single(f => f.Name == "emprestar_livro");

        var propriedades = emprestar.JsonSchema.GetProperty("properties").EnumerateObject().Select(x => x.Name);
        Assert.Equal(["livro"], propriedades);
    }

    [Fact]
    public async Task Consultas_sao_anotadas_como_somente_leitura()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var ferramentas = (await servidor.Cliente.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken))
            .ToDictionary(f => f.Name);

        Assert.True(ferramentas["buscar_livros"].ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.True(ferramentas["consultar_meus_emprestimos"].ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.False(ferramentas["emprestar_livro"].ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.False(ferramentas["emprestar_livro"].ProtocolTool.Annotations?.DestructiveHint);
    }

    [Fact]
    public async Task Resposta_da_busca_e_enxuta()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var json = ServidorDeTeste.Json(await servidor.ChamarAsync("buscar_livros", new() { ["termo"] = "chip huyen" }));

        var livro = json.GetProperty("livros")[0];
        var campos = livro.EnumerateObject().Select(c => c.Name.ToLowerInvariant()).Order().ToList();
        Assert.Equal(["ano", "autor", "disponiveis", "exemplares", "isbn", "titulo"], campos);
        Assert.Equal(JsonValueKind.Number, json.GetProperty("total").ValueKind);
    }
}

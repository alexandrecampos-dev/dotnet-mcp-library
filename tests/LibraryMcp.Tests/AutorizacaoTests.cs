namespace LibraryMcp.Tests;

/// <summary>Autorização acontece no servidor, com a identidade da conexão.</summary>
public sealed class AutorizacaoTests
{
    [Fact]
    public async Task Sem_identidade_toda_ferramenta_recusa()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync(login: null);

        var resultado = await servidor.ChamarAsync("buscar_livros", new() { ["termo"] = "codigo" });

        Assert.True(resultado.IsError);
        Assert.Contains("Acesso negado", ServidorDeTeste.Texto(resultado), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Leitor_nao_cadastra_exemplares()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var resultado = await servidor.ChamarAsync("cadastrar_exemplares",
            new() { ["isbn"] = "9780132350884", ["quantidade"] = 5 });

        Assert.True(resultado.IsError);
        Assert.Contains("só bibliotecários", ServidorDeTeste.Texto(resultado), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Bibliotecaria_cadastra_exemplares()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("carla");

        var json = ServidorDeTeste.Json(await servidor.ChamarAsync("cadastrar_exemplares",
            new() { ["isbn"] = "978-0-13-235088-4", ["quantidade"] = 2 }));

        Assert.Equal(5, json.GetProperty("exemplares").GetInt32());
        Assert.False(json.GetProperty("livroNovo").GetBoolean());
    }

    [Fact]
    public async Task Cada_leitor_so_ve_os_proprios_emprestimos()
    {
        await using (var ana = await ServidorDeTeste.IniciarAsync("ana"))
        {
            ServidorDeTeste.Json(await ana.ChamarAsync("emprestar_livro", new() { ["livro"] = "9780321125217" }));
            var meus = ServidorDeTeste.Json(await ana.ChamarAsync("consultar_meus_emprestimos"));
            Assert.Equal(1, meus.GetProperty("ativos").GetArrayLength());
        }

        // Banco novo a cada servidor: o ponto aqui é que a ferramenta não tem como receber outro leitor.
        await using var bruno = await ServidorDeTeste.IniciarAsync("bruno");
        var deBruno = ServidorDeTeste.Json(await bruno.ChamarAsync("consultar_meus_emprestimos"));
        Assert.Equal(0, deBruno.GetProperty("ativos").GetArrayLength());
    }
}

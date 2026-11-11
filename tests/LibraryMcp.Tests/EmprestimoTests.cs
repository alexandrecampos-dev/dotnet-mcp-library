namespace LibraryMcp.Tests;

/// <summary>Regras de negócio vistas pelo modelo: o erro diz o que aconteceu e o que fazer em seguida.</summary>
public sealed class EmprestimoTests
{
    [Fact]
    public async Task Busca_ignora_acento_e_maiuscula()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var json = ServidorDeTeste.Json(await servidor.ChamarAsync("buscar_livros", new() { ["termo"] = "CODIGO limpo" }));

        Assert.Equal("Código Limpo", json.GetProperty("livros")[0].GetProperty("titulo").GetString());
    }

    [Fact]
    public async Task Empresta_pelo_titulo_com_prazo_de_14_dias()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var json = ServidorDeTeste.Json(await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = "arquitetura limpa" }));

        Assert.Equal("Arquitetura Limpa", json.GetProperty("titulo").GetString());
        Assert.Equal("2026-11-16", json.GetProperty("devolverAte").GetString());
        Assert.Equal(1, json.GetProperty("emprestimosAtivos").GetInt32());
    }

    [Fact]
    public async Task Referencia_ambigua_lista_opcoes_em_vez_de_escolher()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var resultado = await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = "robert martin" });

        Assert.True(resultado.IsError);
        var texto = ServidorDeTeste.Texto(resultado);
        Assert.Contains("9780132350884", texto, StringComparison.Ordinal);
        Assert.Contains("9780134494166", texto, StringComparison.Ordinal);
        Assert.Contains("Pergunte ao usuário", texto, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Respeita_o_limite_de_tres_emprestimos()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");
        foreach (var isbn in new[] { "9780132350884", "9780321125217", "9780134757599" })
            ServidorDeTeste.Json(await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = isbn }));

        var quarto = await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = "entendendo algoritmos" });

        Assert.True(quarto.IsError);
        Assert.Contains("limite é 3", ServidorDeTeste.Texto(quarto), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Atraso_bloqueia_novo_emprestimo_ate_a_devolucao()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");
        ServidorDeTeste.Json(await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = "9781617294532" }));

        servidor.Relogio.Advance(TimeSpan.FromDays(16));

        var bloqueado = await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = "refatoracao" });
        Assert.True(bloqueado.IsError);
        Assert.Contains("em atraso", ServidorDeTeste.Texto(bloqueado), StringComparison.Ordinal);

        var devolucao = ServidorDeTeste.Json(await servidor.ChamarAsync("devolver_livro", new() { ["livro"] = "kubernetes" }));
        Assert.Equal(2, devolucao.GetProperty("diasDeAtraso").GetInt32());

        ServidorDeTeste.Json(await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = "refatoracao" }));
    }

    [Fact]
    public async Task Exemplar_esgotado_recusa_com_mensagem_clara()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");
        // "Kubernetes in Action" tem um único exemplar: depois deste empréstimo, esgota.
        ServidorDeTeste.Json(await servidor.ChamarAsync("emprestar_livro", new() { ["livro"] = "9781617294532" }));

        var json = ServidorDeTeste.Json(await servidor.ChamarAsync("buscar_livros",
            new() { ["termo"] = "kubernetes", ["apenasDisponiveis"] = true }));

        Assert.Equal(0, json.GetProperty("total").GetInt32());
        Assert.Contains("apenasDisponiveis=false", json.GetProperty("observacao").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Devolver_livro_que_nao_esta_com_o_leitor_explica_o_proximo_passo()
    {
        await using var servidor = await ServidorDeTeste.IniciarAsync("ana");

        var resultado = await servidor.ChamarAsync("devolver_livro", new() { ["livro"] = "9780321125217" });

        Assert.True(resultado.IsError);
        Assert.Contains("consultar_meus_emprestimos", ServidorDeTeste.Texto(resultado), StringComparison.Ordinal);
    }
}

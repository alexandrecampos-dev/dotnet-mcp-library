using System.ComponentModel;
using System.Security.Claims;
using LibraryMcp.Core.Dominio;
using LibraryMcp.Core.Servicos;
using LibraryMcp.Tools.Identidade;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace LibraryMcp.Tools.Ferramentas;

/// <summary>
/// Cinco ferramentas por intenção, não uma por endpoint. Cada descrição é o prompt que o modelo lê
/// para decidir se usa a ferramenta: diz quando usar, quando não usar e o que esperar de volta.
/// </summary>
[McpServerToolType]
public sealed class BibliotecaTools(AcervoService acervo, EmprestimoService emprestimos, TimeProvider relogio)
{
    [McpServerTool(Name = "buscar_livros", Title = "Buscar livros no acervo",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description(
        "Procura livros no acervo da biblioteca por título, autor ou ISBN (ignora acentos e maiúsculas). " +
        "Use quando o usuário quer saber se um livro existe, quem escreveu ou se há exemplar disponível, " +
        "ou antes de emprestar quando ele não sabe o ISBN. " +
        "Não use para ver os empréstimos do próprio usuário: para isso existe consultar_meus_emprestimos. " +
        "Devolve no máximo 'limite' livros e o total encontrado; se o total for maior, peça um termo mais específico.")]
    public async Task<ResultadoBusca> BuscarLivros(
        ClaimsPrincipal? usuario,
        [Description("Palavras do título, nome do autor ou ISBN. Ex.: \"arquitetura limpa\", \"chip huyen\", \"9780132350884\".")]
        string termo,
        [Description("true (padrão) para trazer só livros com exemplar disponível agora; false para incluir os esgotados.")]
        bool apenasDisponiveis = true,
        [Description("Quantos livros devolver, de 1 a 25. Padrão 10.")]
        int limite = 10,
        CancellationToken ct = default)
    {
        IdentidadeDaBiblioteca.Exigir(usuario);
        var resultado = await Executar(() => acervo.BuscarAsync(termo, apenasDisponiveis, limite, ct));
        var livros = resultado.Livros
            .Select(l => new LivroResumo(l.Isbn, l.Titulo, l.Autor, l.Ano, l.Disponiveis, l.Exemplares))
            .ToList();

        var observacao = resultado.Total switch
        {
            0 when apenasDisponiveis => "Nenhum livro disponível com esse termo. Tente apenasDisponiveis=false para ver os esgotados.",
            0 => "Nenhum livro do acervo corresponde a esse termo.",
            var t when t > livros.Count => $"Mostrando {livros.Count} de {t}. Peça um termo mais específico ao usuário.",
            _ => null,
        };
        return new ResultadoBusca(resultado.Total, livros, observacao);
    }

    [McpServerTool(Name = "consultar_meus_emprestimos", Title = "Consultar meus empréstimos",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description(
        "Lista os livros que o usuário conectado está com emprestados agora, com data de devolução e dias de atraso, " +
        "e quantos empréstimos ele ainda pode fazer. Use quando ele perguntar o que está com ele, quando vence " +
        "ou se pode pegar mais um. Não recebe parâmetros: o leitor é sempre quem está autenticado nesta conexão.")]
    public async Task<MeusEmprestimos> ConsultarMeusEmprestimos(ClaimsPrincipal? usuario, CancellationToken ct = default)
    {
        var atual = IdentidadeDaBiblioteca.Exigir(usuario);
        var agora = relogio.GetUtcNow();
        var ativos = await Executar(() => emprestimos.ListarAtivosAsync(atual.LeitorId, ct));
        var resumo = ativos
            .Select(e => new EmprestimoResumo(e.Livro.Isbn, e.Livro.Titulo, DateOnly.FromDateTime(e.DevolverAte.UtcDateTime), e.DiasDeAtraso(agora)))
            .ToList();
        return new MeusEmprestimos(resumo, EmprestimoService.LimiteDeEmprestimosAtivos,
            Math.Max(0, EmprestimoService.LimiteDeEmprestimosAtivos - resumo.Count));
    }

    [McpServerTool(Name = "emprestar_livro", Title = "Emprestar um livro",
        ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description(
        "Empresta um livro para o usuário conectado, por 14 dias. Aceita o ISBN ou parte do título; " +
        "o servidor resolve qual é o livro. Se mais de um livro corresponder, a resposta lista as opções: " +
        "pergunte ao usuário qual ele quer e chame de novo com o ISBN, nunca escolha por ele. " +
        "Regras: no máximo 3 empréstimos ativos, nenhum em atraso, um exemplar disponível. " +
        "Só use quando o usuário pedir explicitamente para pegar o livro, não para consultar.")]
    public async Task<ComprovanteDeEmprestimo> EmprestarLivro(
        ClaimsPrincipal? usuario,
        [Description("ISBN (10 ou 13 dígitos, com ou sem hífen) ou parte do título. Ex.: \"9780134494166\" ou \"arquitetura limpa\".")]
        string livro,
        CancellationToken ct = default)
    {
        var atual = IdentidadeDaBiblioteca.Exigir(usuario);
        var emprestimo = await Executar(() => emprestimos.EmprestarAsync(atual.LeitorId, livro, ct));
        var ativos = await Executar(() => emprestimos.ListarAtivosAsync(atual.LeitorId, ct));
        return new ComprovanteDeEmprestimo(emprestimo.Livro.Titulo, emprestimo.Livro.Isbn,
            DateOnly.FromDateTime(emprestimo.DevolverAte.UtcDateTime), ativos.Count, EmprestimoService.LimiteDeEmprestimosAtivos);
    }

    [McpServerTool(Name = "devolver_livro", Title = "Devolver um livro",
        ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description(
        "Registra a devolução de um livro que está emprestado ao usuário conectado. Aceita o ISBN ou parte do título. " +
        "Informa se a devolução foi com atraso. Use só quando o usuário disser que devolveu ou vai devolver o livro agora.")]
    public async Task<ComprovanteDeDevolucao> DevolverLivro(
        ClaimsPrincipal? usuario,
        [Description("ISBN ou parte do título do livro que está com o usuário.")]
        string livro,
        CancellationToken ct = default)
    {
        var atual = IdentidadeDaBiblioteca.Exigir(usuario);
        var emprestimo = await Executar(() => emprestimos.DevolverAsync(atual.LeitorId, livro, ct));
        return new ComprovanteDeDevolucao(emprestimo.Livro.Titulo, emprestimo.Livro.Isbn,
            emprestimo.DiasDeAtraso(relogio.GetUtcNow()));
    }

    [McpServerTool(Name = "cadastrar_exemplares", Title = "Cadastrar exemplares (bibliotecário)",
        ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description(
        "Restrito a bibliotecários. Adiciona exemplares de um livro ao acervo: se o ISBN já existe, soma à quantidade; " +
        "se é novo, cadastra o livro (aí título, autor e ano são obrigatórios). " +
        "Para leitores comuns a chamada é recusada; não tente contornar.")]
    public async Task<ResultadoDoCadastro> CadastrarExemplares(
        ClaimsPrincipal? usuario,
        [Description("ISBN de 10 ou 13 dígitos.")] string isbn,
        [Description("Quantidade de exemplares a adicionar, a partir de 1.")] int quantidade,
        [Description("Título, obrigatório só se o ISBN for novo no acervo.")] string? titulo = null,
        [Description("Autor, obrigatório só se o ISBN for novo no acervo.")] string? autor = null,
        [Description("Ano de publicação, usado só se o ISBN for novo.")] int? ano = null,
        CancellationToken ct = default)
    {
        IdentidadeDaBiblioteca.ExigirBibliotecario(usuario);
        var (livro, novo) = await Executar(() =>
            acervo.CadastrarExemplaresAsync(isbn, titulo ?? string.Empty, autor ?? string.Empty, ano ?? 0, quantidade, ct));
        return new ResultadoDoCadastro(livro.Isbn, livro.Titulo, livro.Exemplares, livro.Disponiveis, novo);
    }

    /// <summary>
    /// Regra de negócio vira McpException, cuja mensagem chega ao modelo como erro da ferramenta.
    /// Qualquer outra exceção segue sem tradução, e o SDK devolve uma mensagem genérica:
    /// detalhe interno (SQL, stack trace) nunca sai do servidor.
    /// </summary>
    private static async Task<T> Executar<T>(Func<Task<T>> acao)
    {
        try
        {
            return await acao();
        }
        catch (RegraDeNegocioException ex)
        {
            throw new McpException(ex.Message, ex);
        }
    }
}

using LibraryMcp.Core.Dados;
using LibraryMcp.Core.Dominio;
using Microsoft.EntityFrameworkCore;

namespace LibraryMcp.Core.Servicos;

public sealed record ResultadoDaBusca(int Total, IReadOnlyList<Livro> Livros);

public sealed class AcervoService(BibliotecaDbContext db)
{
    public const int LimiteMaximoDaBusca = 25;

    public async Task<ResultadoDaBusca> BuscarAsync(
        string termo, bool apenasDisponiveis, int limite, CancellationToken ct = default)
    {
        limite = Math.Clamp(limite, 1, LimiteMaximoDaBusca);
        var consulta = Filtrar(termo);
        if (apenasDisponiveis)
            consulta = consulta.Where(l => l.Disponiveis > 0);

        var total = await consulta.CountAsync(ct);
        var livros = await consulta.OrderBy(l => l.Titulo).Take(limite).AsNoTracking().ToListAsync(ct);
        return new ResultadoDaBusca(total, livros);
    }

    /// <summary>
    /// Resolve a referência que o usuário deu (ISBN ou parte do título/autor) para exatamente um livro.
    /// Ambiguidade não é resolvida no chute: a exceção lista as opções para o modelo perguntar ao usuário.
    /// </summary>
    public async Task<Livro> ResolverAsync(string referencia, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(referencia))
            throw new RegraDeNegocioException("Informe o ISBN ou parte do título do livro.");

        var digitos = Texto.SomenteDigitos(referencia);
        if (digitos.Length is 10 or 13)
        {
            var porIsbn = await db.Livros.FirstOrDefaultAsync(l => l.Isbn == digitos, ct);
            if (porIsbn is not null)
                return porIsbn;
        }

        var candidatos = await Filtrar(referencia).OrderBy(l => l.Titulo).Take(6).ToListAsync(ct);
        return candidatos.Count switch
        {
            1 => candidatos[0],
            0 => throw new RegraDeNegocioException(
                $"Nenhum livro do acervo corresponde a \"{referencia}\". Use buscar_livros para ver as opções."),
            _ => throw new RegraDeNegocioException(
                $"Mais de um livro corresponde a \"{referencia}\": " +
                string.Join("; ", candidatos.Take(5).Select(l => $"\"{l.Titulo}\" de {l.Autor} (ISBN {l.Isbn})")) +
                ". Pergunte ao usuário qual deles e chame de novo informando o ISBN."),
        };
    }

    public async Task<(Livro Livro, bool Novo)> CadastrarExemplaresAsync(
        string isbn, string titulo, string autor, int ano, int quantidade, CancellationToken ct = default)
    {
        var digitos = Texto.SomenteDigitos(isbn);
        var existente = await db.Livros.FirstOrDefaultAsync(l => l.Isbn == digitos, ct);
        if (existente is not null)
        {
            existente.AdicionarExemplares(quantidade);
            await db.SaveChangesAsync(ct);
            return (existente, false);
        }

        if (string.IsNullOrWhiteSpace(titulo) || string.IsNullOrWhiteSpace(autor))
            throw new RegraDeNegocioException(
                $"O ISBN {digitos} ainda não está no acervo. Para cadastrar um livro novo, informe título e autor.");

        var livro = new Livro(isbn, titulo, autor, ano, quantidade);
        db.Livros.Add(livro);
        await db.SaveChangesAsync(ct);
        return (livro, true);
    }

    private IQueryable<Livro> Filtrar(string termo)
    {
        var palavras = Texto.Normalizar(termo)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IQueryable<Livro> consulta = db.Livros;
        foreach (var palavra in palavras)
        {
            var p = palavra;
            consulta = consulta.Where(l => l.TermoDeBusca.Contains(p));
        }
        return consulta;
    }
}

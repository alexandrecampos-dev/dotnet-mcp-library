using System.Globalization;
using System.Text;

namespace LibraryMcp.Core.Dominio;

public enum Papel
{
    Leitor = 0,
    Bibliotecario = 1,
}

/// <summary>
/// Violação de regra de negócio. A mensagem é escrita para ser lida pelo modelo e pelo usuário:
/// diz o que aconteceu e o que fazer em seguida, sem detalhe interno.
/// </summary>
public sealed class RegraDeNegocioException(string mensagem) : Exception(mensagem);

public sealed class Leitor
{
    private Leitor() { }

    public Leitor(string login, string nome, Papel papel)
    {
        Login = login.Trim().ToLowerInvariant();
        Nome = nome.Trim();
        Papel = papel;
    }

    public int Id { get; private set; }
    public string Login { get; private set; } = string.Empty;
    public string Nome { get; private set; } = string.Empty;
    public Papel Papel { get; private set; }
}

public sealed class Livro
{
    private Livro() { }

    public Livro(string isbn, string titulo, string autor, int ano, int exemplares)
    {
        if (exemplares < 1)
            throw new RegraDeNegocioException("A quantidade de exemplares precisa ser pelo menos 1.");

        Isbn = Texto.SomenteDigitos(isbn);
        if (Isbn.Length is not (10 or 13))
            throw new RegraDeNegocioException($"ISBN inválido: '{isbn}'. Informe 10 ou 13 dígitos.");

        Titulo = titulo.Trim();
        Autor = autor.Trim();
        Ano = ano;
        Exemplares = exemplares;
        Disponiveis = exemplares;
        TermoDeBusca = Texto.Normalizar($"{Titulo} {Autor} {Isbn}");
    }

    public int Id { get; private set; }
    public string Isbn { get; private set; } = string.Empty;
    public string Titulo { get; private set; } = string.Empty;
    public string Autor { get; private set; } = string.Empty;
    public int Ano { get; private set; }
    public int Exemplares { get; private set; }
    public int Disponiveis { get; private set; }

    /// <summary>Título, autor e ISBN sem acento e em minúsculas, para busca tolerante.</summary>
    public string TermoDeBusca { get; private set; } = string.Empty;

    internal void Retirar()
    {
        if (Disponiveis == 0)
            throw new RegraDeNegocioException(
                $"Todos os {Exemplares} exemplares de \"{Titulo}\" estão emprestados no momento.");
        Disponiveis--;
    }

    internal void Receber()
    {
        if (Disponiveis < Exemplares)
            Disponiveis++;
    }

    internal void AdicionarExemplares(int quantidade)
    {
        if (quantidade < 1)
            throw new RegraDeNegocioException("A quantidade de exemplares precisa ser pelo menos 1.");
        Exemplares += quantidade;
        Disponiveis += quantidade;
    }
}

public sealed class Emprestimo
{
    private Emprestimo() { }

    internal Emprestimo(Livro livro, Leitor leitor, DateTimeOffset agora, int prazoEmDias)
    {
        Livro = livro;
        LivroId = livro.Id;
        Leitor = leitor;
        LeitorId = leitor.Id;
        RetiradoEm = agora;
        DevolverAte = agora.AddDays(prazoEmDias);
    }

    public int Id { get; private set; }
    public int LivroId { get; private set; }
    public Livro Livro { get; private set; } = null!;
    public int LeitorId { get; private set; }
    public Leitor Leitor { get; private set; } = null!;
    public DateTimeOffset RetiradoEm { get; private set; }
    public DateTimeOffset DevolverAte { get; private set; }
    public DateTimeOffset? DevolvidoEm { get; private set; }

    public bool Ativo => DevolvidoEm is null;

    public int DiasDeAtraso(DateTimeOffset referencia) =>
        Math.Max(0, (int)Math.Ceiling(((DevolvidoEm ?? referencia) - DevolverAte).TotalDays));

    internal void Encerrar(DateTimeOffset agora) => DevolvidoEm = agora;
}

public static class Texto
{
    public static string SomenteDigitos(string valor) =>
        new(valor.Where(char.IsAsciiDigit).ToArray());

    /// <summary>Minúsculas e sem acento: "Código Limpo" e "codigo limpo" viram o mesmo termo.</summary>
    public static string Normalizar(string valor)
    {
        var decomposto = valor.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

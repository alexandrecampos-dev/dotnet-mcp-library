using LibraryMCP.Domain.Common;

namespace LibraryMCP.Domain.Entities;

public class Livro : BaseEntity
{
    public string Titulo { get; set; } = string.Empty;
    public string Autor { get; set; } = string.Empty;
    public string ISBN { get; set; } = string.Empty;
    public string Editora { get; set; } = string.Empty;
    public int AnoPublicacao { get; set; }
    public int QuantidadeTotal { get; set; }
    public int QuantidadeDisponivel { get; set; }
    
    public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();
}

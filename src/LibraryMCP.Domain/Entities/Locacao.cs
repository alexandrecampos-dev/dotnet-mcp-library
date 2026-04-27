using LibraryMCP.Domain.Common;

namespace LibraryMCP.Domain.Entities;

public class Locacao : BaseEntity
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    
    public Guid LivroId { get; set; }
    public Livro Livro { get; set; } = null!;
    
    public DateTime DataLocacao { get; set; }
    public DateTime DataPrevistaDevolucao { get; set; }
    public DateTime? DataDevolucao { get; set; }
    public bool Devolvido { get; set; } = false;
}

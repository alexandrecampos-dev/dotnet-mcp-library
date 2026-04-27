using LibraryMCP.Domain.Common;

namespace LibraryMCP.Domain.Entities;

public class Usuario : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    
    public ICollection<Locacao> Locacoes { get; set; } = new List<Locacao>();
}

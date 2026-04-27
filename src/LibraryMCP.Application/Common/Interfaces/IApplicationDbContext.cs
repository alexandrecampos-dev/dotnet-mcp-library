using LibraryMCP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryMCP.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Usuario> Usuarios { get; }
    DbSet<Livro> Livros { get; }
    DbSet<Locacao> Locacoes { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

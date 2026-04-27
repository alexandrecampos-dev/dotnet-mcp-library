using LibraryMCP.Application.Common.Interfaces;
using LibraryMCP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryMCP.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<Locacao> Locacoes => Set<Locacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CPF).IsRequired().HasMaxLength(14);
            entity.Property(e => e.Telefone).HasMaxLength(20);
        });

        modelBuilder.Entity<Livro>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Autor).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ISBN).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Editora).HasMaxLength(200);
        });

        modelBuilder.Entity<Locacao>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.Locacoes)
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Livro)
                .WithMany(l => l.Locacoes)
                .HasForeignKey(e => e.LivroId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

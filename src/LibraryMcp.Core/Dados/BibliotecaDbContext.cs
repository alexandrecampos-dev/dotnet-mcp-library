using LibraryMcp.Core.Dominio;
using Microsoft.EntityFrameworkCore;

namespace LibraryMcp.Core.Dados;

public sealed class BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options) : DbContext(options)
{
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<Leitor> Leitores => Set<Leitor>();
    public DbSet<Emprestimo> Emprestimos => Set<Emprestimo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Livro>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.Isbn).IsUnique();
            e.Property(l => l.Isbn).HasMaxLength(13).IsRequired();
            e.Property(l => l.Titulo).HasMaxLength(300).IsRequired();
            e.Property(l => l.Autor).HasMaxLength(200).IsRequired();
            e.Property(l => l.TermoDeBusca).HasMaxLength(600).IsRequired();
        });

        modelBuilder.Entity<Leitor>(e =>
        {
            e.HasKey(l => l.Id);
            e.HasIndex(l => l.Login).IsUnique();
            e.Property(l => l.Login).HasMaxLength(60).IsRequired();
            e.Property(l => l.Nome).HasMaxLength(200).IsRequired();
            e.Property(l => l.Papel).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<Emprestimo>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Livro).WithMany().HasForeignKey(x => x.LivroId);
            e.HasOne(x => x.Leitor).WithMany().HasForeignKey(x => x.LeitorId);
            e.HasIndex(x => new { x.LeitorId, x.DevolvidoEm });
            e.Ignore(x => x.Ativo);
            // SQLite não ordena DateTimeOffset nativamente; guardamos em ticks UTC.
            e.Property(x => x.RetiradoEm).HasConversion(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
            e.Property(x => x.DevolverAte).HasConversion(v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero));
            e.Property(x => x.DevolvidoEm).HasConversion(
                v => v.HasValue ? v.Value.UtcTicks : (long?)null,
                v => v.HasValue ? new DateTimeOffset(v.Value, TimeSpan.Zero) : null);
        });
    }
}

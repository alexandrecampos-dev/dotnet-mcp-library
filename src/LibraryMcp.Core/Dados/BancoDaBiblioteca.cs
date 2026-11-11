using LibraryMcp.Core.Dominio;
using LibraryMcp.Core.Servicos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryMcp.Core.Dados;

public static class BancoDaBiblioteca
{
    /// <summary>
    /// Caminho padrão do banco. Fica fora da pasta do projeto porque clientes MCP (Claude Desktop,
    /// Claude Code) iniciam o servidor a partir de diretórios arbitrários.
    /// </summary>
    public static string ConexaoPadrao()
    {
        var pasta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "dotnet-mcp-library");
        Directory.CreateDirectory(pasta);
        return $"Data Source={Path.Combine(pasta, "biblioteca.db")}";
    }

    public static IServiceCollection AddBiblioteca(this IServiceCollection services, string? conexao = null)
    {
        services.AddDbContext<BibliotecaDbContext>(o => o.UseSqlite(conexao ?? ConexaoPadrao()));
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AcervoService>();
        services.AddScoped<EmprestimoService>();
        return services;
    }

    /// <summary>Cria o esquema e popula um acervo de exemplo na primeira execução.</summary>
    public static async Task PrepararAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        await db.Database.EnsureCreatedAsync(ct);

        if (await db.Leitores.AnyAsync(ct))
            return;

        db.Leitores.AddRange(
            new Leitor("ana", "Ana Souza", Papel.Leitor),
            new Leitor("bruno", "Bruno Lima", Papel.Leitor),
            new Leitor("carla", "Carla Mendes", Papel.Bibliotecario));

        db.Livros.AddRange(
            new Livro("9780132350884", "Código Limpo", "Robert C. Martin", 2008, 3),
            new Livro("9780134494166", "Arquitetura Limpa", "Robert C. Martin", 2017, 2),
            new Livro("9780321125217", "Domain-Driven Design", "Eric Evans", 2003, 2),
            new Livro("9781617294532", "Kubernetes in Action", "Marko Luksa", 2017, 1),
            new Livro("9781098166304", "AI Engineering", "Chip Huyen", 2025, 2),
            new Livro("9780134757599", "Refatoração", "Martin Fowler", 2018, 2),
            new Livro("9781492078005", "Designing Machine Learning Systems", "Chip Huyen", 2022, 1),
            new Livro("9788575228579", "Entendendo Algoritmos", "Aditya Bhargava", 2017, 3));

        await db.SaveChangesAsync(ct);
    }
}

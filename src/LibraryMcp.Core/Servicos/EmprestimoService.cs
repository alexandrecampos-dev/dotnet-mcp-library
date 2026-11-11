using LibraryMcp.Core.Dados;
using LibraryMcp.Core.Dominio;
using Microsoft.EntityFrameworkCore;

namespace LibraryMcp.Core.Servicos;

public sealed class EmprestimoService(BibliotecaDbContext db, AcervoService acervo, TimeProvider relogio)
{
    public const int LimiteDeEmprestimosAtivos = 3;
    public const int PrazoEmDias = 14;

    public async Task<IReadOnlyList<Emprestimo>> ListarAtivosAsync(int leitorId, CancellationToken ct = default) =>
        await db.Emprestimos
            .Include(e => e.Livro)
            .Where(e => e.LeitorId == leitorId && e.DevolvidoEm == null)
            .OrderBy(e => e.DevolverAte)
            .AsNoTracking()
            .ToListAsync(ct);

    public async Task<Emprestimo> EmprestarAsync(int leitorId, string referenciaDoLivro, CancellationToken ct = default)
    {
        var leitor = await db.Leitores.FindAsync([leitorId], ct)
            ?? throw new RegraDeNegocioException("Leitor não encontrado.");
        var livro = await acervo.ResolverAsync(referenciaDoLivro, ct);
        var agora = relogio.GetUtcNow();
        var ativos = await ListarAtivosAsync(leitorId, ct);

        if (ativos.Any(e => e.LivroId == livro.Id))
            throw new RegraDeNegocioException($"Você já está com \"{livro.Titulo}\" emprestado.");

        var atrasado = ativos.FirstOrDefault(e => e.DiasDeAtraso(agora) > 0);
        if (atrasado is not null)
            throw new RegraDeNegocioException(
                $"Há um empréstimo em atraso (\"{atrasado.Livro.Titulo}\", {atrasado.DiasDeAtraso(agora)} dia(s)). " +
                "Devolva esse livro antes de pegar outro.");

        if (ativos.Count >= LimiteDeEmprestimosAtivos)
            throw new RegraDeNegocioException(
                $"Você já tem {ativos.Count} empréstimos ativos, o limite é {LimiteDeEmprestimosAtivos}. " +
                "Devolva um livro antes de pegar outro.");

        livro.Retirar();
        var emprestimo = new Emprestimo(livro, leitor, agora, PrazoEmDias);
        db.Emprestimos.Add(emprestimo);
        await db.SaveChangesAsync(ct);
        return emprestimo;
    }

    public async Task<Emprestimo> DevolverAsync(int leitorId, string referenciaDoLivro, CancellationToken ct = default)
    {
        var livro = await acervo.ResolverAsync(referenciaDoLivro, ct);
        var emprestimo = await db.Emprestimos
            .Include(e => e.Livro)
            .FirstOrDefaultAsync(e => e.LeitorId == leitorId && e.LivroId == livro.Id && e.DevolvidoEm == null, ct)
            ?? throw new RegraDeNegocioException(
                $"Não há empréstimo ativo de \"{livro.Titulo}\" no seu nome. Use consultar_meus_emprestimos para ver os seus.");

        emprestimo.Encerrar(relogio.GetUtcNow());
        livro.Receber();
        await db.SaveChangesAsync(ct);
        return emprestimo;
    }
}

using LibraryMCP.Application.Common.Interfaces;
using LibraryMCP.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryMCP.Application.Features.Locacoes.Commands;

public record CreateLocacaoCommand(
    Guid UsuarioId,
    Guid LivroId,
    int DiasEmprestimo = 14
) : IRequest<Guid>;

public class CreateLocacaoCommandHandler : IRequestHandler<CreateLocacaoCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateLocacaoCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateLocacaoCommand request, CancellationToken cancellationToken)
    {
        var livro = await _context.Livros
            .FirstOrDefaultAsync(l => l.Id == request.LivroId, cancellationToken);

        if (livro == null)
            throw new Exception("Livro não encontrado");

        if (livro.QuantidadeDisponivel <= 0)
            throw new Exception("Livro indisponível para locação");

        var locacao = new Locacao
        {
            Id = Guid.NewGuid(),
            UsuarioId = request.UsuarioId,
            LivroId = request.LivroId,
            DataLocacao = DateTime.UtcNow,
            DataPrevistaDevolucao = DateTime.UtcNow.AddDays(request.DiasEmprestimo),
            Devolvido = false,
            CreatedAt = DateTime.UtcNow
        };

        livro.QuantidadeDisponivel--;
        livro.UpdatedAt = DateTime.UtcNow;

        _context.Locacoes.Add(locacao);
        await _context.SaveChangesAsync(cancellationToken);

        return locacao.Id;
    }
}

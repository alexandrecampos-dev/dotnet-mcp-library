using LibraryMCP.Application.Common.Interfaces;
using LibraryMCP.Domain.Entities;
using MediatR;

namespace LibraryMCP.Application.Features.Livros.Commands;

public record CreateLivroCommand(
    string Titulo,
    string Autor,
    string ISBN,
    string Editora,
    int AnoPublicacao,
    int QuantidadeTotal
) : IRequest<Guid>;

public class CreateLivroCommandHandler : IRequestHandler<CreateLivroCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateLivroCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateLivroCommand request, CancellationToken cancellationToken)
    {
        var livro = new Livro
        {
            Id = Guid.NewGuid(),
            Titulo = request.Titulo,
            Autor = request.Autor,
            ISBN = request.ISBN,
            Editora = request.Editora,
            AnoPublicacao = request.AnoPublicacao,
            QuantidadeTotal = request.QuantidadeTotal,
            QuantidadeDisponivel = request.QuantidadeTotal,
            CreatedAt = DateTime.UtcNow
        };

        _context.Livros.Add(livro);
        await _context.SaveChangesAsync(cancellationToken);

        return livro.Id;
    }
}

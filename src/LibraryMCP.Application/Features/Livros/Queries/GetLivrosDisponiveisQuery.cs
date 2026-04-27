using LibraryMCP.Application.Common.Interfaces;
using LibraryMCP.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LibraryMCP.Application.Features.Livros.Queries;

public record GetLivrosDisponiveisQuery : IRequest<List<LivroDto>>;

public record LivroDto(
    Guid Id,
    string Titulo,
    string Autor,
    string ISBN,
    string Editora,
    int AnoPublicacao,
    int QuantidadeDisponivel
);

public class GetLivrosDisponiveisQueryHandler : IRequestHandler<GetLivrosDisponiveisQuery, List<LivroDto>>
{
    private readonly IApplicationDbContext _context;

    public GetLivrosDisponiveisQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LivroDto>> Handle(GetLivrosDisponiveisQuery request, CancellationToken cancellationToken)
    {
        return await _context.Livros
            .Where(l => l.QuantidadeDisponivel > 0)
            .Select(l => new LivroDto(
                l.Id,
                l.Titulo,
                l.Autor,
                l.ISBN,
                l.Editora,
                l.AnoPublicacao,
                l.QuantidadeDisponivel
            ))
            .ToListAsync(cancellationToken);
    }
}

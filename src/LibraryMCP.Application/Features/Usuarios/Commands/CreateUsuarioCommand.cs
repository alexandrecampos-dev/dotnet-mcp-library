using LibraryMCP.Application.Common.Interfaces;
using LibraryMCP.Domain.Entities;
using MediatR;

namespace LibraryMCP.Application.Features.Usuarios.Commands;

public record CreateUsuarioCommand(
    string Nome,
    string Email,
    string CPF,
    string Telefone
) : IRequest<Guid>;

public class CreateUsuarioCommandHandler : IRequestHandler<CreateUsuarioCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateUsuarioCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateUsuarioCommand request, CancellationToken cancellationToken)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Email = request.Email,
            CPF = request.CPF,
            Telefone = request.Telefone,
            Ativo = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync(cancellationToken);

        return usuario.Id;
    }
}

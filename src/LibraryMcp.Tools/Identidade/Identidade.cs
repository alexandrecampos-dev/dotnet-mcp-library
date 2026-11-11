using System.Globalization;
using System.Security.Claims;
using LibraryMcp.Core.Dominio;
using ModelContextProtocol;

namespace LibraryMcp.Tools.Identidade;

/// <summary>Quem está chamando a ferramenta. Vem sempre do transporte, nunca de um parâmetro do modelo.</summary>
public sealed record UsuarioAtual(int LeitorId, string Nome, Papel Papel);

public static class IdentidadeDaBiblioteca
{
    public const string TipoDeAutenticacaoStdio = "BibliotecaStdio";

    /// <summary>Monta o principal a partir do leitor cadastrado. O papel vem do banco, não do token.</summary>
    public static ClaimsPrincipal CriarPrincipal(Leitor leitor, string tipoDeAutenticacao) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, leitor.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, leitor.Nome),
                new Claim(ClaimTypes.Role, leitor.Papel.ToString()),
            ],
            tipoDeAutenticacao, ClaimTypes.Name, ClaimTypes.Role));

    public static UsuarioAtual Exigir(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true ||
            !int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, CultureInfo.InvariantCulture, out var id))
        {
            throw new McpException(
                "Acesso negado: nenhum leitor identificado nesta conexão. " +
                "No stdio, defina BIBLIOTECA_USUARIO com o login; no HTTP, envie um token válido.");
        }

        var papel = Enum.TryParse<Papel>(principal.FindFirst(ClaimTypes.Role)?.Value, out var p) ? p : Papel.Leitor;
        return new UsuarioAtual(id, principal.Identity.Name ?? "leitor", papel);
    }

    public static UsuarioAtual ExigirBibliotecario(ClaimsPrincipal? principal)
    {
        var usuario = Exigir(principal);
        if (usuario.Papel != Papel.Bibliotecario)
            throw new McpException("Acesso negado: só bibliotecários podem cadastrar exemplares no acervo.");
        return usuario;
    }
}

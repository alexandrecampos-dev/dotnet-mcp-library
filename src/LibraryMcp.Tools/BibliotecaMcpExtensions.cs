using System.Diagnostics;
using System.Security.Claims;
using LibraryMcp.Tools.Ferramentas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibraryMcp.Tools;

public static class BibliotecaMcpExtensions
{
    /// <summary>Registra as ferramentas da biblioteca e a trilha de auditoria de cada chamada.</summary>
    public static IMcpServerBuilder WithBibliotecaTools(this IMcpServerBuilder builder) =>
        builder
            .WithTools<BibliotecaTools>()
            .WithRequestFilters(filtros => filtros.AddCallToolFilter(next => async (context, ct) =>
            {
                var inicio = Stopwatch.GetTimestamp();
                var erro = true;
                try
                {
                    // Recusas e regras de negócio saem daqui como exceção (o SDK as converte em erro
                    // de ferramenta depois); por isso o registro fica no finally, para não perder as falhas.
                    var resultado = await next(context, ct);
                    erro = resultado.IsError ?? false;
                    return resultado;
                }
                finally
                {
                    var logger = context.Services?.GetService<ILoggerFactory>()?.CreateLogger("LibraryMcp.Auditoria");
                    logger?.LogInformation(
                        "tool={Ferramenta} leitor={Leitor} erro={Erro} duracao_ms={Duracao}",
                        context.Params?.Name,
                        context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonimo",
                        erro,
                        (long)Stopwatch.GetElapsedTime(inicio).TotalMilliseconds);
                }
            }));

    /// <summary>
    /// Para transportes sem autenticação própria (stdio): a identidade vem do processo,
    /// definida por quem configurou o servidor, e não do que o modelo manda.
    /// Sem leitor configurado, a conexão recebe um principal anônimo (não autenticado): assim a
    /// ferramenta roda e recusa com "Acesso negado", em vez de falhar na injeção do parâmetro.
    /// </summary>
    public static IMcpServerBuilder WithIdentidadeDoProcesso(this IMcpServerBuilder builder, ClaimsPrincipal? principal)
    {
        var efetivo = principal ?? new ClaimsPrincipal(new ClaimsIdentity());
        return builder.WithMessageFilters(filtros => filtros.AddIncomingFilter(next => async (context, ct) =>
        {
            context.User = efetivo;
            await next(context, ct);
        }));
    }
}

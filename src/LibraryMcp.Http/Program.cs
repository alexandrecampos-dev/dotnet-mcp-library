using System.Security.Claims;
using System.Text;
using LibraryMcp.Core.Dados;
using LibraryMcp.Tools;
using LibraryMcp.Tools.Identidade;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore;

// Servidor MCP remoto (Streamable HTTP). Toda chamada exige um JWT válido; o leitor e o papel
// saem do token validado e do cadastro, nunca de um parâmetro que o modelo preenche.

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection("Jwt").Get<OpcoesJwt>() ?? new OpcoesJwt();

// Utilitário de desenvolvimento: `dotnet run -- token ana` imprime um token de teste.
if (args is ["token", var loginDoToken, ..])
{
    if (!builder.Environment.IsDevelopment() || string.IsNullOrEmpty(jwt.ChaveDeAssinatura))
    {
        Console.Error.WriteLine("Geração de token só existe em Development, com Jwt:ChaveDeAssinatura configurada.");
        return 1;
    }
    Console.WriteLine(TokenDeDesenvolvimento.Emitir(jwt, loginDoToken));
    return 0;
}

builder.Services.AddBiblioteca(builder.Configuration.GetConnectionString("Biblioteca"));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        // Em produção: Authority/Audience do seu provedor (Entra ID, Auth0, Keycloak).
        // Em desenvolvimento: chave simétrica local, só para testar sem provedor externo.
        if (!string.IsNullOrEmpty(jwt.Authority))
            o.Authority = jwt.Authority;

        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Emissor,
            ValidateAudience = true,
            ValidAudience = jwt.Audiencia,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = string.IsNullOrEmpty(jwt.ChaveDeAssinatura)
                ? null
                : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveDeAssinatura)),
        };
        o.Events = new JwtBearerEvents
        {
            // O token diz QUEM é (sub = login). O cadastro diz O QUE pode (papel).
            OnTokenValidated = async ctx =>
            {
                var login = ctx.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)?.Trim().ToLowerInvariant();
                var db = ctx.HttpContext.RequestServices.GetRequiredService<BibliotecaDbContext>();
                var leitor = login is null
                    ? null
                    : await db.Leitores.AsNoTracking().FirstOrDefaultAsync(l => l.Login == login, ctx.HttpContext.RequestAborted);

                if (leitor is null)
                {
                    ctx.Fail("Leitor não cadastrado.");
                    return;
                }
                ctx.Principal = IdentidadeDaBiblioteca.CriarPrincipal(leitor, JwtBearerDefaults.AuthenticationScheme);
            },
        };
    });
builder.Services.AddAuthorization();

builder.Services
    .AddMcpServer(o => o.ServerInfo = new() { Name = "biblioteca", Version = "2.0.0" })
    .WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless)
    .WithBibliotecaTools();

var app = builder.Build();
await BancoDaBiblioteca.PrepararAsync(app.Services);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapMcp("/mcp").RequireAuthorization();

await app.RunAsync();
return 0;

internal sealed class OpcoesJwt
{
    public string? Authority { get; init; }
    public string Emissor { get; init; } = "biblioteca-dev";
    public string Audiencia { get; init; } = "biblioteca-mcp";
    public string? ChaveDeAssinatura { get; init; }
}

internal static class TokenDeDesenvolvimento
{
    public static string Emitir(OpcoesJwt jwt, string login) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Emissor,
            Audience = jwt.Audiencia,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, login)]),
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveDeAssinatura!)),
                SecurityAlgorithms.HmacSha256),
        });
}

public partial class Program;

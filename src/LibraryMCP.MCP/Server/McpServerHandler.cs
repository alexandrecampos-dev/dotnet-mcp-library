using LibraryMCP.Application.Features.Livros.Commands;
using LibraryMCP.Application.Features.Livros.Queries;
using LibraryMCP.Application.Features.Locacoes.Commands;
using LibraryMCP.Application.Features.Usuarios.Commands;
using LibraryMCP.MCP.Models;
using MediatR;
using System.Text.Json;

namespace LibraryMCP.MCP.Server;

public class McpServerHandler
{
    private readonly IMediator _mediator;

    public McpServerHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    private static string GetStringValue(Dictionary<string, object> parameters, string key)
    {
        var value = parameters[key];
        if (value is JsonElement jsonElement)
            return jsonElement.GetString() ?? string.Empty;
        return value?.ToString() ?? string.Empty;
    }

    private static int GetIntValue(Dictionary<string, object> parameters, string key)
    {
        var value = parameters[key];
        if (value is JsonElement jsonElement)
            return jsonElement.GetInt32();
        return Convert.ToInt32(value);
    }

    private static Guid GetGuidValue(Dictionary<string, object> parameters, string key)
    {
        var value = parameters[key];
        if (value is JsonElement jsonElement)
            return Guid.Parse(jsonElement.GetString()!);
        return Guid.Parse(value?.ToString()!);
    }

    public List<ToolDefinition> GetAvailableTools()
    {
        return new List<ToolDefinition>
        {
            new ToolDefinition
            {
                Name = "listar_livros_disponiveis",
                Description = "Lista todos os livros disponíveis para locação na biblioteca",
                Parameters = new Dictionary<string, ParameterSchema>()
            },
            new ToolDefinition
            {
                Name = "cadastrar_livro",
                Description = "Cadastra um novo livro no acervo da biblioteca",
                Parameters = new Dictionary<string, ParameterSchema>
                {
                    { "titulo", new ParameterSchema { Type = "string", Description = "Título do livro", Required = true } },
                    { "autor", new ParameterSchema { Type = "string", Description = "Autor do livro", Required = true } },
                    { "isbn", new ParameterSchema { Type = "string", Description = "ISBN do livro", Required = true } },
                    { "editora", new ParameterSchema { Type = "string", Description = "Editora do livro", Required = true } },
                    { "anoPublicacao", new ParameterSchema { Type = "number", Description = "Ano de publicação", Required = true } },
                    { "quantidade", new ParameterSchema { Type = "number", Description = "Quantidade de exemplares", Required = true } }
                }
            },
            new ToolDefinition
            {
                Name = "cadastrar_usuario",
                Description = "Cadastra um novo usuário na biblioteca",
                Parameters = new Dictionary<string, ParameterSchema>
                {
                    { "nome", new ParameterSchema { Type = "string", Description = "Nome completo", Required = true } },
                    { "email", new ParameterSchema { Type = "string", Description = "E-mail", Required = true } },
                    { "cpf", new ParameterSchema { Type = "string", Description = "CPF", Required = true } },
                    { "telefone", new ParameterSchema { Type = "string", Description = "Telefone", Required = true } }
                }
            },
            new ToolDefinition
            {
                Name = "locar_livro",
                Description = "Realiza a locação de um livro para um usuário",
                Parameters = new Dictionary<string, ParameterSchema>
                {
                    { "usuarioId", new ParameterSchema { Type = "string", Description = "ID do usuário", Required = true } },
                    { "livroId", new ParameterSchema { Type = "string", Description = "ID do livro", Required = true } },
                    { "diasEmprestimo", new ParameterSchema { Type = "number", Description = "Dias de empréstimo (padrão 14)", Required = false } }
                }
            }
        };
    }

    public async Task<McpResponse> ExecuteToolAsync(string toolName, Dictionary<string, object>? parameters)
    {
        try
        {
            return toolName switch
            {
                "listar_livros_disponiveis" => await ListarLivrosDisponiveisAsync(),
                "cadastrar_livro" => await CadastrarLivroAsync(parameters!),
                "cadastrar_usuario" => await CadastrarUsuarioAsync(parameters!),
                "locar_livro" => await LocarLivroAsync(parameters!),
                _ => new McpResponse { Success = false, Error = "Ferramenta não encontrada" }
            };
        }
        catch (Exception ex)
        {
            return new McpResponse { Success = false, Error = ex.Message };
        }
    }

    private async Task<McpResponse> ListarLivrosDisponiveisAsync()
    {
        var livros = await _mediator.Send(new GetLivrosDisponiveisQuery());
        return new McpResponse { Success = true, Data = livros };
    }

    private async Task<McpResponse> CadastrarLivroAsync(Dictionary<string, object> parameters)
    {
        var command = new CreateLivroCommand(
            GetStringValue(parameters, "titulo"),
            GetStringValue(parameters, "autor"),
            GetStringValue(parameters, "isbn"),
            GetStringValue(parameters, "editora"),
            GetIntValue(parameters, "anoPublicacao"),
            GetIntValue(parameters, "quantidade")
        );

        var id = await _mediator.Send(command);
        return new McpResponse { Success = true, Data = new { Id = id, Message = "Livro cadastrado com sucesso" } };
    }

    private async Task<McpResponse> CadastrarUsuarioAsync(Dictionary<string, object> parameters)
    {
        var command = new CreateUsuarioCommand(
            GetStringValue(parameters, "nome"),
            GetStringValue(parameters, "email"),
            GetStringValue(parameters, "cpf"),
            GetStringValue(parameters, "telefone")
        );

        var id = await _mediator.Send(command);
        return new McpResponse { Success = true, Data = new { Id = id, Message = "Usuário cadastrado com sucesso" } };
    }

    private async Task<McpResponse> LocarLivroAsync(Dictionary<string, object> parameters)
    {
        var diasEmprestimo = parameters.ContainsKey("diasEmprestimo") 
            ? GetIntValue(parameters, "diasEmprestimo")
            : 14;

        var command = new CreateLocacaoCommand(
            GetGuidValue(parameters, "usuarioId"),
            GetGuidValue(parameters, "livroId"),
            diasEmprestimo
        );

        var id = await _mediator.Send(command);
        return new McpResponse { Success = true, Data = new { Id = id, Message = "Locação realizada com sucesso" } };
    }
}

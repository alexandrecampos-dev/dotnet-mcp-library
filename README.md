# dotnet-mcp-library

Exemplo prático de um **Servidor MCP (Model Context Protocol)** construído com **.NET 9** — expõe um domínio de negócio real como ferramentas tipadas para o Claude interagir diretamente.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![MCP](https://img.shields.io/badge/MCP-Model_Context_Protocol-CC785C?style=flat-square&logo=anthropic)](https://modelcontextprotocol.io)
[![Arquitetura](https://img.shields.io/badge/Arquitetura-Clean_Architecture-00C7B7?style=flat-square)]()
[![Padrão](https://img.shields.io/badge/Padrão-CQRS_+_MediatR-764BA2?style=flat-square)]()
[![Licença](https://img.shields.io/badge/Licença-MIT-green?style=flat-square)](LICENSE)

---

## O que este projeto demonstra

A maioria dos servidores MCP é implementada em Python ou Node.js. Este projeto mostra como construir um servidor MCP em **.NET puro**, integrando o Claude a um sistema de biblioteca com chamadas de ferramenta tipadas e seguras.

| Característica | Detalhe |
|---------------|---------|
| Domínio | Sistema de biblioteca — livros, usuários, locações |
| Ferramentas MCP | 4 ferramentas expostas com schemas bem definidos |
| Arquitetura | Clean Architecture com separação estrita de camadas |
| Padrão | CQRS + MediatR para separação leitura/escrita |
| Persistência | EF Core + SQLite — zero configuração extra |
| Documentação | OpenAPI gerado automaticamente junto às ferramentas MCP |

---

## Arquitetura

```
dotnet-mcp-library/
└── src/
    ├── LibraryMCP.Domain/          # Entidades e regras de negócio — sem dependências externas
    ├── LibraryMCP.Application/     # CQRS: Commands, Queries e Handlers via MediatR
    ├── LibraryMCP.Infrastructure/  # EF Core, SQLite, DbContext
    ├── LibraryMCP.MCP/             # Servidor MCP: definição e execução das ferramentas
    └── LibraryMCP.API/             # Host ASP.NET Core, configuração de DI, endpoints HTTP
```

Regra de dependência: `API -> MCP -> Application -> Domain <- Infrastructure`

---

## Ferramentas MCP disponíveis

| Ferramenta | Descrição |
|-----------|----------|
| `listar_livros_disponiveis` | Lista todos os livros disponíveis para locação |
| `cadastrar_livro` | Registra um novo livro no acervo |
| `cadastrar_usuario` | Registra um novo usuário na biblioteca |
| `locar_livro` | Cria uma locação para um usuário |

Schema de exemplo:

```json
{
  "name": "cadastrar_livro",
  "description": "Registra um novo livro no acervo da biblioteca",
  "parameters": {
    "titulo":        { "type": "string",  "required": true },
    "autor":         { "type": "string",  "required": true },
    "isbn":          { "type": "string",  "required": true },
    "editora":       { "type": "string",  "required": true },
    "anoPublicacao": { "type": "number",  "required": true },
    "quantidade":    { "type": "number",  "required": true }
  }
}
```

---

## Início rápido

**Pré-requisito:** [.NET 9 SDK](https://dotnet.microsoft.com/download)

```bash
# 1. Clone o repositório
git clone https://github.com/alexandrecampos-dev/dotnet-mcp-library.git
cd dotnet-mcp-library

# 2. Restaure e compile
dotnet restore && dotnet build

# 3. Execute
cd src/LibraryMCP.API && dotnet run
```

API disponível em `http://localhost:5000`  
OpenAPI em `http://localhost:5000/openapi/v1.json`

---

## Referência da API HTTP

### Listar ferramentas disponíveis

```http
GET /mcp/tools
```

### Executar uma ferramenta

```http
POST /mcp/execute
Content-Type: application/json

{
  "method": "cadastrar_livro",
  "params": {
    "titulo": "Clean Code",
    "autor": "Robert C. Martin",
    "isbn": "978-0132350884",
    "editora": "Prentice Hall",
    "anoPublicacao": 2008,
    "quantidade": 5
  }
}
```

Resposta:

```json
{
  "success": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "message": "Livro cadastrado com sucesso"
  }
}
```

---

## Integração com Claude Desktop

Adicione ao arquivo de configuração do Claude Desktop:

- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "biblioteca": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/caminho/para/dotnet-mcp-library/src/LibraryMCP.API"],
      "env": {
        "ASPNETCORE_URLS": "http://localhost:5000"
      }
    }
  }
}
```

Após reiniciar o Claude Desktop, as ferramentas da biblioteca aparecerão no painel de ferramentas.

---

## Regras de negócio

- Um livro só pode ser locado quando `QuantidadeDisponivel > 0`
- Cada locação decrementa `QuantidadeDisponivel` automaticamente
- Prazo padrão de empréstimo: 14 dias (configurável por locação)

---

## Stack

| Tecnologia | Finalidade |
|-----------|-----------|
| .NET 9 | Runtime |
| ASP.NET Core | Host HTTP e roteamento |
| MediatR | Pipeline CQRS |
| Entity Framework Core | ORM e migrations |
| SQLite | Banco de dados local sem configuração extra |
| FluentValidation | Validação de entrada |
| OpenAPI | Documentação da API |

---

## Autor

**Alexandre Campos** — Software Engineer & AI Engineer  
[LinkedIn](https://www.linkedin.com/in/alexandre-campos-3725b18b/) · [GitHub](https://github.com/alexandrecampos-dev)

---

MIT License

# dotnet-mcp-library

A practical example of a **Model Context Protocol (MCP) Server** built with **.NET 9**, following **Clean Architecture**, **CQRS** and **MediatR** — exposing a library management domain as AI-accessible tools for Claude.

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![MCP](https://img.shields.io/badge/MCP-Model_Context_Protocol-CC785C?style=flat-square&logo=anthropic)](https://modelcontextprotocol.io)
[![Architecture](https://img.shields.io/badge/Architecture-Clean_Architecture-00C7B7?style=flat-square)]()
[![Pattern](https://img.shields.io/badge/Pattern-CQRS_+_MediatR-764BA2?style=flat-square)]()
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

---

## What This Project Demonstrates

This project shows how to expose a real .NET domain as an **MCP Server** — enabling Claude (or any MCP-compatible AI client) to interact with your application through structured, type-safe tool calls.

The domain is a **library system** (books, users, rentals), but the architecture and patterns apply to any enterprise domain.

Key concepts:
- Building an MCP Server in .NET from scratch, without Python or Node.js
- Clean Architecture with strict layer separation and dependency inversion
- CQRS + MediatR for write/read separation inside the application layer
- EF Core + SQLite for zero-config persistence
- Full OpenAPI documentation alongside MCP tools

---

## Architecture

```
dotnet-mcp-library/
└── src/
    ├── LibraryMCP.Domain/          # Entities and business rules — no external dependencies
    ├── LibraryMCP.Application/     # CQRS: Commands, Queries and Handlers via MediatR
    ├── LibraryMCP.Infrastructure/  # EF Core, SQLite, DbContext
    ├── LibraryMCP.MCP/             # MCP Server: tool definitions and execution
    └── LibraryMCP.API/             # ASP.NET Core host, DI wiring, HTTP endpoints
```

Dependency rule: `API -> MCP -> Application -> Domain <- Infrastructure`

| Layer | Responsibility |
|-------|---------------|
| Domain | Entities (Book, User, Rental) with business invariants |
| Application | Commands (write) and Queries (read) dispatched via MediatR |
| Infrastructure | EF Core persistence, migrations, DbContext implementation |
| MCP | Tool schema definitions, dispatches calls to Application layer |
| API | ASP.NET Core host, DI configuration, HTTP endpoint mapping |

---

## MCP Tools

The server exposes 4 tools that Claude can invoke directly:

| Tool | Description |
|------|-------------|
| `listar_livros_disponiveis` | Lists all books currently available for rental |
| `cadastrar_livro` | Registers a new book in the catalog |
| `cadastrar_usuario` | Registers a new library user |
| `locar_livro` | Creates a rental record for a user |

Tool schema example:

```json
{
  "name": "cadastrar_livro",
  "description": "Registers a new book in the library catalog",
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

## Getting Started

**Prerequisites:** [.NET 9 SDK](https://dotnet.microsoft.com/download)

```bash
git clone https://github.com/alexandrecampos-dev/dotnet-mcp-library.git
cd dotnet-mcp-library

dotnet restore
dotnet build

cd src/LibraryMCP.API
dotnet run
```

API: `http://localhost:5000`  
OpenAPI: `http://localhost:5000/openapi/v1.json`

---

## HTTP Endpoints

**List available tools**

```http
GET /mcp/tools
```

**Execute a tool**

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

Response:

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

## Claude Desktop Integration

Add to your Claude Desktop config file:

- Windows: `%APPDATA%\Claude\claude_desktop_config.json`
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

```json
{
  "mcpServers": {
    "biblioteca": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/path/to/dotnet-mcp-library/src/LibraryMCP.API"],
      "env": {
        "ASPNETCORE_URLS": "http://localhost:5000"
      }
    }
  }
}
```

Restart Claude Desktop — the library tools will appear in the tool panel.

---

## Business Rules

- A book can only be rented when `QuantidadeDisponivel > 0`
- Each rental decrements `QuantidadeDisponivel` automatically
- Default loan period: 14 days (configurable per rental)
- Rental records track `DataLocacao`, `DataPrevistaDevolucao` and `DataDevolucao`

---

## Tech Stack

| Technology | Purpose |
|-----------|---------|
| .NET 9 | Runtime |
| ASP.NET Core | HTTP host and routing |
| MediatR | CQRS mediator pipeline |
| Entity Framework Core | ORM and migrations |
| SQLite | Zero-config local database |
| FluentValidation | Command input validation |
| OpenAPI | API documentation |

---

## References

- [Model Context Protocol — Specification](https://modelcontextprotocol.io)
- [Clean Architecture — Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-Clean-Architecture.html)
- [CQRS — Martin Fowler](https://martinfowler.com/bliki/CQRS.html)
- [MediatR](https://github.com/jbogard/MediatR)

---

## Author

**Alexandre Campos** — Software Engineer & AI Engineer  
[LinkedIn](https://www.linkedin.com/in/alexandre-campos-3725b18b/) · [GitHub](https://github.com/alexandrecampos-dev)

---

MIT License

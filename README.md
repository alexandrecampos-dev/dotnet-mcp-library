# dotnet-mcp-library

Servidor **MCP (Model Context Protocol)** em **.NET 10** com o SDK oficial, expondo um sistema de biblioteca como ferramentas para o Claude e qualquer outro cliente MCP. Roda por **stdio** (Claude Code, Claude Desktop) e por **HTTP** com autenticação JWT.

[![CI](https://github.com/alexandrecampos-dev/dotnet-mcp-library/actions/workflows/ci.yml/badge.svg)](https://github.com/alexandrecampos-dev/dotnet-mcp-library/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![MCP SDK](https://img.shields.io/badge/ModelContextProtocol-2.4-CC785C?style=flat-square)](https://github.com/modelcontextprotocol/csharp-sdk)
[![Licença](https://img.shields.io/badge/Licença-MIT-green?style=flat-square)](LICENSE)

---

## O que este projeto demonstra

Conectar um agente aos seus sistemas deixou de ser o problema: o protocolo já é suportado pelos principais clientes. O que separa um servidor MCP bom de um que só gasta token é o **desenho do que o agente enxerga**. Este repositório é um exemplo pequeno e completo desse desenho.

| Decisão | Como está aqui | Onde no código |
|---|---|---|
| **Ferramenta por intenção, não por endpoint** | 5 ferramentas que correspondem ao que o usuário quer fazer. `emprestar_livro` recebe "arquitetura limpa" ou um ISBN e o servidor resolve qual é o livro, em vez de o modelo encadear buscas e adivinhar IDs. | [`BibliotecaTools.cs`](src/LibraryMcp.Tools/Ferramentas/BibliotecaTools.cs) |
| **A descrição é prompt** | Cada descrição diz quando usar, quando **não** usar e o que esperar de volta; cada parâmetro tem exemplo. Quando a referência é ambígua, a resposta lista as opções e manda perguntar ao usuário, nunca escolher por ele. | [`BibliotecaTools.cs`](src/LibraryMcp.Tools/Ferramentas/BibliotecaTools.cs) |
| **Autorização no servidor** | Nenhuma ferramenta aceita ID de usuário vindo do modelo. O leitor vem do transporte (variável de ambiente no stdio, JWT no HTTP) e o papel (leitor ou bibliotecário) vem do cadastro, não do token. | [`Identidade.cs`](src/LibraryMcp.Tools/Identidade/Identidade.cs), [`LibraryMcp.Http/Program.cs`](src/LibraryMcp.Http/Program.cs) |
| **Resposta enxuta** | Saída estruturada com só o que a próxima decisão precisa: sem Id interno, sem entidade inteira, sem dado de outro leitor. A busca tem teto de itens e avisa quando há mais resultados. | [`Respostas.cs`](src/LibraryMcp.Tools/Ferramentas/Respostas.cs) |
| **Erro seguro** | Regra de negócio volta como erro de ferramenta com mensagem útil ("você já tem 3 empréstimos ativos, devolva um antes"). Qualquer outra exceção vira mensagem genérica: SQL e stack trace não saem do servidor. | `Executar` em [`BibliotecaTools.cs`](src/LibraryMcp.Tools/Ferramentas/BibliotecaTools.cs) |
| **Trilha de auditoria** | Toda chamada registra ferramenta, leitor, resultado e duração, inclusive as recusadas. | [`BibliotecaMcpExtensions.cs`](src/LibraryMcp.Tools/BibliotecaMcpExtensions.cs) |

As consultas são anotadas como somente leitura (`readOnlyHint`), o que permite ao cliente executá-las sem pedir confirmação e reservar a confirmação para o que muda dados.

---

## Ferramentas

| Ferramenta | Quem pode | O que faz |
|---|---|---|
| `buscar_livros` | qualquer leitor | Busca por título, autor ou ISBN, ignorando acento e maiúscula. Só leitura. |
| `consultar_meus_emprestimos` | qualquer leitor | Empréstimos ativos de quem está conectado, com vencimento e atraso. Só leitura. |
| `emprestar_livro` | qualquer leitor | Empresta por 14 dias. Regras: até 3 ativos, nenhum em atraso, exemplar disponível. |
| `devolver_livro` | qualquer leitor | Registra a devolução e informa se houve atraso. |
| `cadastrar_exemplares` | bibliotecário | Soma exemplares a um ISBN existente ou cadastra um livro novo. |

O banco SQLite é criado na primeira execução com um acervo de exemplo e três logins: **`ana`** e **`bruno`** (leitores) e **`carla`** (bibliotecária).

---

## Como usar

Pré-requisito: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/alexandrecampos-dev/dotnet-mcp-library.git
cd dotnet-mcp-library
dotnet build
```

### Claude Code (stdio)

```bash
claude mcp add biblioteca --env BIBLIOTECA_USUARIO=ana -- dotnet run --project /caminho/para/dotnet-mcp-library/src/LibraryMcp.Stdio
```

Depois, numa conversa: *"tem algum livro da Chip Huyen disponível? se tiver, pega pra mim"*.

### Claude Desktop (stdio)

No `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "biblioteca": {
      "command": "dotnet",
      "args": ["run", "--project", "/caminho/para/dotnet-mcp-library/src/LibraryMcp.Stdio"],
      "env": { "BIBLIOTECA_USUARIO": "ana" }
    }
  }
}
```

### MCP Inspector

```bash
npx @modelcontextprotocol/inspector -e BIBLIOTECA_USUARIO=carla dotnet run --project src/LibraryMcp.Stdio
```

### Servidor remoto (Streamable HTTP + JWT)

```bash
dotnet run --project src/LibraryMcp.Http                 # sobe em http://localhost:5100/mcp
dotnet run --project src/LibraryMcp.Http -- token ana    # imprime um token de desenvolvimento
claude mcp add --transport http biblioteca-http http://localhost:5100/mcp --header "Authorization: Bearer <token>"
```

Sem token, ou com token de login não cadastrado, a resposta é `401`. O gerador de token só existe em `Development`.

---

## Testes

```bash
dotnet test
```

São 20 testes, e nenhum deles chama a classe de ferramentas diretamente:

- **Protocolo:** um cliente MCP real conversa com o servidor por pipes em memória e confere a lista de ferramentas, os esquemas (nenhum parâmetro de identidade), as anotações e o formato das respostas.
- **Regras e autorização:** empréstimo por título, referência ambígua, limite de 3, bloqueio por atraso (com relógio controlado), recusa ao leitor que tenta cadastrar.
- **HTTP:** o servidor ASP.NET Core em memória, com JWT válido, forjado, de login inexistente e ausente.

---

## Estrutura

```
src/
  LibraryMcp.Core/    domínio (Livro, Leitor, Emprestimo), regras e EF Core + SQLite
  LibraryMcp.Tools/   ferramentas MCP, identidade e auditoria (compartilhadas pelos dois hosts)
  LibraryMcp.Stdio/   host stdio: identidade vem da variável BIBLIOTECA_USUARIO
  LibraryMcp.Http/    host Streamable HTTP: identidade vem do JWT
tests/
  LibraryMcp.Tests/   protocolo, regras, autorização e transporte HTTP
```

## Para levar a produção

- Configure `Jwt:Authority` e `Jwt:Audiencia` do seu provedor (Entra ID, Auth0, Keycloak) e remova a chave simétrica de desenvolvimento.
- Restrinja `AllowedHosts` aos nomes públicos do serviço. Habilite CORS só se houver cliente de navegador, e com a política mais restrita possível.
- Troque `EnsureCreated` por migrações do EF Core.

## Histórico

- **2.0.0:** reescrita sobre o SDK oficial (`ModelContextProtocol` 2.4), com transportes stdio e HTTP, ferramentas por intenção, autorização no servidor e testes pelo protocolo. Sai o MediatR, que passou a ter licença comercial.
- **1.0.0** ([tag](https://github.com/alexandrecampos-dev/dotnet-mcp-library/tree/v1.0.0)): uma API REST com endpoints em formato próprio (`/mcp/tools`, `/mcp/execute`). Não implementava o protocolo MCP.

## Licença

[MIT](LICENSE)

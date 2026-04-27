# Scripts de Teste - LibraryMCP

Este arquivo contém exemplos de requisições HTTP para testar o sistema.

## 1. Listar Ferramentas Disponíveis

```bash
curl -X GET http://localhost:5000/mcp/tools
```

## 2. Cadastrar um Livro

```bash
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "cadastrar_livro",
    "params": {
      "titulo": "Clean Code",
      "autor": "Robert C. Martin",
      "isbn": "978-0132350884",
      "editora": "Prentice Hall",
      "anoPublicacao": 2008,
      "quantidade": 5
    }
  }'
```

## 3. Cadastrar Mais Livros

```bash
# Domain-Driven Design
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "cadastrar_livro",
    "params": {
      "titulo": "Domain-Driven Design",
      "autor": "Eric Evans",
      "isbn": "978-0321125217",
      "editora": "Addison-Wesley",
      "anoPublicacao": 2003,
      "quantidade": 3
    }
  }'

# Design Patterns
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "cadastrar_livro",
    "params": {
      "titulo": "Design Patterns",
      "autor": "Gang of Four",
      "isbn": "978-0201633610",
      "editora": "Addison-Wesley",
      "anoPublicacao": 1994,
      "quantidade": 4
    }
  }'

# The Pragmatic Programmer
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "cadastrar_livro",
    "params": {
      "titulo": "The Pragmatic Programmer",
      "autor": "Andrew Hunt & David Thomas",
      "isbn": "978-0135957059",
      "editora": "Addison-Wesley",
      "anoPublicacao": 2019,
      "quantidade": 2
    }
  }'
```

## 4. Listar Livros Disponíveis

```bash
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "listar_livros_disponiveis"
  }'
```

## 5. Cadastrar Usuários

```bash
# Usuário 1
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "cadastrar_usuario",
    "params": {
      "nome": "João Silva",
      "email": "joao.silva@email.com",
      "cpf": "123.456.789-00",
      "telefone": "(11) 98765-4321"
    }
  }'

# Usuário 2
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "cadastrar_usuario",
    "params": {
      "nome": "Maria Santos",
      "email": "maria.santos@email.com",
      "cpf": "987.654.321-00",
      "telefone": "(11) 91234-5678"
    }
  }'

# Usuário 3
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "cadastrar_usuario",
    "params": {
      "nome": "Carlos Oliveira",
      "email": "carlos.oliveira@email.com",
      "cpf": "456.789.123-00",
      "telefone": "(11) 99876-5432"
    }
  }'
```

## 6. Realizar Locação

**IMPORTANTE**: Você precisa substituir `USUARIO_ID` e `LIVRO_ID` pelos IDs reais retornados nos passos anteriores.

```bash
curl -X POST http://localhost:5000/mcp/execute \
  -H "Content-Type: application/json" \
  -d '{
    "method": "locar_livro",
    "params": {
      "usuarioId": "USUARIO_ID_AQUI",
      "livroId": "LIVRO_ID_AQUI",
      "diasEmprestimo": 14
    }
  }'
```

## PowerShell (Windows)

### Listar Ferramentas
```powershell
Invoke-RestMethod -Uri "http://localhost:5000/mcp/tools" -Method Get
```

### Cadastrar Livro
```powershell
$body = @{
    method = "cadastrar_livro"
    params = @{
        titulo = "Clean Code"
        autor = "Robert C. Martin"
        isbn = "978-0132350884"
        editora = "Prentice Hall"
        anoPublicacao = 2008
        quantidade = 5
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $body -ContentType "application/json"
```

### Listar Livros Disponíveis
```powershell
$body = @{
    method = "listar_livros_disponiveis"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $body -ContentType "application/json"
```

### Cadastrar Usuário
```powershell
$body = @{
    method = "cadastrar_usuario"
    params = @{
        nome = "João Silva"
        email = "joao.silva@email.com"
        cpf = "123.456.789-00"
        telefone = "(11) 98765-4321"
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $body -ContentType "application/json"
```

### Realizar Locação
```powershell
$body = @{
    method = "locar_livro"
    params = @{
        usuarioId = "USUARIO_ID_AQUI"
        livroId = "LIVRO_ID_AQUI"
        diasEmprestimo = 14
    }
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $body -ContentType "application/json"
```

## Teste Completo (Script PowerShell)

```powershell
# 1. Cadastrar Livro
Write-Host "1. Cadastrando livro..." -ForegroundColor Green
$livroBody = @{
    method = "cadastrar_livro"
    params = @{
        titulo = "Clean Code"
        autor = "Robert C. Martin"
        isbn = "978-0132350884"
        editora = "Prentice Hall"
        anoPublicacao = 2008
        quantidade = 5
    }
} | ConvertTo-Json

$livroResponse = Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $livroBody -ContentType "application/json"
$livroId = $livroResponse.data.id
Write-Host "Livro cadastrado! ID: $livroId" -ForegroundColor Yellow

# 2. Cadastrar Usuário
Write-Host "`n2. Cadastrando usuário..." -ForegroundColor Green
$usuarioBody = @{
    method = "cadastrar_usuario"
    params = @{
        nome = "João Silva"
        email = "joao.silva@email.com"
        cpf = "123.456.789-00"
        telefone = "(11) 98765-4321"
    }
} | ConvertTo-Json

$usuarioResponse = Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $usuarioBody -ContentType "application/json"
$usuarioId = $usuarioResponse.data.id
Write-Host "Usuário cadastrado! ID: $usuarioId" -ForegroundColor Yellow

# 3. Listar Livros Disponíveis
Write-Host "`n3. Listando livros disponíveis..." -ForegroundColor Green
$listarBody = @{
    method = "listar_livros_disponiveis"
} | ConvertTo-Json

$livros = Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $listarBody -ContentType "application/json"
Write-Host "Livros disponíveis:" -ForegroundColor Yellow
$livros.data | Format-Table

# 4. Realizar Locação
Write-Host "`n4. Realizando locação..." -ForegroundColor Green
$locacaoBody = @{
    method = "locar_livro"
    params = @{
        usuarioId = $usuarioId
        livroId = $livroId
        diasEmprestimo = 14
    }
} | ConvertTo-Json

$locacaoResponse = Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $locacaoBody -ContentType "application/json"
Write-Host "Locação realizada! ID: $($locacaoResponse.data.id)" -ForegroundColor Yellow

# 5. Verificar livros disponíveis novamente
Write-Host "`n5. Verificando disponibilidade após locação..." -ForegroundColor Green
$livrosApos = Invoke-RestMethod -Uri "http://localhost:5000/mcp/execute" -Method Post -Body $listarBody -ContentType "application/json"
Write-Host "Livros disponíveis após locação:" -ForegroundColor Yellow
$livrosApos.data | Format-Table

Write-Host "`n✅ Teste completo finalizado!" -ForegroundColor Green
```

## Observações

- Certifique-se de que a API está rodando antes de executar os testes
- Os IDs são gerados automaticamente (GUID)
- Salve os IDs retornados para usar nas locações
- A quantidade disponível diminui automaticamente após cada locação

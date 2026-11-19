# LittleEcommerce

API de venda de produtos em .NET 10.

## Stack

| Peça | Uso |
|---|---|
| ASP.NET Core 10 | API REST em `api/v1` |
| PostgreSQL 17 | banco principal, EF Core com migrations |
| Redis 7 | blacklist de JWT, cache, trava de idempotência |
| MongoDB 8 | logs da aplicação e auditoria |
| Mailpit | caixa de e-mail local para desenvolvimento |

## Como rodar

Tudo em containers:

```bash
docker compose up -d --build
```

| Serviço | Endereço |
|---|---|
| API | http://localhost:5080 |
| Documentação (Scalar) | http://localhost:5080/scalar |
| OpenAPI | http://localhost:5080/openapi/v1.json |
| Mailpit | http://localhost:8025 |

Só a infraestrutura em containers e a API pela IDE ou terminal:

```bash
docker compose up -d postgres redis mongo mailpit
```

```bash
dotnet run --project CoreApi
```

Testes:

```bash
dotnet test LittleEcommerce.slnx
```

## Health checks

| Rota | O que verifica |
|---|---|
| `GET /health/live` | o processo está de pé |
| `GET /health/ready` | conexão com PostgreSQL, Redis e MongoDB (503 se algum falhar) |

## Respostas

Sucesso:

```json
{ "status": 200, "message": "Success", "data": { } }
```

Erro:

```json
{
  "status": 404,
  "code": "NOT_FOUND",
  "title": "Recurso não encontrado",
  "message": "Produto não encontrado",
  "traceId": "0HN...:00000001"
}
```

Erros de validação trazem `errors` com a lista de mensagens por campo.

## Estrutura

```
CoreApi/
  Program.cs
  Source/
    Application/     exceções, interfaces e serviços
    Common/          constantes, enums, erros e regras
    Controllers/     Store (loja) e Admin
    Data/            DbContext, entidades, views, repositórios e seeders
    Infrastructure/  registro de dependências, health checks, autorização, auditoria, outbox
    Middlewares/
    Shared/          DTOs, extensões, options
tests/
  LittleEcommerce.UnitTests/
  LittleEcommerce.IntegrationTests/
```

## Fluxo de branches

| Branch | Papel |
|---|---|
| `release` | integração; toda branch de trabalho nasce daqui e volta por PR |
| `staging` | ambiente de testes; recebe a `release` por PR |
| `main` | produção; versões marcadas com tags `vX.Y.Z` |

## Etapas

| # | Etapa | Status |
|---|---|---|
| 1 | Fundação: solução, Docker, envelope de resposta, exceções, health checks, CI | concluída |
| 2 | Logs no MongoDB | pendente |
| 3 | Banco, usuários e JWT com blacklist | pendente |
| 4 | Permissões e auditoria administrativa | pendente |
| 5 | Outbox e e-mail de boas-vindas | pendente |
| 6 | Produtos e vínculos | pendente |
| 7 | Carrinho | pendente |
| 8 | Checkout idempotente e pedidos | pendente |
| 9 | Views de diagnóstico de vendas | pendente |
| 10 | Testes de integração e acabamento | pendente |

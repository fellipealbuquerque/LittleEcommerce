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
| API | http://localhost:5080 (redireciona para o Swagger) |
| Swagger UI | http://localhost:5080/swagger |
| Scalar | http://localhost:5080/scalar |
| OpenAPI | http://localhost:5080/openapi/v1.json |
| Mailpit | http://localhost:8025 |

Só a infraestrutura em containers e a API pela IDE ou terminal:

```bash
docker compose up -d postgres redis mongo mailpit
```

```bash
dotnet run --project CoreApi
```

Testes (os de integração sobem PostgreSQL, Redis e MongoDB com Testcontainers, então o Docker precisa estar rodando):

```bash
dotnet test LittleEcommerce.slnx
```

## Banco de dados

PostgreSQL com EF Core e migrations em `CoreApi/Migrations`. Tabelas e colunas em snake_case. Entidades com `deleted_at` usam soft delete (somem das consultas, ficam no banco). `created_at` e `updated_at` são preenchidos automaticamente.

As migrations pendentes rodam no startup (`Database:MigrateOnStartup`). Para criar uma nova:

```bash
dotnet tool restore
```

```bash
dotnet tool run dotnet-ef migrations add NomeDaMigration --project CoreApi --output-dir Migrations
```

O seed cria os papéis `Admin`, `Manager` e `Customer` e, se `Seed:AdminEmail` e `Seed:AdminPassword` estiverem preenchidos, o primeiro administrador. Em desenvolvimento eles estão em `appsettings.Development.json`; em outros ambientes use `Seed__AdminEmail` e `Seed__AdminPassword`.

## Autenticação

| Rota | O que faz |
|---|---|
| `POST /api/v1/auth/register` | cria um cliente e devolve os tokens |
| `POST /api/v1/auth/login` | troca e-mail e senha pelos tokens |
| `POST /api/v1/auth/refresh` | troca o refresh token por um par novo |
| `POST /api/v1/auth/logout` | encerra a sessão atual |
| `POST /api/v1/auth/logout-all` | encerra todas as sessões do usuário |
| `GET/PUT /api/v1/users/me` | perfil |
| `PATCH /api/v1/users/me/password` | troca a senha e encerra todas as sessões |

No Swagger, faça login, clique em **Authorize** e cole o `accessToken`.

- **Access token** (JWT, 15 min) carrega `sub`, `role`, `jti` e `sv` (versão de sessão do usuário).
- **Refresh token** (7 dias) é aleatório; o banco guarda só o hash SHA-256. Cada uso gera um par novo e invalida o anterior. Reusar um refresh já trocado é tratado como roubo: a família inteira é revogada e todas as sessões caem (`REFRESH_TOKEN_REUSED`).
- **Blacklist**: no logout, o `jti` vai para o Redis com TTL igual ao tempo que falta para o token expirar, então ele sai da blacklist sozinho quando expira.
- **Versão de sessão**: logout-all e troca de senha incrementam `session_version` do usuário; todo token com `sv` antigo é recusado (`SESSION_REVOKED`). A versão atual fica em cache no Redis.
- Refresh tokens expirados são apagados pelo `RefreshTokenCleanupWorker` (`Jwt:CleanupIntervalMinutes`, 60 min).
- Register, login e refresh têm rate limit por IP (`RateLimiting:AuthPermitLimit` por `AuthWindowSeconds`, 10 por minuto).
- Se o Redis cair, rotas autenticadas respondem 503 `AUTH_UNAVAILABLE` em vez de aceitar um token que pode estar revogado.
- `Jwt__Secret` é obrigatório fora de desenvolvimento e precisa ter pelo menos 32 caracteres.

| Código | Quando |
|---|---|
| `TOKEN_EXPIRED` | access token vencido; renove com o refresh |
| `TOKEN_REVOKED` | access token de uma sessão encerrada por logout |
| `SESSION_REVOKED` | sessão derrubada por logout-all, troca de senha ou reuso de refresh |
| `INVALID_CREDENTIALS` | e-mail ou senha inválidos (mesma resposta para e-mail inexistente) |
| `INVALID_REFRESH_TOKEN` / `REFRESH_TOKEN_EXPIRED` / `REFRESH_TOKEN_REUSED` | problemas no refresh |
| `USER_BLOCKED` | usuário bloqueado |

## Documentação

O documento OpenAPI é gerado pelo próprio ASP.NET Core (`Microsoft.AspNetCore.OpenApi`) a partir dos controllers e atributos (`[EndpointSummary]`, `[ProducesResponseType]`, `[Tags]`). Swagger UI e Scalar leem o mesmo documento.

Fica ligada em Development e Staging (`Documentation:Enabled`) e desligada em produção.

## Logs

Serilog escreve no console e na coleção `logs` do MongoDB, em lotes e em background: se o Mongo cair, as requisições seguem funcionando e os logs ficam na fila (`MongoLogging:QueueLimit`).

- Toda requisição gera um log com método, rota, status, duração, query string, IP, user agent e endpoint. 5xx vira `Error`, 4xx vira `Warning`.
- Health checks e documentação (`/health`, `/swagger`, `/scalar`, `/openapi`) ficam em `Debug` e não são gravados por padrão.
- `X-Correlation-Id`: enviado pelo cliente ou gerado pela API, volta no header da resposta, vai em todo log da requisição e é o `traceId` dos erros.
- Campos com nome de segredo (`password`, `senha`, `token`, `secret`, `authorization`, `cookie`, `apikey`, cartão, `cvv`) viram `***`, inclusive dentro de objetos e na query string.
- Retenção por índice TTL: `MongoLogging:RetentionDays` (30 dias). Mudar o valor atualiza o índice na próxima gravação.

Consultando:

```bash
docker compose exec mongo mongosh little_ecommerce
```

```js
db.logs.find({ correlationId: "abc-123" })
db.logs.find({ level: "Error" }).sort({ timestamp: -1 }).limit(20)
db.logs.find({ statusCode: { $gte: 400 } }, { message: 1, correlationId: 1 })
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

Erros de validação trazem `errors` com a lista de mensagens por campo, com o nome do campo como no JSON (`email`, `items[0].quantity`). JSON malformado responde `errors.body`.

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
| 2 | Logs no MongoDB | concluída |
| 3 | Banco, usuários e JWT com blacklist | concluída |
| 4 | Permissões e auditoria administrativa | pendente |
| 5 | Outbox e e-mail de boas-vindas | pendente |
| 6 | Produtos e vínculos | pendente |
| 7 | Carrinho | pendente |
| 8 | Checkout idempotente e pedidos | pendente |
| 9 | Views de diagnóstico de vendas | pendente |
| 10 | Testes de integração e acabamento | pendente |

# LittleEcommerce

API de venda de produtos em .NET 10.

## Stack

| Peça | Uso |
|---|---|
| ASP.NET Core 10 | API REST em `api/v1` |
| PostgreSQL 17 | banco principal, EF Core com migrations |
| Redis 7 | blacklist de JWT, cache, trava de idempotência |
| MongoDB 8 | logs da aplicação (`logs`) e auditoria (`audit_logs`) |
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

Testes (os de integração sobem PostgreSQL, Redis, MongoDB e Mailpit com Testcontainers, então o Docker precisa estar rodando):

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

## Permissões

Cada papel tem um conjunto de permissões (`permissions` e `role_permissions`). As rotas admin usam `[RequiresPermission("users.manage")]`, que vira um requisito da autorização do ASP.NET Core. As permissões de cada papel ficam em cache no Redis por 10 minutos e o cache é limpo quando elas mudam. Se o Redis cair, a consulta vai direto ao banco.

| Permissão | Libera |
|---|---|
| `users.read` | listar e consultar usuários |
| `users.manage` | trocar papel, bloquear, desbloquear e encerrar sessões |
| `roles.read` | listar papéis e o catálogo de permissões |
| `roles.manage` | criar papel, definir permissões e remover papel |
| `audit.read` | consultar a auditoria |
| `outbox.read` | consultar mensagens do outbox |
| `outbox.manage` | reenviar mensagens e o e-mail de boas-vindas |

| Papel | Permissões |
|---|---|
| `Admin` | todas, sempre; não pode ser editado |
| `Manager` | `users.read` (as próximas etapas acrescentam catálogo, pedidos e relatórios) |
| `Customer` | nenhuma rota admin |

Regras de proteção: papéis do sistema não podem ser removidos, papel com usuários não pode ser removido, ninguém troca o próprio papel nem se bloqueia, e o último administrador ativo não pode ser rebaixado nem bloqueado. Trocar papel ou bloquear derruba as sessões do usuário.

| Rota | Permissão |
|---|---|
| `GET /api/v1/admin/users` (`search`, `role`, `status`, `limit`, `offset`) | `users.read` |
| `GET /api/v1/admin/users/{id}` | `users.read` |
| `PATCH /api/v1/admin/users/{id}/role` | `users.manage` |
| `PATCH /api/v1/admin/users/{id}/block` e `/unblock` | `users.manage` |
| `POST /api/v1/admin/users/{id}/revoke-sessions` | `users.manage` |
| `GET /api/v1/admin/roles`, `GET /api/v1/admin/permissions` | `roles.read` |
| `POST /api/v1/admin/roles`, `PUT /api/v1/admin/roles/{id}/permissions`, `DELETE /api/v1/admin/roles/{id}` | `roles.manage` |
| `GET /api/v1/admin/audit-logs` | `audit.read` |

No Swagger, a descrição de cada rota admin mostra a permissão necessária.

## Outbox e e-mail de boas-vindas

O cadastro grava o usuário e a mensagem `email.welcome` na tabela `outbox_messages` **na mesma transação**: ou os dois existem, ou nenhum. Depois do commit o dispatcher é acordado na hora e envia o e-mail (SMTP com MailKit; em desenvolvimento cai no Mailpit, http://localhost:8025). O cadastro nunca espera o SMTP: se o servidor de e-mail estiver fora, a conta é criada do mesmo jeito e o envio fica para as novas tentativas.

| Status | Quando |
|---|---|
| `Pending` | na fila; `next_attempt_at` diz quando pode ser enviada |
| `Processing` | reservada por um dispatcher (`locked_until`); se o processo cair, a reserva expira e outro pega |
| `Sent` | entregue |
| `DeadLetter` | esgotou as tentativas (`Outbox:MaxAttempts`, 5) ou não tem handler |

- **Envio imediato**: sinal em memória acorda o `OutboxDispatcherWorker` assim que a transação confirma; sem sinal, ele varre a tabela a cada `Outbox:PollIntervalSeconds`.
- **Reenvio automático**: falhas voltam para `Pending` com espera exponencial (`BaseRetryDelaySeconds` 30 s, dobrando até `MaxRetryDelayMinutes` 60), e o erro fica em `last_error`.
- **Reenvio manual**: `POST /api/v1/admin/outbox/{id}/retry` (qualquer status, menos `Processing`), `POST /api/v1/admin/outbox/retry-dead-letters` e `POST /api/v1/admin/users/{id}/resend-welcome-email` (gera um e-mail novo).
- Várias instâncias da API podem rodar o dispatcher juntas: a reserva usa `FOR UPDATE SKIP LOCKED`.
- A entrega é *at-least-once*: se o processo cair entre o envio e a gravação do status, o e-mail pode sair duas vezes. Cada e-mail leva o id da mensagem no `Message-Id`.
- Os logs do envio carregam o `correlationId` da requisição que criou a mensagem.

| Rota | Permissão |
|---|---|
| `GET /api/v1/admin/outbox` (`status`, `type`, `aggregateId`), `GET /api/v1/admin/outbox/{id}`, `GET /api/v1/admin/outbox/summary` | `outbox.read` |
| `POST /api/v1/admin/outbox/{id}/retry`, `POST /api/v1/admin/outbox/retry-dead-letters`, `POST /api/v1/admin/users/{id}/resend-welcome-email` | `outbox.manage` |

Configuração em `Email` (`FromAddress`, `FromName`, `StoreUrl`, `Smtp:Host`, `Port`, `Security`, `Username`, `Password`) e `Outbox`. `Email:FromAddress` e `Email:Smtp:Host` são obrigatórios.

## Auditoria

Coleção `audit_logs` no MongoDB, gravada em background (fila em memória + inserção em lote com novas tentativas). Retenção por TTL em `AuditLogging:RetentionDays` (365 dias).

| `kind` | O que registra |
|---|---|
| `entity-change` | toda alteração de dados feita pelo EF: `Created`, `Updated`, `Deleted` (inclusive soft delete), com `old` e `new` só dos campos que mudaram. Só é gravado depois do commit da transação; rollback descarta. |
| `admin-action` | toda chamada a rota admin, inclusive leituras e falhas: ação (`AdminUsers.Block`), permissão exigida, argumentos, status, duração |
| `access-denied` | toda resposta 403, com a permissão que faltou |

Todo registro traz o autor (`actor`: tipo, id, e-mail, papel, IP, user agent) e o `correlationId` da requisição, o mesmo dos logs. Campos com nome de segredo (`passwordHash`, `token`…) são gravados como `***`. Entidades marcadas com `[NotAudited]` (refresh tokens) ficam de fora.

```js
db.audit_logs.find({ entityType: "User", entityId: "<id>" }).sort({ timestamp: -1 })
db.audit_logs.find({ kind: "admin-action", "actor.userId": "<id>" })
db.audit_logs.find({ kind: "access-denied" }).sort({ timestamp: -1 }).limit(20)
```

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
| 4 | Permissões e auditoria administrativa | concluída |
| 5 | Outbox e e-mail de boas-vindas | concluída |
| 6 | Produtos e vínculos | pendente |
| 7 | Carrinho | pendente |
| 8 | Checkout idempotente e pedidos | pendente |
| 9 | Views de diagnóstico de vendas | pendente |
| 10 | Testes de integração e acabamento | pendente |

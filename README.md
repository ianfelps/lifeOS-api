# ServiceLifeOS

API REST do LifeOS, um sistema pessoal para financas, habitos, treinos, metas e gamificacao. O backend usa .NET 10, PostgreSQL e uma arquitetura de ports and adapters preparada para a evolucao modular do produto.

## Tecnologias

- .NET 10 e ASP.NET Core Web API
- PostgreSQL e Entity Framework Core
- JWT Bearer Authentication
- OpenAPI, Scalar e xUnit
- Docker Compose para desenvolvimento e migrations

## Arquitetura

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
```

```text
src/ServiceLifeOS.Domain          Entidades e regras puras de dominio
src/ServiceLifeOS.Application     Casos de uso, DTOs e ports
src/ServiceLifeOS.Infrastructure  EF Core, PostgreSQL, JWT e adapters de saida
src/ServiceLifeOS.Api             Controllers, autenticacao e configuracao HTTP
tests/ServiceLifeOS.Tests         Testes de arquitetura, aplicacao e controllers
docs/                             Requisitos, contratos e documentacao operacional
```

Controllers apenas convertem HTTP em chamadas da camada Application. Regras de negocio e validacao ficam nas camadas Domain/Application; Infrastructure nao e referenciada pela API de dominio.

## Requisitos

- .NET SDK 10.
- Docker e Docker Compose para executar PostgreSQL localmente.
- Uma copia local de `.env.example` configurada com valores de desenvolvimento.

Nao versione arquivos `.env` reais, credenciais, chaves JWT ou connection strings de ambientes compartilhados.

## Configuracao Local

1. Copie `.env.example` para `.env`.
2. Defina os valores locais de conexao, JWT e usuario provisionado.
3. Suba apenas os servicos de infraestrutura definidos em `docker-compose.dev.yml`.
4. Execute a API pelo perfil de desenvolvimento do projeto.

Em desenvolvimento, a API aplica migrations pendentes e provisiona dados iniciais quando iniciada. O provisionamento e idempotente e nao substitui configuracoes existentes.

Os valores esperados pelo ambiente estao documentados em `docs/production.md`:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`, `Jwt__Audience` e `Jwt__Secret`
- `BootstrapUser__UserId`, `BootstrapUser__UserName`, `BootstrapUser__DisplayName` e `BootstrapUser__Password`
- `Cors__AllowedOrigins__0`
- Configuracoes de `RateLimiting`

## API

Toda rota, exceto `GET /health` e `POST /auth/login`, exige um token JWT Bearer valido. O `user-id` presente no token e a fonte de verdade para a propriedade dos recursos.

| Area | Rotas principais | Contrato |
| --- | --- | --- |
| Autenticacao | `POST /auth/login`, `GET /auth/me` | Codigo e OpenAPI |
| Usuario | `/users/me/preferences`, `/users/me/password`, `/users/me/sessions/others` | `docs/users.md` |
| Operacoes | `GET /operations/audit-logs` | Codigo e OpenAPI |
| Financas | `/finances/categories`, `/finances/transactions`, `/finances/recurrences`, `/finances/installment-purchases`, `/finances/reports` | `docs/finances.md` |
| Habitos | `/habits` | `docs/habits.md` |
| Saude | `GET /health` | `docs/production.md` |

Em desenvolvimento, a especificacao OpenAPI esta em `/openapi/v1.json` e a interface Scalar em `/scalar`.

## Dados e Persistencia

A migration inicial cria o modelo completo do MVP. As entidades usam UUIDs gerados pela aplicacao e pertencem diretamente ao usuario por `UserId`; nao ha tenant, contas financeiras ou dados de exemplo.

No provisionamento inicial, a aplicacao cria somente dados ausentes: preferencias, categorias financeiras, regras de XP, progressao de nivel e badges. Consulte `docs/entities.md` para entidades, relacionamentos, convencoes de datas e exclusao logica.

Em producao, migrations sao uma operacao manual anterior ao deploy. O procedimento e o comando Docker estao em `docs/production.md`.

## Qualidade

Execute verificacoes locais antes de enviar alteracoes:

```bash
dotnet build ServiceLifeOS.slnx
dotnet test ServiceLifeOS.slnx
```

Os testes cobrem dependencias entre camadas, regras de usuario e operacoes, regras financeiras criticas e adaptadores HTTP. Testes de integracao com PostgreSQL e de ponta a ponta com JWT ainda nao fazem parte da suite.

## Documentacao

- `docs/requirements.md`: escopo, requisitos e regras de negocio do MVP.
- `docs/entities.md`: entidades, relacionamentos, convencoes e dados iniciais.
- `docs/architecture.md`: camadas, dependencias e ports.
- `docs/users.md`: contrato de perfil, preferencias, senha e sessoes.
- `docs/finances.md`: contrato e regras operacionais de financas.
- `docs/habits.md`: contrato e regras operacionais de habitos.
- `docs/production.md`: operacao na Render, Supabase, variaveis e migrations.

## Seguranca e Operacao

- Senhas sao armazenadas com PBKDF2, nunca em texto puro.
- Sessoes sao validadas em cada requisicao autenticada e podem ser revogadas.
- A API aplica rate limiting, CORS configuravel e headers de seguranca.
- OpenAPI e Scalar nao sao expostos em producao.
- O endpoint de saude nao acessa o banco de dados.
- Logs e auditoria nao devem conter senhas, tokens ou outros valores sensiveis.

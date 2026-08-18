# ServiceLifeOS

API REST de um LifeOS pessoal para organizar finanças, hábitos, treinos, metas e gamificação. O projeto foi construído como ferramenta de uso diário e, principalmente, como laboratório pessoal para estudar modelagem de domínio, arquitetura em camadas, segurança de API, persistência relacional e evolução de um backend realista.

O sistema parte de um único proprietário provisionado pelo ambiente. Embora os dados pertençam a um `UserId`, multi-tenancy, cadastro público e colaboração não fazem parte do escopo atual.

## Domínios

| Domínio | Responsabilidade principal |
| --- | --- |
| Autenticação e usuário | Login, sessões, senha e preferências pessoais. |
| Finanças | Categorias, orçamentos versionados por mês, lançamentos, recorrências, parcelamentos e relatórios. |
| Hábitos | Agendas, conclusões, ofensivas, pendências e lembretes internos. |
| Treinos | Catálogo de exercícios, fichas, sessões e progressão por carga e volume. |
| Gamificação | Ledger de XP, nível, metas, badges e reconciliação de efeitos derivados. |
| Operações | Auditoria de ações sensíveis e endpoint de saúde. |

## Arquitetura

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
```

```text
src/ServiceLifeOS.Domain          Entidades, enums e regras puras de domínio
src/ServiceLifeOS.Application     Casos de uso, DTOs, ports e serviços de aplicação
src/ServiceLifeOS.Infrastructure  EF Core, PostgreSQL, JWT, hash de senha e repositórios
src/ServiceLifeOS.Api             Controllers, autenticação, middleware e configuração HTTP
tests/ServiceLifeOS.Tests         Testes de arquitetura, aplicação e adaptadores HTTP
docs/                             Requisitos, contratos, fluxos e operação
```

### Direção das dependências

- `Domain` não depende de framework, banco de dados ou HTTP.
- `Application` define os casos de uso e as interfaces que precisa para persistir ou integrar recursos externos.
- `Infrastructure` implementa essas interfaces com EF Core, PostgreSQL, JWT e PBKDF2.
- `Api` traduz HTTP em chamadas de aplicação; controllers não concentram regra de negócio.

Essa separação permite testar regras com repositórios falsos, trocar adaptadores de infraestrutura com impacto limitado e manter os casos de uso independentes de detalhes de transporte.

## Decisões Técnicas

- **.NET 10 e ASP.NET Core:** base da API e pipeline HTTP.
- **PostgreSQL e EF Core:** persistência relacional com migrations versionadas.
- **UUIDs gerados pela aplicação:** identificadores independentes do banco e seguros para exposição em rotas.
- **`DateOnly` para datas de negócio:** hábitos, meses financeiros e metas respeitam `America/Sao_Paulo`; timestamps técnicos usam UTC.
- **Exclusão lógica e arquivamento:** históricos financeiros, de hábitos e treinos permanecem consistentes; catálogos reutilizáveis preservam referências.
- **Ledger de XP:** grants, reversões e ajustes formam o histórico imutável; XP total, nível e badges são derivados desse estado.
- **Orçamentos com vigência mensal:** uma alteração cria uma nova versão aplicável a partir do mês atual, sem alterar limites de meses anteriores.

## Fluxo de Dados

1. O frontend envia uma requisição autenticada.
2. O middleware valida JWT e confirma que a sessão ainda está ativa.
3. O controller extrai apenas a entrada HTTP e o usuário das claims.
4. Um serviço da camada Application valida regras, consulta ports e altera entidades.
5. Repositórios da Infrastructure persistem os dados em uma unidade de trabalho.
6. Quando um evento afeta gamificação, o sistema reconcilia XP, metas e badges derivados.
7. A API retorna DTOs sem expor entidades de persistência.

## Principais Jornadas

| Jornada | Entrada do usuário | Resultado esperado |
| --- | --- | --- |
| Registrar despesa | Categoria, valor, data, situação e pagamento | Lançamento persistido; saldo e orçamento mudam apenas quando confirmado. |
| Concluir hábito | Data da conclusão | Progresso e ofensiva atualizados; XP e meta semanal são recalculados. |
| Concluir treino | Séries realizadas | Sessão entra no histórico, alimenta progressão e gera XP. |
| Criar meta | Tipo, alvo, unidade e fonte opcional | Progresso manual ou automático, com conclusão e reversão derivadas. |
| Consultar dashboard | Nenhuma entrada adicional | Resumo do mês, pendências, treinos recentes e perfil de gamificação. |

O passo a passo completo de entradas, chamadas e respostas esta em [`docs/user-flows.md`](docs/user-flows.md).

## API

Todas as rotas, exceto `GET /health` e `POST /auth/login`, exigem um token JWT Bearer. O `user-id` do token é a fonte de verdade para propriedade de recursos.

| Area | Rotas principais | Documento |
| --- | --- | --- |
| Autenticação | `POST /auth/login`, `POST /auth/refresh`, `GET /auth/me` | Código e OpenAPI |
| Dashboard | `GET /dashboard` | [`docs/user-flows.md`](docs/user-flows.md) |
| Usuário | `/users/me/preferences`, `/users/me/password`, `/users/me/sessions/others` | [`docs/users.md`](docs/users.md) |
| Operações | `GET /operations/audit-logs` | Código e OpenAPI |
| Finanças | `/finances/categories`, `/finances/transactions`, `/finances/recurrences`, `/finances/installment-purchases`, `/finances/reports` | [`docs/finances.md`](docs/finances.md) |
| Hábitos | `/habits`, `/habits/reminders` | [`docs/habits.md`](docs/habits.md) |
| Treinos | `/workouts` | [`docs/workouts.md`](docs/workouts.md) |
| Gamificação | `/gamification/profile`, `/gamification/goals`, `/gamification/ledger`, `/gamification/xp-rules`, `/gamification/badges` | [`docs/gamification.md`](docs/gamification.md) |
| Saúde | `GET /health` | [`docs/production.md`](docs/production.md) |

Em desenvolvimento, a especificação OpenAPI está em `/openapi/v1.json` e a interface Scalar em `/scalar`.

## Persistência e Dados Iniciais

Cada entidade funcional pertence diretamente ao usuário autenticado. O bootstrap cria somente dados ausentes para o usuário provisionado:

- Preferência inicial de carga em kg.
- Categorias financeiras iniciais de receita e despesa.
- Regras padrão de XP e progressão de nível.
- Catálogo inicial de badges.

O processo é idempotente: não duplica nem substitui configurações existentes. O modelo completo, relacionamentos, convenções de data e estratégias de exclusão estão em [`docs/entities.md`](docs/entities.md).

## Requisitos Locais

- .NET SDK 10.
- Docker e Docker Compose para o PostgreSQL local.
- Um arquivo `.env` criado a partir de `.env.example`.

Não versione `.env`, connection strings, segredos JWT ou credenciais reais.

## Configuração

1. Copie `.env.example` para `.env`.
2. Configure conexão, JWT, usuário provisionado, CORS, rate limiting e política de senha.
3. Suba somente a infraestrutura definida em `docker-compose.dev.yml`.
4. Execute a API pelo perfil de desenvolvimento do projeto.

Em desenvolvimento, a API aplica migrations pendentes e executa o bootstrap. Em produção, migrations são uma operação manual anterior ao deploy.

Variaveis relevantes:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Secret`
- `Jwt__AccessTokenExpirationMinutes`, `Jwt__RefreshTokenExpirationDays`
- `BootstrapUser__UserId`, `BootstrapUser__UserName`, `BootstrapUser__DisplayName`, `BootstrapUser__Password`
- `Cors__AllowedOrigins__0`
- `RateLimiting__LoginPermitLimit`, `RateLimiting__LoginWindowMinutes`
- `RateLimiting__RefreshPermitLimit`, `RateLimiting__RefreshWindowMinutes`
- `RateLimiting__ApiPermitLimit`, `RateLimiting__ApiWindowMinutes`
- `PasswordPolicy__MinimumLength`

Consulte [`docs/production.md`](docs/production.md) para configuracao operacional e migrations.

## Seguranca e Operacao

- Senhas usam PBKDF2; nunca sao persistidas em texto puro.
- Cada requisicao autenticada valida a sessao persistida; outras sessoes podem ser revogadas.
- A API aplica rate limiting, CORS configuravel, HSTS em producao e headers de seguranca.
- Auditoria registra autenticacao e alteracoes relevantes sem incluir senhas ou tokens.
- Excecoes nao tratadas sao registradas com contexto de rota.
- A API mede quantidade, duracao e falhas de requisicoes por meio de metricas internas.
- OpenAPI e Scalar ficam restritos ao ambiente de desenvolvimento.
- `GET /health` nao acessa o banco de dados.

## Qualidade

Execute antes de alterar ou enviar codigo:

```bash
dotnet build ServiceLifeOS.slnx
dotnet test ServiceLifeOS.slnx
```

A suite atual cobre dependencias entre camadas, regras criticas de aplicacao e controllers. Testes de integracao com PostgreSQL e fluxos ponta a ponta com JWT ainda sao oportunidades de evolucao do projeto.

## Documentacao

- [`docs/requirements.md`](docs/requirements.md): escopo e regras de negocio do MVP.
- [`docs/architecture.md`](docs/architecture.md): camadas, dependencias e ports.
- [`docs/entities.md`](docs/entities.md): modelo de entidades e convencoes de persistencia.
- [`docs/user-flows.md`](docs/user-flows.md): jornadas do usuario e guia de integracao do frontend.
- [`docs/users.md`](docs/users.md): perfil, preferencias, senha e sessoes.
- [`docs/finances.md`](docs/finances.md): financas, orcamentos e relatorios.
- [`docs/habits.md`](docs/habits.md): agendas, conclusoes, progresso e lembretes.
- [`docs/workouts.md`](docs/workouts.md): catalogo, fichas, sessoes e progressao.
- [`docs/gamification.md`](docs/gamification.md): metas, XP, niveis e badges.
- [`docs/production.md`](docs/production.md): deploy, variaveis e procedimento de migration.

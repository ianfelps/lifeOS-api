# ServiceLifeOS

API do ServiceLifeOS, um sistema pessoal para financas, habitos, treinos, metas e gamificacao.

## Documentacao

- `docs/requirements.md`: escopo e regras de negocio do MVP.
- `docs/entities.md`: entidades, relacionamentos, convencoes e dados iniciais.
- `docs/architecture.md`: camadas e dependencias da aplicacao.

## Tecnologias

- .NET 10 e ASP.NET Core Web API
- PostgreSQL e Entity Framework Core
- JWT Bearer Authentication
- OpenAPI e xUnit

## Estrutura

```text
src/ServiceLifeOS.Domain          Entidades e regras de dominio
src/ServiceLifeOS.Application     Casos de uso, DTOs e ports
src/ServiceLifeOS.Infrastructure  Persistencia, PostgreSQL e JWT
src/ServiceLifeOS.Api             Controllers e configuracao HTTP
tests/ServiceLifeOS.Tests         Testes de arquitetura e dominio
docs/                             Documentacao do produto e do modelo
```

## Persistencia

A migration inicial cria o modelo completo do MVP, sem tabelas de exemplo ou tenant. As novas entidades usam UUIDs gerados pela aplicacao e pertencem ao usuario por `UserId`.

No startup, o inicializador cria dados padrao ausentes para o usuario provisionado: preferencias, categorias financeiras, regras de XP, progressao de nivel e badges. O processo e idempotente e nao substitui configuracoes existentes.

## Verificacao

```bash
dotnet build ServiceLifeOS.slnx
dotnet test ServiceLifeOS.slnx
```

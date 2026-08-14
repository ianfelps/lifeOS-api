# Arquitetura do ServiceLifeOS

ServiceLifeOS usa uma separacao simples de ports and adapters para um unico proprietario.

```text
Api -> Application -> Domain
Infrastructure -> Application + Domain
```

## Domain

Contem entidades e regras puras para financas, habitos, treinos, metas, gamificacao e seguranca de sessoes. Consulte `docs/entities.md` para o modelo completo.

## Application

Contem casos de uso, DTOs e ports. Ela nao referencia EF Core nem ASP.NET.

Ports principais:

- `ITokenService`
- `IUnitOfWork`
- `ICurrentUser`
- `ICurrentTenant`
- `IUserRepository`
- `IPasswordHasher`
- `IFinanceRepository`

## Infrastructure

Implementa adapters de saida:

- EF Core/PostgreSQL para entidades do dominio e `AppUser`
- JWT para emissao de token
- PBKDF2 para hash e verificacao de senha
- Repositorio EF Core para categorias, orcamentos, lancamentos, recorrencias, parcelamentos e ledger financeiro de XP

Senhas nao sao armazenadas em texto puro. O seed de desenvolvimento le a senha demo da configuracao apenas para criar o hash inicial em `users.password_hash`; o login valida o hash persistido no banco.

## Api

Implementa adapters de entrada:

- `AuthController`
- `FinancesController`
- `HabitsController`
- `WorkoutsController`

Controllers convertem HTTP para chamadas de Application. Regras de negocio devem ficar em Domain/Application, nao nos controllers.

O `user-id` do JWT e a fonte da verdade para identidade. Entidades de dominio pertencem diretamente a esse usuario por `UserId`.

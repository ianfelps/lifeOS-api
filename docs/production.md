# Producao

## Arquitetura

O ambiente de producao usa um Web Service Docker da Render chamado `lifeOS-api`, na regiao Oregon e plano Free. O PostgreSQL e fornecido pelo Supabase via Session Pooler. A Render encerra TLS e encaminha requisicoes HTTPS para a API.

O deploy ocorre automaticamente a partir da branch `main`. O `render.yaml` declara apenas valores publicos e nomes de variaveis privadas; segredos nunca sao versionados.

## Variaveis de ambiente

Copie `.env.example` para `.env` somente no ambiente local. O arquivo `.env` e ignorado pelo Git.

| Variavel | Uso |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | Connection string do Supabase Session Pooler com SSL. |
| `Jwt__Issuer` | Emissor do JWT. |
| `Jwt__Audience` | Audiencia do JWT. |
| `Jwt__Secret` | Segredo com no minimo 32 caracteres. |
| `Jwt__AccessTokenExpirationMinutes` | Validade do access token, com padrao de 15 minutos. |
| `Jwt__RefreshTokenExpirationDays` | Validade do refresh token rotativo, com padrao de 30 dias. |
| `BootstrapUser__UserId` | Identificador estavel da conta unica. |
| `BootstrapUser__UserName` | Nome de usuario inicial. |
| `BootstrapUser__DisplayName` | Nome exibido. |
| `BootstrapUser__Password` | Senha inicial. |
| `Cors__AllowedOrigins__0` | Origem HTTPS permitida, atualmente `https://lifeos.vercel.app`. |
| `RateLimiting__LoginPermitLimit` | Limite de login por IP. |
| `RateLimiting__LoginWindowMinutes` | Janela do limite de login. |
| `RateLimiting__ApiPermitLimit` | Limite global da API por IP. |
| `RateLimiting__ApiWindowMinutes` | Janela do limite global. |
| `RateLimiting__RefreshPermitLimit` | Limite de renovacoes de sessao por IP. |
| `RateLimiting__RefreshWindowMinutes` | Janela do limite de renovacoes por IP. |
| `PasswordPolicy__MinimumLength` | Comprimento minimo da senha, com padrao de 12. |

Na Render, configure os valores privados no Dashboard do servico. Nunca envie um `.env` real ao repositorio.

## Bootstrap

Em producao, a API recusa iniciar se a connection string, a origem CORS ou os dados de `BootstrapUser` estiverem ausentes. Quando a conta ainda nao existe, o bootstrap cria o usuario e seus dados iniciais. Em execucoes posteriores, o processo nao duplica categorias, configuracoes ou badges existentes.

## Migrations

O plano Free da Render nao oferece `preDeployCommand`. Por isso, aplique migrations manualmente antes de cada deploy:

```bash
docker compose --env-file .env -f docker-compose.migrate.yml run --rm migrations
```

Esse comando usa o alvo `migrations` do `Dockerfile`, que contem o SDK e `dotnet ef`. A factory de design-time le `ConnectionStrings__DefaultConnection` do ambiente, portanto o comando usa a mesma conexao configurada no `.env`. A imagem da API publicada usa apenas o runtime .NET e nao executa migrations no startup.

## Runtime

- `GET /health` e publico e nao testa o banco; configure este caminho como health check da Render.
- OpenAPI (`/openapi/v1.json`) e Scalar (`/scalar`) ficam disponiveis apenas em desenvolvimento.
- CORS aceita somente as origens configuradas em `Cors__AllowedOrigins`.
- Login aceita 10 requisicoes por IP a cada 15 minutos.
- A API aceita 300 requisicoes por IP por minuto.
- HSTS, headers de seguranca e suporte a headers encaminhados pela proxy da Render sao habilitados em producao.

## Supabase

Use a connection string do Session Pooler com SSL para `ConnectionStrings__DefaultConnection`. Mantenha as credenciais somente no Dashboard da Render e no `.env` local ignorado. Realize backups pelo painel do Supabase conforme o procedimento operacional do projeto.

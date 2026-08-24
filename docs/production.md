# Producao

## Arquitetura

O ambiente de producao executa a API em uma VPS Ubuntu 24.04 ARM64. O GitHub Actions testa, gera imagens Docker ARM64 no GitHub Container Registry (GHCR) e faz o deploy por SSH. O Nginx encerra TLS e encaminha requisicoes para a API vinculada exclusivamente a `127.0.0.1:3001`.

O PostgreSQL continua externo. A VPS nao executa banco de dados, nao armazena codigo-fonte da aplicacao e nao expoe a porta do container publicamente.

O deploy ocorre apenas quando uma pull request interna de `development` e mesclada em `main`. O workflow publica imagens imutaveis identificadas pelo SHA do commit, aplica migrations com a imagem correspondente e somente entao atualiza a API.

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

Crie `/opt/lifeos-api/.env.production` somente na VPS, com permissao `600` e propriedade do usuario de deploy. A API recusa iniciar em producao quando connection string ou JWT estiverem ausentes; ela nunca usa valores de demonstracao em producao. Nunca envie um `.env` real ao repositorio.

## Bootstrap

Em producao, a API recusa iniciar se a connection string, a origem CORS ou os dados de `BootstrapUser` estiverem ausentes. Quando a conta ainda nao existe, o bootstrap cria o usuario e seus dados iniciais. Em execucoes posteriores, o processo nao duplica categorias, configuracoes ou badges existentes.

## Migrations

O workflow executa migrations automaticamente antes de atualizar a API. A imagem de migrations contem o SDK e `dotnet ef`; a imagem de runtime contem somente o runtime .NET e nao executa migrations no startup.

Para executar uma migration manualmente em uma emergencia, use a mesma tag publicada no deploy:

```bash
MIGRATIONS_IMAGE=ghcr.io/OWNER/lifeos-api-migrations:COMMIT_SHA \
docker compose --env-file .env.production -f docker-compose.migrate.production.yml \
  run --rm migrations
```

## Bootstrap manual

O deploy normal nunca cria usuarios ou dados iniciais. Depois de aplicar as migrations, crie ou atualize o usuario provisionado de forma manual:

```bash
APP_IMAGE=ghcr.io/OWNER/lifeos-api:COMMIT_SHA \
docker compose --env-file .env.production -f docker-compose.bootstrap.production.yml \
  run --rm bootstrap
```

O comando exige `BootstrapUser__UserId`, `BootstrapUser__UserName`, `BootstrapUser__DisplayName` e `BootstrapUser__Password` no `.env.production`. Ele e idempotente: cria o usuario e os dados iniciais ausentes, sem remover registros existentes. O log final informa se o usuario foi criado ou atualizado. O alvo `bootstrap` encerra quando a operacao termina e nao inicia um servidor HTTP.

## Runtime

- `GET /health` e publico, nao testa o banco e responde sem cache; o workflow o consulta por `127.0.0.1:3001` apos cada deploy.
- O container escuta `PORT` quando fornecida, com fallback em `8080`. O Compose publica somente `127.0.0.1:3001` para o Nginx.
- A imagem desabilita recarga de arquivos de configuracao para evitar watchers `inotify`; alteracoes de configuracao exigem novo deploy.
- OpenAPI (`/openapi/v1.json`) e Scalar (`/scalar`) ficam disponiveis apenas em desenvolvimento.
- CORS aceita somente as origens configuradas em `Cors__AllowedOrigins`.
- Login aceita 10 requisicoes por IP a cada 15 minutos.
- A API aceita 300 requisicoes por IP por minuto.
- HSTS, headers de seguranca e suporte a headers encaminhados pelo Nginx sao habilitados em producao.

## Supabase

Use a connection string do Session Pooler com SSL para `ConnectionStrings__DefaultConnection`. Mantenha as credenciais somente no `.env.production` da VPS e no `.env` local ignorado. Realize backups pelo painel do Supabase conforme o procedimento operacional do projeto.

## GitHub Actions

Configure os seguintes secrets no repositorio:

| Secret | Uso |
| --- | --- |
| `VPS_HOST` | IP publico ou hostname da VPS. |
| `VPS_USER` | Usuario restrito de deploy, por exemplo `lifeos-api-deploy`. |
| `VPS_SSH_PORT` | Porta SSH da VPS, normalmente `22`. |
| `VPS_SSH_PRIVATE_KEY` | Conteudo completo da chave privada exclusiva do GitHub Actions. |
| `VPS_SSH_KNOWN_HOSTS` | Chave publica do host, obtida com `ssh-keyscan -H HOSTNAME`. |

O `GITHUB_TOKEN` temporario faz login no GHCR durante o deploy. Nenhum token de registry e persistido na VPS.

## Operacao

```bash
sudo -u lifeos-api-deploy -H docker compose \
  --env-file /opt/lifeos-api/.env.production \
  -f /opt/lifeos-api/docker-compose.production.yml ps
```

```bash
sudo -u lifeos-api-deploy -H docker compose \
  --env-file /opt/lifeos-api/.env.production \
  -f /opt/lifeos-api/docker-compose.production.yml logs --tail 100
```

O arquivo `render.yaml` e legado da hospedagem anterior. Mantenha o servico Render ativo ate validar a VPS e desative-o pelo Dashboard da Render para evitar deploys paralelos.

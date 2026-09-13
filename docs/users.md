# Usuario

Exceto por `POST /auth/register`, as rotas deste documento exigem JWT Bearer. A identidade e a sessao atual sao obtidas exclusivamente das claims do token; a API nao aceita identificador de usuario informado pelo cliente.

Erros de validacao respondem `400`, credenciais invalidas respondem `401` e recursos ausentes respondem `404`. As respostas de erro usam `{ "message": "..." }`.

## Cadastro inicial

| Metodo | Rota | Funcao |
| --- | --- | --- |
| POST | `/auth/register` | Cria a unica conta e seus dados iniciais quando nao ha usuarios ativos. |

Exemplo:

```json
{
  "userName": "ian",
  "displayName": "Ian",
  "password": "a-strong-password"
}
```

A senha precisa respeitar `PasswordPolicy:MinimumLength`. O endpoint retorna `201` com `userId`, `userName` e `displayName`. Depois da primeira conta, retorna `409`.

## Preferencias

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/users/me/preferences` | Consulta a preferencia de unidade de carga |
| PUT | `/users/me/preferences` | Atualiza a preferencia de unidade de carga |

Exemplo de atualizacao:

```json
{
  "preferredWeightUnit": "Pounds"
}
```

As unidades aceitas sao `Kilograms` e `Pounds`. A preferencia apenas sugere a unidade inicial em uma serie de treino; cada serie pode manter sua propria unidade.

Resposta:

```json
{
  "preferredWeightUnit": "Pounds"
}
```

## Identidade

| Metodo | Rota | Funcao |
| --- | --- | --- |
| PUT | `/users/me/identity` | Atualiza o nome de usuario e o nome de exibicao da conta autenticada |

Exemplo de atualizacao:

```json
{
  "userName": "ian",
  "displayName": "Ian Felipe"
}
```

Os dois campos sao obrigatorios apos remover espacos nas extremidades. O nome de usuario aceita no maximo 120 caracteres, o nome de exibicao aceita no maximo 160 e o nome de usuario deve ser unico, inclusive entre contas inativas. Um nome de usuario indisponivel retorna `409`.

A atualizacao preserva as sessoes ativas. Tokens emitidos antes da alteracao podem manter os nomes antigos nas claims ate a proxima renovacao, mas a consulta de identidade retorna imediatamente os valores atuais. A alteracao gera um evento `Updated` em `AuditLog`, sem registrar senha, token ou outros dados sensiveis.

## Senha

| Metodo | Rota | Funcao |
| --- | --- | --- |
| PUT | `/users/me/password` | Altera a senha e encerra as demais sessoes ativas |

Exemplo de atualizacao:

```json
{
  "currentPassword": "current-password",
  "newPassword": "new-password"
}
```

A senha atual e obrigatoria e precisa ser valida. A nova senha precisa respeitar `PasswordPolicy:MinimumLength`, configurado no ambiente e com padrao de 12 caracteres. A senha nunca e registrada em auditoria, logs ou respostas HTTP.

Ao alterar a senha, a API preserva a sessao do token atual e revoga todas as demais sessoes ainda ativas do usuario.

## Sessoes

| Metodo | Rota | Funcao |
| --- | --- | --- |
| DELETE | `/users/me/sessions/others` | Revoga todas as sessoes ativas, exceto a atual |

Resposta:

```json
{
  "revokedSessionCount": 2
}
```

Tokens de sessoes revogadas deixam de autorizar novas requisicoes. A sessao atual nao e afetada.

## Auditoria

As alteracoes de preferencia, senha e sessoes registram eventos em `AuditLog`. Os eventos usados sao `Updated` para preferencias, `PasswordChanged` para alteracao de senha e `SessionsRevoked` para revogacao de sessoes. A consulta paginada desses registros esta em `GET /operations/audit-logs`.

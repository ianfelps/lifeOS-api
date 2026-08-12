# Usuario

As rotas deste documento exigem JWT Bearer. A identidade e a sessao atual sao obtidas exclusivamente das claims do token; a API nao aceita identificador de usuario informado pelo cliente.

Erros de validacao respondem `400`, credenciais invalidas respondem `401` e recursos ausentes respondem `404`. As respostas de erro usam `{ "message": "..." }`.

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

A senha atual e obrigatoria e precisa ser valida. A nova senha e validada pela politica configurada no ambiente quando essa politica for disponibilizada pela aplicacao. A senha nunca e registrada em auditoria, logs ou respostas HTTP.

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

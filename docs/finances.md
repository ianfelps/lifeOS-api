# Financas

Todas as rotas exigem JWT Bearer e usam o usuario presente no token. Recursos de outro usuario respondem `404`. Erros de validacao respondem `400`, conflitos com historico respondem `409` e todos usam `{ "message": "..." }`.

## Convencoes

- Valores monetarios usam BRL e `decimal(18,2)`.
- Datas usam `yyyy-MM-dd`; meses sao normalizados para o primeiro dia.
- Dados realizados e orcamentos usam apenas transacoes `Confirmed`.
- Projecoes usam transacoes `Confirmed` e `Planned`.
- Uma planejada passada e retornada como `Overdue`, mas continua persistida como `Planned` ate confirmacao, edicao ou exclusao.
- Exclusao de transacao e logica. Categorias sao arquivadas para preservar referencias.

## Categorias e orcamentos

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/finances/categories?includeArchived=false` | Lista categorias |
| POST | `/finances/categories` | Cria categoria |
| PUT | `/finances/categories/{categoryId}` | Edita categoria |
| DELETE | `/finances/categories/{categoryId}` | Arquiva categoria |
| GET | `/finances/categories/{categoryId}/budget?month=2026-08-01` | Consulta teto recorrente ou excecao mensal |
| PUT | `/finances/categories/{categoryId}/budget` | Define teto recorrente de categoria de despesa |
| PUT | `/finances/categories/{categoryId}/budget-overrides/{month}` | Define excecao mensal |
| DELETE | `/finances/categories/{categoryId}/budget-overrides/{month}` | Remove excecao mensal e restaura o teto recorrente |

Uma categoria precisa estar ativa e ter o mesmo tipo da transacao. Alertas retornados por relatorio sao `None`, `EightyPercent`, `AtLimit` e `Exceeded`; os limites sao, respectivamente, abaixo de 80%, de 80% a menos de 100%, exatamente 100% e acima de 100%.

## Transacoes

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/finances/transactions` | Lista paginada |
| POST | `/finances/transactions` | Cria lancamento simples |
| PUT | `/finances/transactions/{transactionId}` | Edita lancamento simples |
| POST | `/finances/transactions/{transactionId}/confirm` | Confirma lancamento |
| DELETE | `/finances/transactions/{transactionId}` | Exclui logicamente |

O filtro aceita `page`, `pageSize`, `from`, `to`, `categoryId`, `type`, `status`, `paymentMethod` e `sort`. As ordenacoes aceitas sao `date-desc`, `date-asc`, `amount-desc` e `amount-asc`.

Exemplo de criacao:

```json
{
  "categoryId": "00000000-0000-0000-0000-000000000000",
  "amount": 120.50,
  "transactionDate": "2026-08-12",
  "type": "Expense",
  "paymentMethod": "Pix",
  "status": "Planned",
  "description": "Internet"
}
```

`InstallmentCredit` nao pode ser usado em transacao simples; use compras parceladas.

## Recorrencias

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/finances/recurrences` | Lista regras mensais |
| POST | `/finances/recurrences` | Cria regra e ocorrencias ate o mes atual |
| PUT | `/finances/recurrences/{recurrenceId}` | Encerra a regra anterior e cria sucessora para ocorrencias futuras |
| POST | `/finances/recurrences/{recurrenceId}/end` | Encerra a serie sem alterar historico |

As ocorrencias sao materializadas de forma idempotente em consultas e operacoes financeiras. Cada ocorrencia inicia planejada e pode ser confirmada individualmente.

## Compras parceladas

| Metodo | Rota | Funcao |
| --- | --- | --- |
| POST | `/finances/installment-purchases` | Cria compra e parcelas mensais |
| GET | `/finances/installment-purchases/{purchaseId}` | Consulta compra e parcelas ativas |
| PUT | `/finances/installment-purchases/{purchaseId}` | Edita compra e substitui somente parcelas futuras nao confirmadas |

O valor e dividido em parcelas mensais e a primeira absorve o residuo de centavos. Parcelas confirmadas sao imutaveis. A alteracao nao pode reduzir o total abaixo do confirmado nem definir menos parcelas que as ja confirmadas.

## Relatorios

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/finances/reports/monthly-summary?month=2026-08-01` | Receitas, despesas e saldo realizado/projetado |
| GET | `/finances/reports/monthly-comparison?from=2026-01-01&to=2026-08-01` | Serie de resumos mensais |
| GET | `/finances/reports/cash-flow-projection?from=2026-01-01&to=2026-08-01` | Serie mensal de valores realizados e projetados |
| GET | `/finances/reports/category-spending?month=2026-08-01` | Gasto, teto, saldo, percentual e alerta por categoria |

## Auditoria e gamificacao

Criacao, edicao, arquivamento e exclusao financeira geram `AuditLog` com snapshots. Uma transacao confirmada recebe uma entrada `Grant` no `XpLedgerEntry` conforme a regra `TransactionConfirmed`. Quando deixa de estar confirmada ou e excluida, recebe uma entrada `Reversal` vinculada ao grant original. O ledger preserva o historico das concessoes e reversoes.

Badges cujos criterios sejam exclusivamente `TransactionConfirmationCount` sao reavaliados a cada alteracao de transacao. Um badge e removido quando uma exclusao ou desconfirmacao faz o usuario deixar de atender seus criterios. Nivel e badges com criterios de outros dominios permanecem valores derivados pelo futuro modulo de Gamificacao; o modelo atual nao persiste um nivel de usuario nem possui caso de uso para expor esse perfil.

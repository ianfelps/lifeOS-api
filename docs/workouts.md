# Treinos

Todas as rotas exigem JWT Bearer e usam o usuario presente no token. Recursos de outro usuario respondem `404`. Erros de validacao respondem `400`, conflitos de estado respondem `409` e todos usam `{ "message": "..." }`.

## Exercicios

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/workouts/exercises?includeArchived=false` | Lista catalogo |
| POST | `/workouts/exercises` | Cria exercicio |
| PUT | `/workouts/exercises/{exerciseId}` | Edita exercicio |
| DELETE | `/workouts/exercises/{exerciseId}` | Arquiva exercicio |

Exercicios arquivados preservam fichas e sessoes existentes, mas nao podem ser incluidos em novas fichas.

## Fichas

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/workouts/sheets?includeArchived=false` | Lista fichas |
| POST | `/workouts/sheets` | Cria ficha |
| GET | `/workouts/sheets/{sheetId}` | Consulta ficha |
| PUT | `/workouts/sheets/{sheetId}` | Edita ficha |
| DELETE | `/workouts/sheets/{sheetId}` | Arquiva ficha |

Uma ficha possui exercicios ordenados e cada exercicio deve possuir ao menos uma serie planejada com repeticoes positivas.

```json
{
  "name": "Upper body",
  "exercises": [
    { "exerciseId": "00000000-0000-0000-0000-000000000000", "sets": [{ "targetRepetitions": 10 }, { "targetRepetitions": 8 }] }
  ]
}
```

## Sessoes

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/workouts/sessions` | Lista paginada |
| POST | `/workouts/sessions` | Inicia sessao por ficha ou avulsa |
| GET | `/workouts/sessions/{sessionId}` | Consulta sessao |
| PUT | `/workouts/sessions/{sessionId}` | Salva sessao rascunho ou concluida |
| POST | `/workouts/sessions/{sessionId}/complete` | Conclui sessao |
| POST | `/workouts/sessions/{sessionId}/cancel` | Cancela sessao |
| DELETE | `/workouts/sessions/{sessionId}` | Exclui logicamente sessao |

`POST /workouts/sessions` aceita `workoutSheetId` ou `exercises`. Ao iniciar por ficha, exercicios, series e repeticoes planejadas sao copiados como snapshot. Uma sessao avulsa aceita nome livre por exercicio.

```json
{
  "exercises": [
    {
      "exerciseName": "Barbell row",
      "sets": [
        { "weight": 60, "weightUnit": "Kilograms", "repetitions": 10 }
      ]
    }
  ]
}
```

O filtro de sessoes aceita `page`, `pageSize`, `from`, `to`, `workoutSheetId` e `status`. Sessoes canceladas ou excluidas nao entram no historico nem na progressao. Carga e unidade devem ser informadas juntas; cada serie pode usar `Kilograms` ou `Pounds` independentemente das demais. A preferencia do usuario apenas sugere a unidade inicial das series copiadas de uma ficha.

## Progressao

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/workouts/progress/exercises/{exerciseId}` | Consulta progressao por exercicio |

A resposta contem carga maxima, melhor serie e volume por sessao concluida. O volume e `carga * repeticoes`. Valores de `Kilograms` e `Pounds` sao retornados separadamente, sem conversao automatica.

## Auditoria e gamificacao

Criacao, edicao, arquivamento, conclusao, cancelamento e exclusao registram `AuditLog`. Uma sessao concluida recebe uma entrada `Grant` conforme a regra `WorkoutCompleted`. Cancelamento ou exclusao cria uma `Reversal` vinculada ao grant ativo. Badges cujos criterios sejam exclusivamente `WorkoutCompletionCount` sao reavaliados a cada alteracao de elegibilidade.

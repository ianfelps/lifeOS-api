# Gamificacao

Todas as rotas exigem JWT Bearer e usam o usuario presente no token. Recursos de outro usuario respondem `404`. Erros de validacao respondem `400`, conflitos de estado respondem `409` e todos usam `{ "message": "..." }`.

## Perfil e ledger

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/gamification/profile` | XP total, nivel, limiares e badges |
| GET | `/gamification/ledger` | Historico paginado de grants, reversoes e ajustes |

O ledger e a fonte de verdade para o XP. A soma de `amount` determina o XP total. Um grant revertido continua no historico, acompanhado de uma entrada `Reversal` negativa vinculada por `reversedEntryId`.

O nivel inicial e um. Cada avanco usa `baseXp + (nivel anterior - 1) * incrementPerLevel`. O perfil retorna os limiares acumulados do nivel atual e do proximo nivel.

O ledger aceita `page`, `pageSize`, `eventType`, `from` e `to`.

## Metas

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/gamification/goals` | Lista paginada |
| GET | `/gamification/goals/{goalId}` | Consulta uma meta |
| POST | `/gamification/goals` | Cria meta |
| PUT | `/gamification/goals/{goalId}` | Edita definicao e vinculos |
| PUT | `/gamification/goals/{goalId}/progress` | Atualiza progresso de meta livre |
| POST | `/gamification/goals/{goalId}/cancel` | Cancela meta |
| DELETE | `/gamification/goals/{goalId}` | Arquiva meta |

Os filtros aceitos sao `page`, `pageSize`, `includeArchived` e `status`. Cada meta possui `type`, `title`, `description`, `targetValue`, `unit`, `dueDate` opcional e `sources`.

- `Financial` nao recebe fonte e mede a sequencia atual de meses fechados positivos.
- `Habit` exige uma fonte `Habit` e mede a ofensiva atual do habito, por dia ou semana conforme a agenda.
- `Training` exige uma fonte `Exercise` ou `WorkoutSheet` e mede semanas consecutivas com uma sessao concluida correspondente.
- `FreeForm` nao recebe fonte e aceita apenas progresso manual.

Quando o progresso alcanca `targetValue`, a meta muda para `Completed` e concede `GoalCompleted`. Se uma correcao retroativa reduzir o progresso, ela retorna para `Active` e o grant e revertido. Metas canceladas nao sao recalculadas. `dueDate` e informativo no MVP: a interface pode destacar o vencimento, mas ele nao cancela nem altera automaticamente o progresso.

Exemplo de meta de sequencia de habito:

```json
{
  "type": "Habit",
  "title": "Leitura consistente",
  "targetValue": 14,
  "unit": "days",
  "sources": [
    {
      "sourceType": "Habit",
      "sourceId": "00000000-0000-0000-0000-000000000000"
    }
  ]
}
```

## Mes positivo

Um mes somente e avaliado apos encerrar. Ele e positivo quando, simultaneamente, receitas confirmadas excedem despesas confirmadas e nenhuma categoria com teto excede o valor aplicavel naquele mes. A reavaliacao ocorre depois de mudancas financeiras e cria ou reverte o evento `PositiveMonth` de forma idempotente.

## Configuracao

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET, PUT | `/gamification/xp-rules` | Consulta ou substitui valores de XP por evento |
| GET, PUT | `/gamification/level-progression` | Consulta ou altera `baseXp` e `incrementPerLevel` |
| GET | `/gamification/badges` | Lista badges bloqueados e desbloqueados |
| POST | `/gamification/badges` | Cria badge e criterios |
| PUT | `/gamification/badges/{badgeId}` | Edita badge e criterios |
| DELETE | `/gamification/badges/{badgeId}` | Arquiva badge |

Os criterios de um badge sao cumulativos: todos devem ser atendidos. Os tipos disponiveis sao `Xp`, `Level`, `HabitCompletionCount`, `WeeklyHabitGoalCount`, `WorkoutCompletionCount`, `TransactionConfirmationCount`, `GoalCompletionCount` e `PositiveMonthCount`. Criterios de conclusao de habito, treino, transacao e meta podem restringir a contagem ao respectivo `habitId`, `exerciseId`, `financialCategoryId` ou `goalId`. A API reconcilia desbloqueios apos cada evento relevante, removendo `UserBadge` quando uma reversao elimina a elegibilidade.

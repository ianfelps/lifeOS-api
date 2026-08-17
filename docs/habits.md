# Habitos

Todas as rotas exigem JWT Bearer e usam o usuario presente no token. Recursos de outro usuario respondem `404`. Erros de validacao respondem `400`, conflitos de estado respondem `409` e todos usam `{ "message": "..." }`.

## Convencoes

- Datas usam `yyyy-MM-dd` e respeitam `America/Sao_Paulo`.
- A janela para criar ou remover uma conclusao vai do dia atual aos sete dias anteriores.
- Semanas comecam na segunda-feira e terminam no domingo.
- Habitos pausados ou arquivados preservam o historico, mas nao sao pendencias e nao aceitam conclusoes.
- `DELETE /habits/{habitId}` arquiva o habito; nao remove dados historicos.

## Habitos

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/habits` | Lista paginada |
| POST | `/habits` | Cria habito |
| GET | `/habits/{habitId}` | Consulta habito |
| PUT | `/habits/{habitId}` | Edita habito e agenda |
| POST | `/habits/{habitId}/pause` | Pausa habito |
| POST | `/habits/{habitId}/resume` | Retoma habito |
| DELETE | `/habits/{habitId}` | Arquiva habito |

O filtro de listagem aceita `page`, `pageSize`, `includeArchived` e `status`.

Exemplo de agenda por dias da semana:

```json
{
  "title": "Read",
  "priority": "Medium",
  "schedule": {
    "type": "Weekdays",
    "weekdays": ["Monday", "Wednesday", "Friday"]
  }
}
```

Os tipos de agenda sao `Daily`, `Weekdays`, `WeeklyCount` e `DailyCount`. `Daily` e `Weekdays` possuem meta fixa de uma conclusao. `WeeklyCount` e `DailyCount` exigem `targetCount` positivo. Apenas `Weekdays` aceita `weekdays`, sem valores duplicados.

## Conclusoes e progresso

| Metodo | Rota | Funcao |
| --- | --- | --- |
| GET | `/habits/{habitId}/completions?from=2026-08-05&to=2026-08-12` | Consulta historico |
| POST | `/habits/{habitId}/completions` | Registra conclusao |
| DELETE | `/habits/{habitId}/completions/{completionId}` | Corrige conclusao por exclusao logica |
| GET | `/habits/{habitId}/progress?date=2026-08-12` | Consulta periodo, progresso e ofensiva |
| GET | `/habits/pending?date=2026-08-12` | Lista habitos pendentes no dia |
| GET | `/habits/reminders?date=2026-08-12` | Lista pendencias com titulo e progresso para lembretes na interface |

Exemplo de conclusao:

```json
{
  "completedOn": "2026-08-12"
}
```

Uma conclusao fora da agenda ou acima da meta do periodo nao e aceita. A ofensiva conta dias consecutivos para `Daily`, `Weekdays` e `DailyCount`; dias nao configurados em `Weekdays` sao ignorados. Para `WeeklyCount`, conta semanas consecutivas cumpridas.

## Auditoria e gamificacao

Criacao, edicao, pausa, retomada, arquivamento e alteracoes de conclusoes registram `AuditLog`. Cada conclusao valida concede o XP configurado para `HabitCompletion`; uma semana que atinge a agenda `WeeklyCount` concede o XP de `WeeklyHabitGoal`. Remover uma conclusao reverte os grants afetados. Badges compostos exclusivamente por `HabitCompletionCount` e `WeeklyHabitGoalCount` sao reavaliados a cada alteracao.

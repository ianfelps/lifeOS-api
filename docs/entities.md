# Modelo de Entidades

## Escopo

As entidades do MVP pertencem diretamente a `AppUser` por `UserId`. Todas as novas chaves usam UUID gerado pela aplicacao. O modelo nao usa tenant nem entidades de exemplo.

## Convencoes

- Tabelas e colunas usam `snake_case`.
- Valores monetarios usam `decimal(18,2)`.
- Transacoes financeiras, conclusoes de habitos e sessoes de treino usam exclusao logica por `deleted_at`.
- Categorias, habitos, exercicios, fichas, metas e badges usam arquivamento para preservar referencias historicas.
- Timestamps tecnicos usam UTC; datas de negocio usam `DateOnly` no contexto `America/Sao_Paulo`.

## Usuario e operacao

- `AppUser`: proprietario autenticado.
- `UserPreference`: relacao 1:1 com o usuario; contem a unidade de carga sugerida (`kg` ou `lb`).
- `UserSession`: sessoes persistidas, com identificador de token, expiracao, revogacao e ultimo uso.
- `AuditLog`: auditoria com ator, acao, recurso, data e snapshots anterior e posterior.

## Financas

- `FinancialCategory`: categoria de receita ou despesa, exclusiva de um tipo.
- `CategoryBudget` e `CategoryBudgetOverride`: teto recorrente e excecao mensal de uma categoria.
- `FinancialTransaction`: registro financeiro planejado, confirmado ou vencido.
- `RecurringTransaction`: regra de geracao mensal de transacoes futuras.
- `InstallmentPurchase`: compra parcelada raiz; suas parcelas sao `FinancialTransaction` vinculadas.

## Habitos

- `Habit`: habito com prioridade e estado ativo, pausado ou arquivado.
- `HabitSchedule`: agenda diaria, por dias da semana, contagem semanal ou contagem diaria.
- `HabitScheduleWeekday`: dias da semana de uma agenda especifica.
- `HabitCompletion`: ocorrencia concluida em uma data.

## Treinos

- `Exercise`: catalogo reutilizavel de exercicios.
- `WorkoutSheet`, `WorkoutSheetExercise` e `WorkoutSheetExerciseSet`: ficha e suas series planejadas individualmente.
- `WorkoutSession`, `WorkoutSessionExercise` e `WorkoutSessionSet`: execucao de treino. Exercicios e series da sessao sao snapshot independente da ficha. Cada serie com carga registra sua propria unidade, permitindo kg e lb no mesmo treino.

## Metas e gamificacao

- `Goal`: meta financeira, de habito, treino ou livre.
- `GoalSourceLink`: fontes polimorficas de uma meta com multiplos vinculos.
- `XpEventRule`: valor de XP por evento configuravel pelo usuario.
- `LevelProgressionRule`: progressao infinita com `base_xp` e `increment_per_level`.
- `Badge` e `BadgeCriterion`: badge e um ou mais criterios, todos obrigatorios para desbloqueio.
- `UserBadge`: registro de badge desbloqueado pelo usuario.
- `XpLedgerEntry`: razao imutavel de concessoes, reversoes e ajustes de XP.

## Dados iniciais

No startup, o inicializador cria dados somente quando inexistentes para cada usuario provisionado:

- Preferencia inicial de carga em `kg`; cada serie pode usar kg ou lb.
- Categorias de receita: Salario, Freelance, Investimentos, Reembolsos e Outras receitas.
- Categorias de despesa: Moradia, Alimentacao, Transporte, Saude, Educacao, Lazer, Assinaturas, Cuidados pessoais, Compras, Impostos e taxas e Outras despesas.
- XP: habito 5, meta semanal 20, treino 25, transacao confirmada 1, mes positivo 50 e meta concluida 40.
- Progressao de nivel: 100 XP para o primeiro avanco e acrescimo de 25 XP por nivel seguinte.
- 17 badges globais de habitos, metas semanais, treinos, transacoes, metas pessoais, mes positivo e nivel cinco.

O inicializador e idempotente: nao duplica dados e nao substitui configuracoes existentes.

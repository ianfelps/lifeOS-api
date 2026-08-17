# Fluxos e Casos de Uso

Este guia descreve as jornadas principais da API. Ele complementa os contratos detalhados de cada dominio e serve como referencia para a implementacao do frontend.

## Convencoes gerais

- Exceto por `POST /auth/login` e `GET /health`, todas as rotas exigem `Authorization: Bearer <accessToken>`.
- O usuario autenticado e obtido do token; o frontend nunca informa `userId` no corpo ou na URL.
- Datas de negocio usam `yyyy-MM-dd` e seguem `America/Sao_Paulo`.
- Respostas de erro usam `{ "message": "..." }`.
- Entradas invalidas retornam `400`, recursos inexistentes ou de outro usuario retornam `404` e conflitos de estado retornam `409`.
- Listagens paginadas recebem `page` e `pageSize`; o formato retorna `items`, `page`, `pageSize` e `totalCount`.

## 1. Autenticacao

### Entrar

1. O usuario informa `userName` e `password` em `POST /auth/login`.
2. A API valida as credenciais e cria uma sessao persistida.
3. O frontend recebe `accessToken`, `expiresAt` e os dados basicos do usuario.
4. O frontend armazena o token de forma segura e o envia nas requisicoes seguintes.

### Restaurar sessao

1. Ao abrir a aplicacao, o frontend chama `GET /auth/me` com o token salvo.
2. A API valida token, usuario e sessao ativa.
3. A resposta confirma a identidade; falha `401` significa que o frontend deve encerrar a sessao local e voltar ao login.

## 2. Dashboard

1. O frontend chama `GET /dashboard` ao abrir a area autenticada.
2. A API retorna, em uma resposta, o resumo financeiro do mes atual, habitos pendentes do dia, cinco treinos recentes e perfil de gamificacao.
3. O frontend usa a resposta para montar os atalhos de registrar transacao, concluir habito e iniciar treino.
4. Apos uma escrita relevante, o frontend pode atualizar apenas o recurso alterado ou recarregar o dashboard.

## 3. Financas

### Registrar transacao

1. O frontend carrega categorias ativas por `GET /finances/categories`.
2. O usuario informa categoria, valor, data, tipo, meio de pagamento, situacao e descricao opcional.
3. O frontend envia os dados para `POST /finances/transactions`.
4. A API retorna a transacao criada. Se ela estiver `Confirmed`, o saldo, os relatorios e o XP sao atualizados.
5. O frontend atualiza a lista e o resumo financeiro.

### Confirmar transacao planejada

1. O frontend mostra lancamentos planejados e vencidos.
2. O usuario confirma um lancamento por `POST /finances/transactions/{transactionId}/confirm`.
3. A API retorna a transacao confirmada e concede o XP configurado.
4. O frontend atualiza saldo, orcamento e status do item.

### Consultar relatorios e orcamentos

1. O frontend informa o mes como o primeiro dia do mes, por exemplo `2026-08-01`.
2. Para saldo, chama `GET /finances/reports/monthly-summary`.
3. Para alertas por categoria, chama `GET /finances/reports/category-spending`.
4. A resposta de categoria possui gasto, teto aplicavel, saldo, percentual e alerta.
5. Alterar o teto em `PUT /finances/categories/{categoryId}/budget` cria uma nova vigencia a partir do mes atual; meses anteriores preservam o teto historico.

### Recorrencias e parcelamentos

1. Para uma recorrencia mensal, o usuario informa os dados em `POST /finances/recurrences`.
2. A API cria ocorrencias planejadas ate o mes atual; consultas posteriores materializam novas ocorrencias quando necessario.
3. Para uma compra parcelada, o usuario envia total, quantidade e primeira data em `POST /finances/installment-purchases`.
4. A resposta inclui as parcelas mensais. A primeira parcela absorve diferencas de arredondamento.

## 4. Habitos e Lembretes

### Criar e acompanhar habito

1. O usuario informa titulo, prioridade e agenda em `POST /habits`.
2. A agenda pode ser diaria, por dias da semana, meta semanal ou meta diaria.
3. O frontend consulta pendencias em `GET /habits/pending?date=...` ou lembretes prontos para exibicao em `GET /habits/reminders?date=...`.
4. Cada lembrete retorna titulo, conclusoes atuais e meta do periodo.

### Concluir ou corrigir

1. O usuario marca uma conclusao enviando `completedOn` para `POST /habits/{habitId}/completions`.
2. A API valida a agenda, o limite do periodo e a janela dos ultimos sete dias.
3. A resposta retorna a conclusao criada; XP, meta semanal e badges relacionados sao recalculados.
4. Para corrigir, o frontend chama `DELETE /habits/{habitId}/completions/{completionId}`.
5. A API remove logicamente a conclusao e reverte efeitos de gamificacao que deixaram de ser validos.

## 5. Treinos

### Preparar catalogo e ficha

1. O usuario cria exercicios em `POST /workouts/exercises`.
2. O usuario cria uma ficha em `POST /workouts/sheets`, informando exercicios ordenados e series planejadas.
3. A ficha pode ser reutilizada para iniciar sessoes futuras.

### Executar treino

1. O usuario inicia por ficha ou avulso em `POST /workouts/sessions`.
2. O frontend apresenta exercicios e series, permitindo informar carga, unidade e repeticoes.
3. O frontend salva alteracoes por `PUT /workouts/sessions/{sessionId}`.
4. Ao terminar, chama `POST /workouts/sessions/{sessionId}/complete`.
5. A API concede XP, atualiza badges e retorna a sessao concluida.
6. Cancelar ou excluir a sessao remove seu efeito de historico, progressao e gamificacao.

## 6. Gamificacao

### Consultar perfil e historico

1. O frontend chama `GET /gamification/profile` para exibir XP, nivel atual, limite do proximo nivel e badges.
2. Para uma tela de historico, chama `GET /gamification/ledger` com filtros opcionais de evento e periodo.
3. O ledger mostra grants, reversoes e ajustes; a soma dos valores e o XP atual.

### Criar meta

1. O usuario escolhe tipo, titulo, alvo, unidade e prazo opcional.
2. Para `Habit`, o frontend envia uma fonte `Habit`; para `Training`, uma fonte `Exercise` ou `WorkoutSheet`.
3. Metas `Financial` e `FreeForm` nao recebem fontes.
4. O frontend envia para `POST /gamification/goals`.
5. Metas automaticas exibem sequencia calculada pela API. Metas livres recebem progresso por `PUT /gamification/goals/{goalId}/progress`.
6. Ao atingir o alvo, a API conclui a meta e concede XP. Uma correcao retroativa pode reativar a meta e reverter esse XP.

### Configurar regras e badges

1. O frontend carrega regras por `GET /gamification/xp-rules` e progressao por `GET /gamification/level-progression`.
2. Alteracoes sao enviadas por `PUT` para as mesmas rotas.
3. Badges sao listados em `GET /gamification/badges`, inclusive bloqueados.
4. Ao criar ou editar um badge, o frontend envia nome, descricao e todos os criterios.
5. Criterios podem limitar contagens a um habito, exercicio, categoria ou meta especifica.

## 7. Preferencias e Seguranca

### Preferencia de carga

1. O frontend consulta `GET /users/me/preferences`.
2. O usuario escolhe kg ou lb e o frontend envia `PUT /users/me/preferences`.
3. A preferencia apenas sugere a unidade inicial; cada serie permanece independente.

### Alterar senha e revogar sessoes

1. O usuario informa senha atual e nova em `PUT /users/me/password`.
2. A API exige o comprimento minimo configurado por `PasswordPolicy__MinimumLength`.
3. Em caso de sucesso, as demais sessoes sao revogadas e o token atual permanece valido.
4. O usuario tambem pode encerrar outras sessoes por `DELETE /users/me/sessions/others`.

## 8. Atualizacao de tela

- Apos criacoes e edicoes, prefira usar a resposta da API como estado atual do recurso.
- Apos confirmar, excluir ou corrigir dados que afetam calculos, recarregue os resumos relacionados: dashboard, relatorios financeiros, progresso de habitos, perfil de gamificacao ou badges.
- Trate `404` como recurso indisponivel ou removido, `409` como estado que precisa ser recarregado e `401` como sessao expirada ou revogada.

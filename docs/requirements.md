# ServiceLifeOS: Documento de Requisitos

## 1. Objetivo

O ServiceLifeOS e um sistema web pessoal para gerir financas, habitos, treinos e evolucao pessoal por gamificacao. O produto deve privilegiar interacoes simples, rapidas e de baixo esforco, sem sacrificar a completude dos registros. Este documento define o escopo do MVP, as regras de negocio e os requisitos de qualidade que orientam sua implementacao.

## 2. Escopo e usuarios

O MVP sera usado exclusivamente por um proprietario. A conta sera provisionada manualmente como parte da operacao do ambiente; nao havera cadastro publico nem autenticacao por provedores externos.

O produto sera disponibilizado como aplicacao web responsiva, com abordagem mobile-first e instalavel como PWA. A arquitetura pode manter preparacao para multiplos usuarios no futuro, mas multi-tenancy nao faz parte do comportamento funcional do MVP.

## 3. Regras transversais de negocio

- A moeda do sistema sera Real brasileiro (BRL).
- O fuso horario de referencia sera `America/Sao_Paulo`.
- Datas, periodos diarios, semanas de habitos e meses financeiros devem respeitar esse fuso horario.
- Semanas de habitos iniciam na segunda-feira e terminam no domingo.
- O mes financeiro e determinado pela data da transacao ou parcela.
- O sistema utiliza saldo consolidado; nao havera contas, carteiras ou transferencias entre contas no MVP.
- Listagens extensas devem oferecer paginacao, filtros e ordenacao consistentes.
- Dados alterados ou removidos que tenham concedido XP devem recalcular XP, nivel e badges derivados, preservando a consistencia com o estado atual do sistema.

## 4. Principios de experiencia

- Os fluxos diarios de registrar transacao, concluir habito e registrar treino devem priorizar poucos passos e preenchimento objetivo.
- Informacoes detalhadas e configuracoes avancadas devem permanecer disponiveis, mas fora do caminho principal de uso diario.
- O sistema deve usar valores iniciais sensatos para XP, niveis, badges e tipos de meta, sem exigir configuracao inicial do proprietario.
- A configuracao de gamificacao e metas deve ficar em area secundaria da aplicacao, separada dos fluxos diarios.
- O dashboard deve oferecer acesso direto as acoes mais frequentes, sem exigir navegacao por telas intermediarias.

## 5. Requisitos Funcionais

### 5.1 Acesso e perfil

- **RF01:** O sistema deve autenticar o proprietario por nome de usuario e senha.
- **RF02:** O sistema deve permitir que o proprietario autenticado altere sua propria senha.
- **RF03:** O sistema deve permitir que o proprietario encerre as demais sessoes ativas.
- **RF04:** A expiracao de sessoes e a politica de senha devem ser definidas por configuracao segura do ambiente.
- **RF05:** O dashboard deve exibir um resumo de financas, habitos do periodo, treinos e perfil de gamificacao, com acesso direto aos registros frequentes.

**Criterios de aceitacao:**

- Credenciais validas devem iniciar uma sessao autenticada; credenciais invalidas nao devem conceder acesso.
- A alteracao de senha deve exigir autenticacao do proprietario.
- Ao encerrar as demais sessoes, os respectivos tokens devem deixar de autorizar novas requisicoes.

### 5.2 Financas

- **RF06:** O sistema deve permitir criar, editar e excluir transacoes de receita e despesa com valor, data, categoria, meio de pagamento, descricao e situacao.
- **RF07:** Os meios de pagamento aceitos devem incluir PIX, credito, debito e credito parcelado.
- **RF08:** Uma transacao pode estar planejada ou confirmada. Transacoes planejadas representam movimentos futuros e podem ser confirmadas posteriormente.
- **RF09:** O sistema deve permitir criar recorrencias mensais para receitas e despesas, encerrar uma serie sem alterar o historico existente e editar somente suas ocorrencias futuras.
- **RF10:** Uma compra em credito parcelado deve gerar parcelas mensais vinculadas; cada parcela deve impactar exclusivamente o mes de sua propria data. Quando houver diferenca de arredondamento, a primeira parcela deve absorver os centavos residuais.
- **RF11:** O sistema deve disponibilizar categorias iniciais e permitir criar, editar e arquivar categorias personalizadas.
- **RF12:** O sistema deve permitir definir um teto mensal de despesas por categoria, manter seu valor nos meses seguintes e definir excecoes para meses especificos.
- **RF13:** O saldo mensal, os orcamentos e os relatorios devem considerar somente transacoes confirmadas. A projecao de fluxo de caixa deve considerar transacoes confirmadas e planejadas.
- **RF14:** Uma transacao planejada cuja data ja tenha passado deve permanecer planejada e ser destacada como vencida ate ser confirmada, editada ou excluida.
- **RF15:** O sistema deve emitir alerta visual quando uma categoria atingir 80% do teto, atingir 100% do teto ou exceder o teto mensal.
- **RF16:** O saldo nao utilizado de um teto de categoria nao deve ser transferido para o mes seguinte.
- **RF17:** O sistema deve exibir resumo mensal de receitas, despesas e saldo; distribuicao de despesas por categoria; comparacao entre meses; e projecao de fluxo de caixa.

**Criterios de aceitacao:**

- Uma despesa confirmada deve reduzir o saldo e o orcamento da categoria no mes de sua data; uma receita confirmada deve aumentar o saldo.
- Uma transacao planejada nao deve alterar o saldo, os orcamentos ou relatorios realizados antes de ser confirmada.
- Ao registrar uma compra parcelada, cada parcela deve estar visivel no mes correspondente e nao deve concentrar o valor total no primeiro mes.
- Os alertas devem refletir os limites de 80%, 100% e acima de 100% do teto aplicavel ao mes.

### 5.3 Habitos

- **RF18:** O sistema deve permitir criar, editar, pausar, retomar, arquivar e excluir habitos.
- **RF19:** Um habito deve ter titulo, prioridade baixa, media ou alta e uma agenda definida como diaria, por dias especificos da semana, por quantidade de conclusoes semanais ou por quantidade de conclusoes diarias.
- **RF20:** O sistema deve permitir registrar conclusoes de habitos, inclusive mais de uma conclusao no mesmo dia quando a meta diaria assim exigir.
- **RF21:** O sistema deve permitir consultar e corrigir o historico de conclusoes do dia atual e dos sete dias anteriores.
- **RF22:** O sistema deve calcular a ofensiva de um habito por periodos consecutivos cumpridos: dias para agendas diarias ou por dias da semana, e semanas de segunda-feira a domingo para metas semanais.
- **RF23:** O sistema deve exibir no dashboard os habitos pendentes e o progresso do periodo atual.
- **RF24:** O sistema deve apresentar lembretes dentro da aplicacao para habitos pendentes, sem notificacoes push.

**Criterios de aceitacao:**

- Uma conclusao fora da agenda ou acima da quantidade diaria definida nao deve ser contabilizada como progresso valido.
- Uma correcao retroativa deve ser aceita somente dentro da janela de sete dias, considerada no fuso `America/Sao_Paulo`.
- Pausar ou arquivar um habito deve preservar seu historico e impedir que apareca como pendencia durante esse estado.
- A ofensiva deve ser atualizada quando uma conclusao for criada, corrigida ou removida.

### 5.4 Treinos

- **RF25:** O sistema deve manter um catalogo de exercicios reutilizaveis.
- **RF26:** O sistema deve permitir criar e editar fichas de treino com lista ordenada de exercicios, series e repeticoes planejadas.
- **RF27:** O sistema deve permitir iniciar uma sessao a partir de uma ficha ou registrar uma sessao avulsa.
- **RF28:** Durante uma sessao, o sistema deve registrar carga e repeticoes realizadas por serie.
- **RF29:** A unidade de carga deve ser selecionavel por serie entre quilogramas (kg) e libras (lb). A preferencia do usuario deve apenas sugerir a unidade inicial.
- **RF30:** O sistema deve permitir salvar sessoes incompletas, concluir sessoes, editar sessoes concluidas e cancelar sessoes.
- **RF31:** Sessoes canceladas nao devem compor o historico de treino nem gerar XP.
- **RF32:** O sistema deve exibir a progressao por exercicio por carga maxima, melhor serie e volume total, calculado pela soma de carga multiplicada por repeticoes em cada serie.

**Criterios de aceitacao:**

- Uma sessao iniciada a partir de ficha deve carregar os exercicios, series e repeticoes planejados, permitindo registrar os valores realizados.
- Uma sessao avulsa deve poder ser registrada sem ficha preexistente.
- A edicao de uma sessao concluida deve atualizar os indicadores de progressao e os efeitos de gamificacao relacionados.

### 5.5 Metas e gamificacao

- **RF33:** O sistema deve permitir criar metas financeiras, de habitos, de treinos e metas livres, com titulo, descricao, valor-alvo, unidade, prazo opcional e estado ativo, concluido ou cancelado.
- **RF34:** Metas financeiras, de habitos e de treinos devem ter progresso calculado automaticamente a partir dos dados do sistema. Metas livres devem permitir atualizacao manual de progresso.
- **RF35:** O sistema deve conceder XP automaticamente por conclusao de habito, cumprimento de meta semanal de habito, conclusao de treino, confirmacao de transacao, fechamento mensal positivo e conclusao de meta pessoal.
- **RF36:** O fechamento mensal positivo deve exigir, simultaneamente, receitas confirmadas superiores a despesas confirmadas e ausencia de categorias acima do respectivo teto naquele mes.
- **RF37:** O sistema deve manter um historico das concessoes e ajustes de XP, com sua origem.
- **RF38:** O sistema deve calcular o nivel do proprietario conforme os limiares de XP configurados.
- **RF39:** O sistema deve manter um catalogo configuravel de badges e desbloquea-los automaticamente conforme criterios de XP ou nivel, consistencia de habitos, progresso de treinos, resultados financeiros ou conclusao de metas.
- **RF40:** O sistema deve exibir badges bloqueados e desbloqueados.
- **RF41:** A interface de configuracao deve permitir alterar valores de XP por evento, limiares de nivel, criterios de badges e definicoes de tipos de meta.

**Criterios de aceitacao:**

- Cada concessao de XP deve identificar o evento que a originou.
- A alteracao ou exclusao de um evento de origem deve recalcular XP, nivel e badges afetados.
- Um badge so deve ser exibido como desbloqueado quando todos os seus criterios configurados forem atendidos.
- Uma meta com progresso automatico deve refletir seus dados de origem sem atualizacao manual.

## 6. Requisitos Nao Funcionais

### 6.1 Arquitetura e integracao

- **RNF01:** O backend deve utilizar .NET 10 e C#.
- **RNF02:** O frontend deve utilizar React com Next.js.
- **RNF03:** A persistencia deve utilizar PostgreSQL.
- **RNF04:** A comunicacao entre frontend e backend deve ocorrer exclusivamente por HTTP, seguindo o estilo REST.
- **RNF05:** O backend deve manter arquitetura modular, favorecendo futura extracao de contextos isolados.
- **RNF06:** A API deve disponibilizar e manter atualizada documentacao OpenAPI.
- **RNF07:** A API deve adotar um formato padronizado e documentado para erros HTTP.

### 6.2 Usabilidade, acessibilidade e PWA

- **RNF08:** A interface deve ser mobile-first e adaptar-se a telas de smartphones e desktop.
- **RNF09:** A interface deve atender ao nivel WCAG 2.2 AA.
- **RNF10:** A aplicacao deve poder ser instalada como PWA.
- **RNF11:** A PWA deve permitir consultar dados carregados recentemente sem conexao. Criacoes, edicoes, exclusoes e sincronizacao exigem conexao.
- **RNF12:** O sistema deve suportar as versoes atuais de Chrome, Edge, Firefox e Safari.
- **RNF13:** A interface deve aplicar divulgacao progressiva, mantendo os fluxos diarios objetivos e exibindo opcoes avancadas somente quando solicitadas.

### 6.3 Seguranca

- **RNF14:** Todo trafego em producao deve utilizar HTTPS.
- **RNF15:** A API deve aplicar limitacao de requisicoes, com protecao reforcada contra tentativas abusivas de autenticacao.
- **RNF16:** A API deve aplicar cabecalhos HTTP de seguranca apropriados.
- **RNF17:** O sistema deve registrar em auditoria as acoes sensiveis, incluindo autenticacao, alteracao de senha e alteracoes financeiras.
- **RNF18:** Logs, auditoria e rastreamento de erros nao devem expor senhas, tokens ou outros dados sensiveis.

### 6.4 Operacao e qualidade

- **RNF19:** O ambiente deve contar com procedimento manual de backup.
- **RNF20:** A API deve expor endpoint de verificacao de saude.
- **RNF21:** O sistema deve registrar excecoes nao tratadas em mecanismo de rastreamento de erros.
- **RNF22:** O sistema deve coletar metricas operacionais de disponibilidade, latencia e erros.
- **RNF23:** Nao ha meta numerica de desempenho definida para o MVP; operacoes usuais devem manter resposta adequada ao uso pessoal esperado.

## 7. Fora do escopo do MVP

- Cadastro publico de usuarios, autenticacao social e recuperacao de senha.
- Multi-tenancy e colaboracao entre usuarios.
- Contas financeiras, carteiras, transferencias e anexos de comprovantes.
- Recorrencias financeiras diarias ou semanais.
- Notificacoes push e operacoes de escrita offline.
- Aplicativos moveis nativos.
- Exportacao ou exclusao permanente de dados pela interface.
- Metas de desempenho numericas e backup automatizado.

## 8. Glossario

- **Transacao planejada:** lancamento financeiro futuro que ainda nao impacta saldo, orcamentos ou relatorios realizados.
- **Transacao confirmada:** lancamento financeiro efetivado que impacta os calculos do mes.
- **Ofensiva:** sequencia de periodos consecutivos nos quais a meta de um habito foi cumprida.
- **Ficha de treino:** modelo reutilizavel com exercicios e planejamento de series e repeticoes.
- **Sessao de treino:** execucao registrada de uma ficha ou treino avulso.
- **XP:** pontos de experiencia concedidos por eventos positivos definidos nas regras de gamificacao.
- **Badge:** conquista desbloqueada automaticamente quando seus criterios configurados sao atendidos.

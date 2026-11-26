> Migrado para `backend/` (24–25/11/2026). Os scripts citados aqui como `scripts` estão em `scripts/` na raiz. Esta lista é a referência das tasks B00–B27.

# Checklist de implementação manual — backend Help Desk Comprove

Base: `C:\Users\JFC\Desktop\SUPORTE HELP DESK\docs\arquitetura_helpdesk_comprove.md` (`Versão: 0.1`, `Data: 23/11/2026`) reconciliada com as decisões `D-01` a `D-15` já confirmadas.

Backend é autoritativo. O protótipo em `C:\Users\JFC\Desktop\SUPORTE HELP DESK\front_end\SUPORTE-HELP-DESK-` é adaptado na camada BFF (`B20`/`B21`); não copie storage keys do navegador para o domínio.

## Status dos pré-requisitos (validado em 24/11/2026)

- `B00` concluído: o SDK 10.0.x do sistema atende (ADR-011).
- `B01` concluído: solution compila com 0 avisos e 0 erros; skeleton `Api`/`Worker` e pacotes com versão fixa.
- `B02` pendente de ação sua: o serviço Windows `MongoDB` ocupa `27017`. Rode `Stop-Service MongoDB` (admin) antes de `New-HelpDeskInfra.ps1 -Start`.
- `B14` validado: RabbitMQ subiu com as 8 filas (4 + 4 DLQ) e exchanges `helpdesk.topic` e `helpdesk.dlx`.
- Replica set `rs0`: script de init testado (PRIMARY eleito, idempotente).
- Frontend: install, lint e build passam (`Validate-HelpDeskFrontend.ps1`).

## Decisão pendente antes de B01 ser considerado final

**D-01 resolvido: .NET 10 LTS (ADR-011). O .NET 8 saiu de suporte em 10/11/2026.** `global.json` em `10.0.100`, projetos em `net10.0`, pacotes Microsoft.* em 10.0.x.

## Ordem recomendada

`B00 → B01 → B02 → B03 → B04 → B05 → B06 → B07 → B08 → B09 → B10 → B11 → B12 → B13 → B14 → B15 → B16 → B17 → B18 → B19 → B20 → B21 → B22 → B23 → B24 → B25 → B26 → B27`

## Tasks

### B00 — Instalar .NET 10 SDK local

- **Depende:** nada.
- **Como:** `.\scripts\Install-DotNetSdk.ps1`.
- **Critérios de aceite:**
  - `dotnet --list-sdks` mostra `10.0.x`.
  - SDK 10.0.x disponível no sistema, sem instalação local.
  - `global.json` com `10.0.100` e `rollForward: latestFeature` é respeitado.

### B01 — Criar solution e projetos

- **Depende:** `B00`.
- **Como:** `.\scripts\Scaffold-HelpDeskBackend.ps1 -Root "C:\Users\JFC\Desktop\SUPORTE HELP DESK\backend"` (idempotente; pode ser rodado de novo).
- **Critérios de aceite:**
  - Solution `Comprove.HelpDesk.sln` com projetos: `Domain`, `Application`, `Infrastructure`, `Api`, `Worker`, `Contracts`, `Tests.Unit`, `Tests.Integration`.
  - Todos os projetos usam `net10.0`, `Nullable`, `ImplicitUsings` e `TreatWarningsAsErrors` conforme `templates/Directory.Build.props`.
  - Referências: `Domain` sem dependências externas; `Application → Domain`; `Infrastructure → Application`; `Api/Worker → Application + Infrastructure + Contracts`.
  - `dotnet build Comprove.HelpDesk.sln -c Release` passa.

### B02 — Materializar infra local

- **Depende:** nada.
- **Pré-requisito de ambiente:** Docker Desktop em `Engine running` e serviço Windows `MongoDB` parado (`Stop-Service MongoDB`, como administrador).
- **Como:** `.\scripts\New-HelpDeskInfra.ps1 -Target "C:\Users\JFC\Desktop\SUPORTE HELP DESK\infra" -Start`. O script checa Docker e porta 27017, espera o healthcheck e inicializa `rs0` (`mongo-init.js`).
- **Critérios de aceite:**
  - MongoDB em `127.0.0.1:27017` com replica set `rs0` (necessário para transação da Outbox).
  - RabbitMQ em `5672` e management em `15672`, com exchange `helpdesk.topic` e topology de `templates/rabbitmq-definitions.json`.
  - Redis em `6379` é opcional e usado apenas como L2 após medição de gargalo.
  - `docker compose --file infra/docker-compose.yml ps` mostra os serviços desejados.

### B03 — Configuração e segredos

- **Depende:** `B01`, `B02`.
- **Critérios de aceite:**
  - `appsettings.json` e `appsettings.Development.json` contêm `ConnectionStrings:HelpDeskMongo`, `RabbitMq`, `Storage`, `BubbleSecurity` e `Sla`.
  - Nenhum segredo real é commitado; valores vêm de variáveis de ambiente ou user-secrets.
  - `BubbleSecurity:SigningKey`, `BubbleSecurity:Issuer`, `BubbleSecurity:Audience`, `BubbleSecurity:TokenTtlSeconds` existem.
  - `Tenant` é sempre resolvido no backend a partir do token validado, nunca do corpo da requisição confiado.

### B04 — Domínio: agregado Ticket e estado

- **Depende:** `B01`.
- **Critérios de aceite:**
  - `Ticket`, `TicketStatus`, `Severity`, `TicketMessage`, `TicketAttachment`, `SlaSnapshot`, `AuditEntry`.
  - `TicketStatus` exatamente: `Recebido`, `EmTriagem`, `AguardandoAtendimento`, `EmAtendimento`, `AguardandoSolicitante`, `Resolvido`, `Fechado`.
  - `Severity` com pesos `Crítica=4`, `Alta=3`, `Média=2`, `Baixa=1`.
  - `Ticket` não depende de MongoDB, RabbitMQ, ASP.NET ou Bubble.
  - Métodos de domínio não recebem `tenant_id` de fonte não confiável.

### B05 — State machine e SLA

- **Depende:** `B04`.
- **Critérios de aceite:**
  - Transições permitidas:
    - `[*] → Recebido`
    - `Recebido → EmTriagem`
    - `EmTriagem → AguardandoAtendimento`
    - `AguardandoAtendimento → EmAtendimento`
    - `EmAtendimento → AguardandoSolicitante`
    - `AguardandoSolicitante → EmAtendimento`
    - `EmAtendimento → Resolvido`
    - `Resolvido → Fechado`
    - `Resolvido → EmAtendimento` com `reason` obrigatório.
  - Transições proibidas testadas: `Recebido → Resolvido` sem classificação e `Fechado → EmAtendimento` sem reabertura explícita.
  - `SlaPolicy` com matriz inicial `Crítica=2h`, `Alta=6h`, `Média=24h`, `Baixa=48h`.
  - Campos persistidos: `sla_deadline`, `paused_at`, `resolved_at`.
  - `AguardandoSolicitante` pausa o relógio; `EmAtendimento` retoma.
  - Testes unitários cobrem todas as transições e os quatro prazos.

### B06 — Contratos de persistência

- **Depende:** `B04`.
- **Critérios de aceite:**
  - Database lógica `helpdesk_database`, separada de `erp_database`.
  - Collections: `tickets`, `ticket_history`, `ticket_comments`, `ticket_attachments`, `ticket_categories`, `support_queues`, `support_teams`, `sla_policies`, `outbox_events`, `processed_messages`.
  - Documento de ticket usa `snake_case`: `tenant_id`, `protocol`, `requester.external_user_id`, `category_id`, `title`, `description`, `priority`, `status`, `queue_id`, `assigned_to`, `created_at`, `updated_at`, `sla_deadline`, `paused_at`, `resolved_at`.
  - `ticket_history` registra `ticket_id`, `tenant_id`, `change_type`, `from_status`, `to_status`, `actor`, `occurred_at`, `reason`, `correlation_id`.
  - Nenhum array ilimitado de histórico dentro de `tickets`.

### B07 — Repositórios e índices

- **Depende:** `B06`.
- **Critérios de aceite:**
  - Interface `ITicketRepository`, `ITicketHistoryRepository`, `IOutboxRepository`, `IProcessedMessageRepository`, `ISlaPolicyRepository`.
  - Implementação em `Infrastructure/Persistence/Mongo` usando `MongoDB.Driver`.
  - Toda query aplica filtro `tenant_id`, exceto consulta administrativa explicitamente autorizada.
  - Índices criados no bootstrap:
    - `(tenant_id, created_at)`
    - `(tenant_id, status, updated_at)`
    - `(tenant_id, requester.external_user_id, created_at)`
    - `(tenant_id, assigned_to, status)`
    - `(tenant_id, queue_id, status)`
    - `(protocol)` com unicidade global
    - `(event_id)` com unicidade em `processed_messages`
    - `(idempotency_key)` com unicidade em `tickets`
  - Testes de integração provam que dois tenants com o mesmo `external_user_id` não se veem.

### B08 — Protocolo global

- **Depende:** `B07`.
- **Critérios de aceite:**
  - Formato `SUP-YYYY-NNNNNN` (exemplo da arquitetura: `SUP-2026-000001`).
  - Sequência por ano com retry em colisão de índice único.
  - Protocolo é global, não por tenant (`D-08`).
  - `ticketCode` do protótipo (`HD-2041`) é apenas projeção na BFF; o identificador canônico é `protocol`.
  - Teste concorrente de 100 aberturas não gera duplicidade.

### B09 — Autenticação, tenant e autorização

- **Depende:** `B03`, `B07`.
- **Critérios de aceite:**
  - Middleware valida token curto assinado vindo do Bubble (`D-04`).
  - Claims obrigatórias: `sub` (external user id), `tenant_id`, `roles`, `exp`, `iat`, `jti`.
  - Token expirado, assinatura inválida ou `tenant_id` ausente retorna `401`.
  - Usuário sem permissão para o recurso retorna `403`.
  - Nenhum endpoint aceita `tenant_id` do body para decidir isolamento.
  - Testes de integração cobrem `401`, `403` e isolamento entre tenants.

### B10 — Command: abrir ticket

- **Depende:** `B05`, `B08`, `B09`.
- **Critérios de aceite:**
  - Endpoint/Command `OpenTicket` valida título, descrição, severidade/categoria e identidade.
  - API persiste `tickets` com status `Recebido`, `ticket_history` inicial e `outbox_events` na mesma transação MongoDB.
  - Evento gravado: `helpdesk.ticket.created` com `event_id`, `schema_version`, `tenant_id`, `ticket_id`, `protocol`, `correlation_id`, `occurred_at`.
  - API retorna `protocol` ao Bubble antes de qualquer Worker terminar.
  - Worker nunca é a primeira persistência do ticket.
  - Teste de integração usa replica set e confirma rollback da Outbox se a transação falhar.

### B11 — Queries autorizadas

- **Depende:** `B09`, `B10`.
- **Critérios de aceite:**
  - `GET /tickets`, `GET /tickets/{id}` filtram por `tenant_id` do token.
  - Projeção de listagem não traz anexos privados nem comentários internos.
  - Paginação por `created_at` e filtros por `status`, `queue_id`, `assigned_to`, `requester`.
  - Teste prova que `GET /tickets/{id}` de outro tenant retorna `404`, nunca `403` com vazamento de existência.

### B12 — Command: transições e histórico

- **Depende:** `B05`, `B10`.
- **Critérios de aceite:**
  - Toda mudança de status passa pela state machine.
  - `ticket_history` é gravado na mesma transação da mudança.
  - `from_status`, `to_status`, `actor`, `reason`, `correlation_id` são obrigatórios quando aplicável.
  - Reabertura de `Resolvido` exige `reason` e o tipo de alteração `reopened`.
  - Testes unitários e de integração cobrem transições válidas e inválidas.

### B13 — Outbox e publicador

- **Depende:** `B10`.
- **Critérios de aceite:**
  - Collection `outbox_events` com status `pending`, `published`, `failed` e `attempts`.
  - Publicador faz polling com backoff, publica em `helpdesk.topic`, marca como publicado e suporta recuperação.
  - Routing key canônico: `helpdesk.ticket.created`; demais eventos seguem `helpdesk.ticket.*`, `helpdesk.comment.created`, `helpdesk.notification.requested`, `helpdesk.sla.*`.
  - Mensagens duplicadas não provocam efeito duplicado.
  - Teste de integração derruba o broker, confirma evento pendente e o recupera.

### B14 — Contratos RabbitMQ e topology

- **Depende:** `B02`, `B13`.
- **Critérios de aceite:**
  - Exchange `helpdesk.topic` do tipo `topic`.
  - Filas exatas: `ticket.created.classification`, `ticket.classified.dispatch`, `ticket.resolved.notify`, `integration.comprove.query`.
  - DLQ por fila: `<fila>.dlq`.
  - `dead-letter-exchange: helpdesk.dlx`, `dead-letter-routing-key: <fila>.dlq`.
  - Contratos em `Contracts` com `event_id`, `schema_version`, `correlation_id`, `occurred_at`, `tenant_id`.
  - `templates/rabbitmq-definitions.json` é idempotente e pode ser reaplicado.

### B15 — Inbox/idempotência dos consumidores

- **Depende:** `B14`.
- **Critérios de aceite:**
  - `processed_messages` com índice único em `event_id`.
  - Consumidor registra `event_id` e resultado antes de confirmar a mensagem.
  - Reentrega da mesma mensagem não cria histórico, comentário, notificação ou evento duplicado.
  - Mensagem inválida vai para DLQ com metadados de erro.
  - Política: retry com backoff, limite de tentativas, DLQ, alerta de fila parada e procedimento de reprocessamento documentado.

### B16 — Worker de classificação

- **Depende:** `B15`.
- **Critérios de aceite:**
  - Consome `ticket.created.classification`.
  - Usa regra determinística: `IMPACT` (`3/2/1`) + `SCOPE` (`3/2/1`) + `FREQUENCY` (`2/1/0`).
  - `computeSeverity(total)`: `>=7 Crítica`, `>=5 Alta`, `>=3 Média`, senão `Baixa`.
  - Atualiza `priority`/`severity`, `category_id` e status `EmTriagem`.
  - Publica `helpdesk.ticket.updated`/`helpdesk.ticket.status-changed` na fila `ticket.classified.dispatch`.
  - IA não é chamada nesta task.

### B17 — Worker de dispatch

- **Depende:** `B16`.
- **Critérios de aceite:**
  - Consome `ticket.classified.dispatch`.
  - Usa `support_queues` e `support_teams`.
  - Atribui `queue_id` e `assigned_to`, transiciona para `AguardandoAtendimento` ou `EmAtendimento`.
  - Respeita carga/rodízio definido em configuração; sem distribuição aleatória não auditável.
  - Registra histórico com `actor=system` e `correlation_id`.

### B18 — Resolução e sincronização FAQ

- **Depende:** `B15`, `B17`.
- **Critérios de aceite:**
  - `POST /tickets/{id}/resolve` é command idempotente por `idempotency_key`.
  - Uma transação no MongoDB: status `Resolvido`, `resolved_at`, `ticket_history` e `outbox_events`.
  - Um único evento `helpdesk.ticket.resolved` publicado na fila `ticket.resolved.notify`.
  - Sincronização FAQ acontece como reação ao evento confirmado, não em dois disparos independentes.
  - Se a publicação falhar, a resolução permanece consistente e a Outbox recupera.

### B19 — ACL Comprove / FAQ

- **Depende:** `B15`.
- **Critérios de aceite:**
  - Consome `integration.comprove.query`.
  - Worker de integração consulta o Comprove e retorna ao usuário; a fonte exata (`D-12`) ainda precisa ser confirmada, então a implementação deve ficar atrás de `IComproveIntegration`.
  - Nunca acessa collections do ERP diretamente (`ADR-003`/`D-05`).
  - Usa `Microsoft.Extensions.Http.Resilience` (já referenciado pelo scaffold) com timeout, retry, circuit breaker e bulkhead.
  - Propaga `correlation_id`; logs não registram token nem payload sensível.
  - Teste com stub confirma que falha da integração não muda o estado do ticket nem gera efeito não auditado.

### B20 — API BFF

- **Depende:** `B09`, `B10`, `B11`, `B12`, `B18`.
- **Critérios de aceite:**
  - Endpoints: `POST /tickets`, `GET /tickets`, `GET /tickets/{id}`, `PATCH /tickets/{id}/meta`, `POST /tickets/{id}/messages`, `POST /tickets/{id}/resolve`.
  - Upload de anexos usa URLs temporárias, não upload direto no corpo do ticket.
  - Validação de entrada com FluentValidation; erros de domínio retornam `422`, não `500`.
  - `ProblemDetails` com `correlation_id` em toda falha.
  - OpenAPI/Swagger disponível em Development.
  - Nenhum segredo do Bubble é exposto ao navegador.

### B21 — Contrato com o protótipo React e SignalR

- **Depende:** `B20`.
- **Critérios de aceite:**
  - Mapeamento explícito:
    - `Recebido` → `Novo`
    - `EmTriagem` / `AguardandoAtendimento` → `Novo` ou agrupamento configurável
    - `EmAtendimento` → `Em Atendimento`
    - `AguardandoSolicitante` → `Aguardando Solicitante`
    - `Resolvido` / `Fechado` → `Resolvido`
  - Campos do protótipo mapeados: `Id`, `ticketCode`, `title`, `description`, `category`, `severity`, `severityScore`, `impact`, `scope`, `frequency`, `status`, `createdAt`, `updatedAt`, `slaDeadline`, `messages`, `linkedFaqId`, `linkedFaqCode`, `assignedAgent`.
  - `slaDeadline` do frontend é projeção de `sla_deadline`.
  - Atualização em tempo real usa `SignalR + refetch`: SignalR avisa, refetch busca estado autoritativo.
  - Cliente não calcula prazo, isolamento nem transição.
  - URL do backend vem de env/query; sem hardcode de `localhost` no bundle de produção.

### B22 — Anexos

- **Depende:** `B20`.
- **Critérios de aceite:**
  - `POST /tickets/{id}/attachments` devolve URL temporária de upload.
  - `ticket_attachments` guarda metadados: `ticket_id`, `tenant_id`, `object_key`, `content_type`, `size`, `created_by`, `created_at`.
  - Download também usa URL temporária.
  - Nenhum arquivo privado é cacheado em L1/L2.
  - TTL e limites de tamanho configuráveis.

### B23 — Resiliência e performance

- **Depende:** `B14`, `B19`.
- **Critérios de aceite:**
  - Retry com backoff e jitter, circuit breaker, bulkhead e DLQ configurados.
  - Cache-aside L1 `IMemoryCache` com TTL `5-15 min` e chave incluindo `rules_version`.
  - Redis L2 apenas se houver evidência de gargalo; não obrigatório no MVP.
  - Não cachear permissões, notas internas, anexos privados ou estado de ticket com TTL longo.
  - Teste de carga simples documenta latência antes/depois do cache.

### B24 — Observabilidade

- **Depende:** `B13`, `B15`, `B20`.
- **Critérios de aceite:**
  - Logs estruturados com `correlation_id`, `tenant_id`, `event_id`, `ticket_id`.
  - Métricas: tickets criados por tenant, eventos pendentes na Outbox, mensagens em DLQ, tempo de publicação, latência de API, falhas de ACL.
  - Health checks para MongoDB, RabbitMQ e storage.
  - Alertas para Outbox antiga, DLQ não vazia e fila parada.
  - Nenhum log com token, segredo ou payload sensível.

### B25 — Testes

- **Depende:** `B05`, `B07`, `B10`, `B15`, `B19`, `B20`.
- **Critérios de aceite:**
  - `Tests.Unit`: state machine, SLA, severidade determinística, protocolo, autorização.
  - `Tests.Integration`: Mongo com replica set, transação Outbox, RabbitMQ, idempotência, isolamento entre tenants, endpoints BFF.
  - Testes de contrato entre eventos `Contracts` e consumidores.
  - `dotnet test Comprove.HelpDesk.sln -c Release` passa sem testes ignorados relevantes.
  - Cobertura das regras essenciais registrada no relatório.

### B26 — ADRs e documentação

- **Depende:** `B01`–`B25`.
- **Atenção:** o projeto não tem pasta de ADRs ainda. Crie `docs\adr\` com um arquivo por ADR (`ADR-002` … `ADR-006`) antes de atualizar os status abaixo; o validador não verifica ADRs.
- **Critérios de aceite:**
  - `ADR-002` passa de `Proposto` para `Aceito`.
  - `ADR-003` passa de `Recomendado, pendente` para `Aceito` com database `helpdesk_database`.
  - `ADR-004` registra validação da transação MongoDB com replica set.
  - `ADR-005` passa de `Candidato, não aprovado` para `Aceito — RabbitMQ`.
  - `ADR-006` registra multi-tenancy desde o início.
  - Novos ADRs para classificação determinística, SLA e cache-aside.
  - `D-14`, `D-15` e fonte de `D-12` permanecem como pendências explícitas se ainda não resolvidas.

### B27 — Validação final pelo agente

- **Depende:** `B26`.
- **Como:**

```powershell
.\scripts\Validate-HelpDeskBackend.ps1 -Root "C:\Users\JFC\Desktop\SUPORTE HELP DESK\backend"
```

- **Critérios de aceite:**
  - Termina com `VALIDAÇÃO: PASS`.
  - `dotnet build` e `dotnet test` passam.
  - Estrutura, dependências, tokens de domínio, collections, filas, endpoints e portas locais são confirmados.
  - Qualquer `FAIL` é corrigido e o validador é rerodado.

## Definição de pronto global

- Usuário autenticado abre chamado e recebe protocolo global sem colisão.
- Backend valida identidade, tenant e permissão.
- Ticket, histórico e Outbox são gravados atomicamente.
- Evento pode ser publicado e consumido; duplicidade não gera efeito duplicado.
- Usuário autorizado consulta o chamado.
- Transições respeitam a state machine e o histórico é persistido.
- Falhas são registradas, observáveis e recuperáveis.
- Testes automatizados cobrem regras essenciais e isolamento entre tenants.
- Integração com Bubble funciona em homologação.
- Deploy e rollback estão documentados.

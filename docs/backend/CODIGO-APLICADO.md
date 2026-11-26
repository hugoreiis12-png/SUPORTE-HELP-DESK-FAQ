# Código do backend — onde está cada task

O código do backend está aplicado em `backend/src` e `backend/tests` (.NET 10). Este mapa liga cada task do `CHECKLIST.md` aos arquivos.

| Task | Arquivos |
|---|---|
| B03 configuração | `Api/appsettings*.json`, `Worker/appsettings*.json`, `Infrastructure/Configuration/HelpDeskOptions.cs`, `Api/Configuration/BubbleSecurityOptions.cs` |
| B04 agregado | `Domain/Entities/Ticket.cs`, `TicketSnapshot.cs`, `Domain/Enums/*` |
| B05 state machine e SLA | `Domain/Rules/TicketStateMachine.cs`, `Domain/Rules/SlaPolicy.cs` |
| B06 persistência | `Infrastructure/Persistence/Mongo/Documents/*`, `MongoCollectionNames.cs` |
| B07 repositórios e índices | `Infrastructure/Persistence/Mongo/*Repository.cs`, `MongoBootstrap.cs` |
| B08 protocolo | `Domain/ValueObjects/ProtocolNumber.cs`, `Application/Services/ProtocolGenerator.cs` |
| B09 autenticação e tenant | `Api/Auth/*`, `Api/Middlewares/CorrelationIdMiddleware.cs` |
| B10 abrir ticket | `Application/UseCases/OpenTicketUseCase.cs` |
| B11 consultas | `Application/UseCases/TicketReadUseCase.cs`, `Api/Controllers/TicketsController.cs` |
| B12 transições | `Application/UseCases/TransitionTicketUseCase.cs`, `Application/Services/AuditFactory.cs` |
| B13 outbox | `Infrastructure/Persistence/Mongo/OutboxRepository.cs`, `Worker/Outbox/OutboxRelayService.cs`, `Infrastructure/Messaging/RabbitMq/RabbitEventPublisher.cs` |
| B14 topologia | `Contracts/Integration/RabbitTopology.cs` (topologia em `infra/rabbitmq-definitions.json`) |
| B15 inbox | `Infrastructure/Persistence/Mongo/ProcessedMessageRepository.cs`, `Worker/Consumers/RabbitQueueWorker.cs`, `MessageRetryPolicy.cs` |
| B16 classificação | `Domain/Rules/SeverityClassifier.cs`, `Worker/Consumers/TicketCreatedClassificationWorker.cs` |
| B17 dispatch | `Application/UseCases/DispatchTicketUseCase.cs`, `Infrastructure/Persistence/Mongo/TeamRoster.cs` |
| B18 resolução | `Application/UseCases/ResolveTicketUseCase.cs`, `Worker/Consumers/TicketResolvedNotifyWorker.cs` |
| B19 ACL Comprove | `Infrastructure/Integration/Comprove/*`, `Worker/Consumers/ComproveIntegrationWorker.cs` |
| B20 API | `Api/Controllers/TicketsController.cs`, `Api/Validation/*`, `Api/Middlewares/ExceptionHandlingMiddleware.cs` |
| B21 contrato com o front | `Api/Mapping/*`, `Api/SignalR/*`, `front_end/.../src/api/helpdeskClient.ts` |
| B22 anexos (GridFS, ADR-010) | `Infrastructure/Storage/*`, `Api/Controllers/AttachmentsController.cs`, `Application/UseCases/RequestAttachmentUseCase.cs` |
| B23 cache | `Infrastructure/Cache/CachedSlaPolicyProvider.cs` |
| B24 observabilidade | `Api/Health/HealthChecks.cs` |
| B25 testes | `backend/tests/*` (unitários passando; integração precisa de Mongo e RabbitMQ) |
| B26 ADRs | `docs/adr/` |

## Pendências

- **FAQ (D-12):** `TicketResolvedNotifyWorker` fica desligado (`Comprove:FaqSyncEnabled=false`) até o endpoint ser definido.
- **Testes de integração:** faltam outbox com rollback e inbox com duplicata. Os existentes precisam só de Mongo em replica set.
- **Métricas (B24):** só health checks. Contadores de outbox, DLQ e latência não foram escritos.

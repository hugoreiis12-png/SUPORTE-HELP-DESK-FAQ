# Registro de decisões arquiteturais (ADR) — Help Desk Comprove

Um arquivo por decisão. Status possíveis: `Proposto`, `Aceito`, `Substituído`.
Fonte: seção 20 de `../arquitetura_helpdesk_comprove.md` e decisões `D-01`…`D-15` do `docs/backend/CHECKLIST.md`.

| ADR | Título | Status |
|---|---|---|
| [001](ADR-001-servico-independente.md) | Help Desk como serviço independente | Aceito |
| [002](ADR-002-api-e-workers-processos-separados.md) | API e Workers como processos separados | Aceito |
| [003](ADR-003-database-logica-propria.md) | Database lógica própria (`helpdesk_database`) | Aceito |
| [004](ADR-004-transactional-outbox.md) | Transactional Outbox com replica set | Aceito |
| [005](ADR-005-rabbitmq.md) | RabbitMQ como broker | Aceito |
| [006](ADR-006-multi-tenancy.md) | Multi-tenancy desde o início | Aceito |
| [007](ADR-007-classificacao-deterministica.md) | Classificação determinística de severidade | Substituído (012) |
| [008](ADR-008-sla.md) | Matriz de SLA e pausa do relógio | Proposto |
| [009](ADR-009-cache-aside.md) | Cache-aside com `rules_version` | Aceito |
| [010](ADR-010-anexos-mongodb-gridfs.md) | Anexos no MongoDB (GridFS) com URL assinada | Aceito |
| [011](ADR-011-plataforma-dotnet-10.md) | Plataforma .NET 10 LTS | Aceito |
| [012](ADR-012-pontuacao-quatro-perguntas.md) | Severidade pela pontuação de quatro perguntas | Aceito |
| [013](ADR-013-aviso-de-resolucao-whatsapp.md) | Aviso de resolução por WhatsApp (OpenWA) | Aceito |

Pendências: D-12 (canal e endpoint do FAQ — definido quando o backend estiver pronto, com `Comprove:FaqSyncEnabled=false` até lá), D-14 e D-15. D-13 foi resolvido pelo ADR-010.

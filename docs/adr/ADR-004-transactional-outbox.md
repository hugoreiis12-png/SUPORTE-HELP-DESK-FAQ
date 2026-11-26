# ADR-004 — Transactional Outbox com replica set

**Status:** Aceito (antes `Recomendado, pendente`). Validação técnica: transação multi-documento exige replica set.

## Decisão
Ticket, `ticket_history` e `outbox_events` são gravados na mesma transação MongoDB. Um publicador (Worker) lê `outbox_events` pendentes e publica no RabbitMQ, com retry e backoff.

## Consequências
- MongoDB precisa rodar como replica set `rs0` (dev: `New-HelpDeskInfra.ps1`).
- Outbox não elimina duplicidade: consumidores são idempotentes (controle em `processed_messages`).
- Transações são re-entrantes: uma chamada dentro de outra transação reutiliza a sessão.

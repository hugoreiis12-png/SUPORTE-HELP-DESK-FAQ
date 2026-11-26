# ADR-005 — RabbitMQ como broker

**Status:** Aceito (antes `Candidato, não aprovado`). Decisão D-03.

## Decisão
Exchange `helpdesk.topic` (tipo topic), exchange `helpdesk.dlx` para dead-letter, uma fila por consumidor e uma DLQ `<fila>.dlq`. Topologia em `infra/rabbitmq-definitions.json`.

## Consequências
- Mensagem que falha após o limite vai para DLQ com metadados.
- Qualquer nova fila exige entrada na topologia e no `RabbitTopology` de Contracts.

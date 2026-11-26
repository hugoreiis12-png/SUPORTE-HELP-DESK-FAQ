# ADR-013 — Aviso de resolução por WhatsApp (OpenWA)

**Status:** Aceito (canal e origem do celular definidos pelo responsável em 26/11/2026). Texto e gatilho são o padrão proposto, a confirmar.

## Decisão
- O Bubble inclui `name`, `phone` e `roles` no token. Na abertura, o chamado grava os três, na mesma transação do evento `helpdesk.ticket.created`.
- O aviso reage a `helpdesk.ticket.resolved` (somente status Resolvido), no consumidor `TicketResolvedNotifyWorker`.
- O envio usa o OpenWA da VPS: verifica o número e envia para o identificador retornado.
- O envio fica fora da transação do inbox. Duplicidade é barrada pelo histórico (`RequesterNotified`) e pelo `Idempotency-Key` (24 h).
- Só a resolução oficial do FAQ entra no texto; mensagens e notas internas não.
- Reaberto antes do envio, ou sem celular válido: não envia. Chamados anteriores não têm celular.

## Falhas
- 429, 5xx, timeout e envio em andamento: até 6 tentativas (cerca de 31 s) e depois DLQ `ticket.resolved.notify.dlq`.
- Demais 4xx e resultado desconhecido: direto para a DLQ, sem reenvio automático. Não há reprocessamento automático da DLQ.

## Segurança
- Celular é dado pessoal: não é devolvido pela API, não vai em evento da fila nem em log.
- `OpenWa__ApiKey` só por variável de ambiente; chave `operator`, restrita à sessão (e ao IP de saída, se fixo). URL obrigatoriamente `https`.

## Configuração do Worker
`OpenWa__Enabled=true`, `OpenWa__BaseUrl` (sem `/api` e sem `/sessions`), `OpenWa__SessionId`, `OpenWa__ApiKey`. Padrão: desligado.

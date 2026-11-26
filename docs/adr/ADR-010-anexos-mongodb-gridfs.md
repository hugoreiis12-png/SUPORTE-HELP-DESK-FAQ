# ADR-010 — Anexos no MongoDB (GridFS) com URL temporária assinada

**Status:** Aceito (24/11/2026). Resolve D-13.

## Contexto
O provedor de storage de anexos estava em aberto. O responsável decidiu usar o mesmo MongoDB que guarda os dados.

## Decisão
- Binários em GridFS, bucket `attachments`, dentro de `helpdesk_database`.
- Metadados (`ticket_attachments`) continuam em documento, gravados na transação do caso de uso.
- Upload e download usam URL temporária assinada (HMAC-SHA256) apontando para a API: `/api/v1/attachments/upload` e `/api/v1/attachments/download`, com `key`, `exp` e `sig`.
- A chave de assinatura vem de `Storage:LinkSigningKey` (env), com pelo menos 32 caracteres.

## Consequências
- Sem dependência de provedor externo no MVP.
- Blob é gravado antes da transação de metadados. Se a transação falhar, sobra blob órfão: limpeza periódica é trabalho futuro.
- Limite de tamanho é checado por `Content-Length`. Upload sem `Content-Length` é recusado.
- Se o volume de anexos crescer, reavaliar object storage (ADR a criar).

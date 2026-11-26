# ADR-003 — Database lógica própria (`helpdesk_database`)

**Status:** Aceito (antes `Recomendado, pendente`). Confirmado pela decisão D-05.

## Decisão
O cluster MongoDB pode ser compartilhado, mas o Help Desk usa somente a database `helpdesk_database`, com credencial própria de privilégio mínimo. Nenhum código acessa `erp_database`.

## Consequências
- Índices e permissões são governados pela database do Help Desk.
- Dados de usuário e empresa entram por contrato, não por leitura direta de collections do ERP.

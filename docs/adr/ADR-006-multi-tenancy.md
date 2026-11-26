# ADR-006 — Multi-tenancy desde o início

**Status:** Aceito (antes `Recomendado`).

## Decisão
- `tenant_id` vem sempre do token validado, nunca do corpo da requisição.
- Toda query de ticket aplica filtro `tenant_id`.
- Recurso de outro tenant responde `404` (não `403`), para não vazar existência.

## Consequências
- Testes de integração provam isolamento entre dois tenants (`TenantIsolationTests`).

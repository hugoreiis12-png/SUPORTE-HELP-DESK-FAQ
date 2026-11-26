# ADR-009 — Cache-aside com `rules_version`

**Status:** Aceito (confirmado pelo responsável em 24/11/2026).

## Decisão
Apenas regras de configuração (matriz de SLA) são cacheadas em `IMemoryCache`, TTL 10 min, com chave `sla_policy:{rules_version}`. Permissões, notas internas, anexos e estado de ticket **não** são cacheados.

## Consequências
- Mudança de regra = incrementar `Sla:RulesVersion`, o que invalida o cache por chave.
- Redis só entra se houver medição de gargalo (B23).

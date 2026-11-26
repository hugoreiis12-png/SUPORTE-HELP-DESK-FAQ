# ADR-011 — Plataforma .NET 10 LTS

**Status:** Aceito (24/11/2026). Substitui a decisão D-01 (.NET 8).

## Contexto
.NET 8 chegou ao fim de suporte em 10/11/2026. A Microsoft recomenda .NET 10 LTS, com suporte até nov/2028.

## Decisão
- `global.json` fixa `10.0.100` com `rollForward: latestFeature`.
- Todos os projetos usam `net10.0`. Pacotes Microsoft.* em 10.0.x.
- Swashbuckle 10.x para OpenAPI; `Microsoft.AspNetCore.OpenApi` removido (não era usado).

## Consequências
- Projetos existentes em `net8.0` precisam ser ajustados (`TargetFramework`) antes do primeiro build.
- O SDK 10.0.x do sistema atende. Nenhum SDK local é necessário.

# ADR-001 — Help Desk como serviço independente

**Status:** Aceito (premissa do projeto).

## Contexto
O ERP Comprove roda em Bubble. O Help Desk precisa de regras, escala e ciclo de deploy próprios.

## Decisão
O Help Desk é um serviço externo com backend próprio em C#/.NET. O Bubble é apenas frontend e consome a API por HTTPS/REST.

## Consequências
- Bubble não acessa o MongoDB nem o broker.
- Contratos versionados (`/api/v1/...`) são a única interface.

# ADR-002 — API e Workers como processos separados

**Status:** Aceito (antes `Proposto`).

## Contexto
A API responde síncrono; tarefas demoradas ou independentes não podem bloquear a resposta.

## Decisão
`Comprove.HelpDesk.Api` e `Comprove.HelpDesk.Worker` são executáveis separados. Compartilham Domain, Application, Infrastructure e Contracts.

## Consequências
- Escala independente por tipo de carga.
- Ambos aplicam as mesmas regras de domínio; nenhum executável contém regra exclusiva.

# ADR-008 — Matriz de SLA e pausa do relógio

**Status:** Proposto. Aguardando confirmação do responsável. Matriz inicial de D-11.

## Decisão
- Matriz inicial: Crítica 2h, Alta 6h, Média 24h, Baixa 48h, contadas a partir da abertura.
- `AguardandoSolicitante` pausa o relógio (`paused_at`); voltar a `EmAtendimento` soma o tempo pausado em `sla_deadline`.
- `resolved_at` é gravado na resolução.

## Consequências
- Calendário comercial, feriados e fuso ficam fora desta versão (pendência de D-11).

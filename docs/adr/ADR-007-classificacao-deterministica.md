# ADR-007 — Classificação determinística de severidade

**Status:** Substituído pelo [ADR-012](ADR-012-pontuacao-quatro-perguntas.md) em 26/11/2026.

## Decisão
Severidade = soma de três pesos informados no chamado:
- Impacto: Alto 3, Médio 2, Baixo 1
- Escopo: Alto 3, Médio 2, Baixo 1
- Frequência: Alta 2, Média 1, Baixa 0

Total ≥ 7 → Crítica; ≥ 5 → Alta; ≥ 3 → Média; senão Baixa.

## Consequências
- Sem IA nesta fase. IA só entra se houver necessidade medida.
- Regra fica no Domain (`SeverityClassifier`) e é coberta por teste unitário.

# ADR-012 — Severidade pela pontuação de quatro perguntas

**Status:** Aceito (perguntas, pesos e faixas definidos pelo responsável em 26/11/2026). Substitui o ADR-007.

## Decisão
O solicitante responde quatro perguntas; cada resposta vale 25, 15 ou 5 pontos.

| Pergunta (campo) | 25 | 15 | 5 |
|---|---|---|---|
| Abrangência (`scope`) | Várias pessoas, setores ou unidades (`Alto`) | Grupo específico ou equipe (`Medio`) | Uma única pessoa (`Baixo`) |
| Impacto (`impact`) | Bloqueia processos (`Alto`) | Exige alternativa manual ou retrabalho (`Medio`) | Dificuldade pontual (`Baixo`) |
| Frequência (`frequency`) | Contínua ou a cada ação (`Alta`) | Intermitente (`Media`) | Uma única vez (`Baixa`) |
| Urgência (`urgency`) | Intervenção imediata (`Alta`) | Curto prazo (`Media`) | Programada (`Baixa`) |

Soma de 20 a 100: **≥ 90 Crítica; ≥ 70 Alta; ≥ 50 Média; abaixo disso, Baixa.**

## Consequências
- Os pesos ficam só no Domain (`SeverityClassifier`); o front envia apenas o nível de cada resposta.
- O Worker classifica o chamado (`Ticket.Classify`) e o SLA segue o ADR-008.
- Registros anteriores não têm `urgency`: a severidade já gravada é mantida e não há pontuação exibida.
- Chamado antigo ainda sem classificação conta a urgência como `Media` (15 pontos).
- Não há migração de dados.

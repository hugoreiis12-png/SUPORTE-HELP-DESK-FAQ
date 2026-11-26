# Comprove Help Desk

Central de FAQ + Help Desk da Comprove. O front roda **dentro do Bubble** (comprover.bubbleapps.io), o backend é um serviço .NET independente com MongoDB.
Este README é o ponto de entrada para quem assume o projeto. Leia as **Regras que não se quebram** antes de mexer em qualquer coisa.

## Mapa do repositório

| Pasta | O que é |
|---|---|
| `backend/` | Solução .NET 10: `Domain`, `Application`, `Infrastructure`, `Contracts`, `Api`, `Worker`, testes em `tests/` |
| `front_end/SUPORTE-HELP-DESK-/` | React 19 + Vite 8 + Tailwind 4 (bun). Duas telas independentes: `faq.html` e `helpdesk.html` |
| `infra/` | Docker (RabbitMQ) e scripts `infra/mongo/*.js` (criar/verificar/limpar o banco) |
| `scripts/` | PowerShell 5.1 (com BOM): `Start-Infra`, `Start-Api`, `New-DevToken`, `Validate-*` |
| `docs/adr/` | ADR-001 a 011 (decisões). `docs/backend/` tem checklist, código aplicado e **`API-ROTAS-FRONT.md`** (contrato das rotas) |
| `_backup_backend_antes_migracao/` | Backup do backend antigo. Pode apagar quando quiser |

## Regras que não se quebram (regra de negócio)

1. **Chamado nasce do FAQ.** Registrar uma dúvida (`POST /faqs`) cria o FAQ e o chamado juntos, 1:1. O front não abre chamado direto e o Help Desk não tem botão de abrir chamado.
2. **FAQ e Help Desk são duas telas sem atalho entre si.** Não crie link de uma para a outra.
3. **Tenant e usuário vêm só do token.** `tenant_id` e `sub` nunca são lidos do corpo da requisição (ADR-006). Toda consulta filtra por tenant.
4. **Severidade é determinística e do servidor** (ADR-012): abrangência, impacto, frequência e urgência valem 25/15/5 cada; ≥90 Crítica, ≥70 Alta, ≥50 Média, senão Baixa. O front só envia os níveis e não conhece os pesos; os textos das perguntas ficam em `src/data/severityQuiz.ts`. A severidade só é entregue ao suporte.
5. **SLA** (ADR-008, ainda "Proposto"): Crítica 2h, Alta 6h, Média 24h, Baixa 48h. É calculado pelo Worker na classificação. Até lá o front mostra "SLA em cálculo".
6. **Máquina de estados do chamado** (`Domain/Rules/TicketStateMachine.cs`): `Recebido → EmTriagem → AguardandoAtendimento` é do **Worker**. O agente move de `EmAtendimento` em diante: `EmAtendimento ↔ AguardandoSolicitante`, `EmAtendimento → Resolvido → Fechado`. Reabrir (`Resolvido → EmAtendimento`) exige motivo. `Fechado` é terminal. "Novo" no front cobre 3 estados internos e nunca é destino manual.
7. **O status do FAQ acompanha o do chamado:** Novo = Pendente; Em Atendimento/Aguardando Solicitante = Em Análise; Resolvido/Fechado = Resolvido. "Solução de Contorno" só existe via `POST /support/tickets/{id}/resolution`.
8. **Notas internas** só agente cria e só agente vê. O solicitante nunca recebe nota interna.
9. **Escrita multi-passo é transacional e efeito externo é idempotente** (Outbox/Inbox, ADR-004). `idempotencyKey` é obrigatória em abrir FAQ e resolver; repetir a chamada não duplica.
10. **Resolver e publicar no FAQ** (`resolution`) é do agente de suporte e idempotente.
11. **Anexos** ficam no MongoDB (GridFS, ADR-010) e são acessados por link assinado e curto (HMAC). O nome do arquivo nunca vira chave.
12. **Nenhum segredo no repositório.** Senhas e chaves só por variável de ambiente (`ConnectionStrings__HelpDeskMongo`, `BubbleSecurity__SigningKey`, `Storage__LinkSigningKey`).
13. **Regras do harness** (`docs/_rules/nio.md`): arquivo ≤ 300 linhas, função ≤ 30, comentário de 1 linha, early-return, sem `any`, sem código morto, tipos fora do componente, estado de servidor separado do de UI.

## Estado atual

### Backend (.NET 10) — pronto e testado
- Compila sem avisos; **49 testes unitários e 33 de integração passando** (os de integração usam a database `helpdesk_tests` num Mongo em replica set; aponte com `ConnectionStrings__HelpDeskMongo`).
- Categoria sem fila própria cai na fila padrão `suporte-geral`; sem agentes cadastrados, o chamado espera em `AguardandoAtendimento`. Banco já criado: rode `infra/mongo/configurar-filas.js`.
- Worker repete falha passageira até 6 vezes com backoff; falha de regra ou formato vai direto para a DLQ.
- Hexagonal: `Domain` (regras puras) → `Application` (casos de uso, portas) → `Infrastructure` (Mongo, RabbitMQ, GridFS) → `Api`/`Worker`.
- MongoDB 3.14.1 com replica set (`rs0`; transações exigem). Outbox publica no RabbitMQ 7.3; o Worker classifica e despacha chamados.
- **Rotas** (detalhe em `docs/backend/API-ROTAS-FRONT.md`): `GET /me`; FAQ (`GET /faqs`, `GET /faqs/{id}`, `POST /faqs`, `POST /faqs/{id}/upvote`); anexos (`POST /tickets/{id}/attachments` + `PUT` na URL assinada); Help Desk (`GET /support/tickets`, `GET /support/tickets/{id}`, `POST /support/tickets/{id}/resolution`); e as já existentes em `/tickets` (mensagens, transições, resolve, meta).
- CORS por configuração (`Cors:AllowedOrigins`). Autenticação por JWT HS256 assinado pelo Bubble (D-04).
- Coleções novas: `faqs`, `faq_upvotes`, `faq_counters` (índices criados no start).

### Front (React) — pronto e validado contra API simulada
- **Zero dado fixo.** Tudo vem da API: `src/api/*` (cliente, DTOs, mapeadores), `src/state/*` (sessão, FAQ, chamados, estado remoto).
- Sessão vem do Bubble por `postMessage` (contrato em `src/state/sessionBridge.ts` e no doc de rotas). Em dev local, o token é digitado na tela (campo "Token de desenvolvimento", só com `import.meta.env.DEV`; nunca vai para o bundle).
- Loading, erro com "tentar de novo" e vazio em ambas as telas; atualização automática a cada 20 s.
- Help Desk só para `support_agent` (outros veem "Acesso restrito"). Sidebar de acesso rápido com as 107 telas do Comprove já existe, **ainda sem navegação real** (`handleNavigateToScreen`).
- Build para o Bubble: `node scripts/build-bubble.mjs` gera `dist-bubble/faq.bubble.html` (~1,4 MB) e `helpdesk.bubble.html` (~0,5 MB). **Exige `VITE_API_BASE_URL`** com URL HTTPS (ou localhost) e imprime a URL gravada. O último build foi feito com o texto de exemplo `https://SUA-API-PUBLICA`: **precisa ser refeito com a URL real.**
- As imagens dos personagens da ilustração do FAQ estão embutidas no bundle (o Bubble bloqueava o servidor externo).

### O que foi validado e o que não foi
- Validado: compilação, testes unitários, type-check do front, telas contra API simulada (estado vazio, com dados, erro do servidor).
- Validado em 25/11/2026, com API, Worker, Mongo e RabbitMQ descartáveis: registrar dúvida, triagem, despacho, resposta, resolução repetida e queda do Mongo.
- **Não validado:** o banco do servidor, o front contra a API real e o handshake de token dentro do Bubble.

## Próximas tarefas (em ordem)

1. **Validar em local com o backend real.** Reiniciar API e Worker (`scripts\Start-Api.ps1`), gerar token (`scripts\New-DevToken.ps1 -Role agent -Name ... -Email ...`), digitá-lo na tela de erro (campo "Token de desenvolvimento") e rodar `bun run dev`. Para testar pela rede local, use `scripts\Start-Api.ps1 -Lan`. Testar: registrar dúvida com imagem, ver o chamado no Help Desk, responder, mudar status, "Resolver e sincronizar". Corrigir o que aparecer. Rodar também os testes de integração.
2. **Publicar a API em HTTPS público**, com proxy reverso e certificado válido, num lugar que alcance o Mongo. Configurar `Storage__PublicBaseUrl` (URL pública, com `/` final), `Cors__AllowedOrigins__*`, `BubbleSecurity__SigningKey` (≥ 32 caracteres) e `Storage__LinkSigningKey`. Conferir `https://api/health`.
3. **Decidir e implementar a emissão do token.** O JWT não pode ser assinado no navegador. Opção recomendada: nova rota `POST /auth/token` na API, protegida por uma chave secreta que só o backend workflow do Bubble conhece, que devolve o JWT com `sub`, `tenant_id`, `roles`, `name`, `email`, `unit` (TTL 5 min). Alternativa: plugin de JWT no Bubble. **Pendente de decisão do usuário.**
4. **Ligar a sessão no Bubble.** Colocar em cada página o script de `postMessage` do doc de rotas, chamando o workflow do passo 3. Help Desk: `suporte_helpdesk` (id `bXgZo0`); FAQ: `faq` (`bXgZi0`). `suporte-comprover` (`bXgJV0`) está vazia. As duas páginas têm colado o protótipo antigo e não têm workflow.
5. **Gerar o build final** com a URL real e colar `faq.bubble.html` na página `faq` e `helpdesk.bubble.html` na página do Help Desk (campo HTML do elemento, substituindo o conteúdo).
6. **Navegação real da sidebar** do Help Desk: trocar `handleNavigateToScreen` (em `src/screens/HelpDeskScreen.tsx`) por navegação para a página `screen.slug` do Bubble.
7. **Teste final no Bubble:** FAQ como usuário comum, Help Desk como suporte, e usuário sem papel de suporte vendo "Acesso restrito".

### Backlog (decidir antes de fazer)
- Confirmar a **ADR-008** (SLA, hoje "Proposto"); sincronização do FAQ com o Comprove (**D-12**, `Comprove:FaqSyncEnabled=false`).
- Tempo real: trocar o polling pelo hub SignalR (`/hubs/tickets`, já existe no backend).
- Reações (emoji) do FAQ ficam só no navegador (`FaqSplitPane`); decidir se ganham rota.
- Foto de perfil é preferência local por usuário; o servidor não guarda.
- O fluxo do front deixa o agente tentar transições que a máquina de estados recusa (ex.: chamado ainda em triagem); hoje o erro do servidor aparece na tela. Opcional: esconder as opções inválidas.
- Rodar `APAGAR_DADOS_DE_TESTE` de volta para `false` em `infra/mongo/limpar-dados-de-teste.js` se ainda estiver ligado. O Pencil (recriar a interface no .pen) está bloqueado por limite de plano.

## Como rodar em local

```powershell
# 1. Infra: RabbitMQ (Mongo já roda no servidor 192.168.0.142, instância 27018, acessada por túnel SSH)
scripts\Start-Infra.ps1
# 2. API e Worker
scripts\Start-Api.ps1
# 3. Front
cd front_end\SUPORTE-HELP-DESK-
bun install
bun run dev        # http://localhost:3000 (/faq.html e /helpdesk.html)
bun run lint       # type-check
node scripts\build-bubble.mjs   # exige VITE_API_BASE_URL
```

Testes do backend: `dotnet test backend\Comprove.HelpDesk.sln` (os de integração precisam do Mongo). Se as DLLs estiverem travadas por uma API em execução, pare-a ou use `-p:OutDir=<pasta temporária>`.

## Armadilhas já conhecidas

- **PowerShell 5.1:** scripts `.ps1` precisam de BOM; ao editar, não toque na primeira linha.
- **Nome `TenantId`:** em documentos Mongo, a propriedade string `TenantId` sombreia o tipo `TenantId`. Use alias (`DomainTenantId`), como em `FaqDocument.cs`.
- **Windows é case-insensitive:** arquivos com nomes que só diferem na caixa colidem.
- **Mongo:** `DateTimeOffset` é registrado como data BSON em `MongoSerialization.cs`; transações exigem replica set; a API chega ao Mongo por túnel (`directConnection=true`).
- **Bubble:** o elemento HTML aceita ~1,4 MB; se recusar, gerar versão com imagens recomprimidas. O MCP do Bubble lista ferramentas de escrita, mas a config acusa `firebase: ausente` (escrita de design volta 401), então a colagem é manual. O servidor do MCP (`192.168.0.160:8787`) pode estar fora do ar.
- **Segurança:** senhas e credenciais compartilhadas em conversas anteriores (SSH do servidor Mongo, login do Bubble) devem ser **trocadas**. Nunca imprimir o token do `opencode.json`.
- **Porta 3000** pode estar ocupada por um Vite antigo; o novo sobe em 3001 e o CORS de dev só libera 3000, então encerre o antigo ou ajuste.

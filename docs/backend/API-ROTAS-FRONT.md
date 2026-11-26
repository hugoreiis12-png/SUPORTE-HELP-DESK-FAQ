# API do Help Desk — rotas usadas pelo front

Base: `{VITE_API_BASE_URL}/api/v1`. Toda rota exige `Authorization: Bearer <JWT do Bubble>` (exceto os links assinados de anexo).
Erros vêm como `ProblemDetails` com `correlation_id`: 422 regra de negócio, 404 não encontrado, 403 sem permissão, 409 conflito.

## Rotas

| # | Método e rota | Quem | Para que serve | Tela |
|---|---|---|---|---|
| 1 | `GET /me` | todos | Identidade do token (nome, e-mail, unidade, `isSupportAgent`) | ambas |
| 2 | `GET /faqs?status&search&cursor&pageSize` | todos | Lista paginada de FAQs, já com `upvotedByMe` e `image` | FAQ |
| 3 | `GET /faqs/{faqId}` | todos | Um FAQ | FAQ |
| 4 | `POST /faqs` | todos | Registra a dúvida: **cria o FAQ e abre o chamado vinculado** (idempotente por `idempotencyKey`) | FAQ |
| 5 | `POST /faqs/{faqId}/upvote` | todos | Alterna "também tenho essa dúvida"; devolve `{askCount, upvoted}` | FAQ |
| 6 | `POST /tickets/{ticketId}/attachments` | dono do chamado / agente | Pede URL assinada de upload da imagem | FAQ |
| 7 | `PUT {uploadUrl}` | quem recebeu a URL | Envia o binário (a assinatura é a autorização, sem Bearer) | FAQ |
| 8 | `GET /support/tickets?status&cursor&pageSize` | agente | Fila do Help Desk: chamado + FAQ + mensagens (com notas internas) + imagem | Help Desk |
| 9 | `GET /support/tickets/{ticketId}` | agente | Um chamado no mesmo formato | Help Desk |
| 10 | `POST /tickets/{ticketId}/messages` | agente | Resposta pública ou nota interna (`visibility`: `Public`/`Internal`) | Help Desk |
| 11 | `POST /tickets/{ticketId}/transitions` | agente | Muda o status (`targetStatus`: `Em Atendimento`, `Aguardando Solicitante`, `Resolvido`) | Help Desk |
| 12 | `POST /support/tickets/{ticketId}/resolution` | agente | "Resolver e sincronizar": move o chamado, grava a resolução oficial no FAQ e registra no histórico | Help Desk |

Já existiam e continuam: `POST/GET /tickets`, `GET /tickets/{id}`, `PATCH /tickets/{id}/meta`, `POST /tickets/{id}/resolve`, `GET /health`.
O front **não** abre chamado direto: chamado nasce do FAQ (rota 4).

### Corpos principais

- `POST /faqs`: `{ title, problemDescription, problemField, specificScreen, bugType, bugSubType, scope: Alto|Medio|Baixo, impact: Alto|Medio|Baixo, frequency: Alta|Media|Baixa, urgency: Alta|Media|Baixa, idempotencyKey }`.
  `problemField` e `bugType` são listas fechadas (`Domain/Rules/FaqTaxonomy.cs`). A severidade é calculada no servidor (ADR-012); `POST /tickets` também exige `urgency`.
- `POST /tickets/{id}/transitions`: `{ targetStatus, reason? }`. "Novo" não é destino manual. Reabrir exige `reason` (o front já envia).
- `POST /tickets/{id}/attachments`: só `image/png`, `image/jpeg`, `image/webp` e `image/gif` (422 para o resto). O `PUT` maior que o `sizeBytes` declarado volta 413.
- `POST /support/tickets/{id}/resolution`: `{ resolutionSummary, faqStatus: Resolvido|Solução de Contorno|Em Análise|Pendente, idempotencyKey }`.

### Visibilidade da severidade

`severity`, `severityScore` e `severityBreakdown` (rotas de FAQ) e `severity`, `severityScore`, `impact`, `scope`, `frequency` e `urgency` (rotas de `/tickets`) só vêm preenchidos para `support_agent`; para os demais chegam `null`.

## Token (D-04)

JWT HS256 assinado pelo Bubble. Claims: `sub`, `tenant_id`, `roles` (`support_agent` para suporte), `iss=bubble-comprove`, `aud=helpdesk-api`, `exp`, `iat`, `jti`.
Opcionais: `name`, `email`, `unit` (o Help Desk mostra solicitante e autor das mensagens a partir delas) e `phone`.
`phone`: celular do próprio usuário, de preferência com país e DDD (`5521999999999`); sem `+`, 10 ou 11 dígitos são tratados como Brasil. Sem ele não há aviso de resolução (ADR-013).
O workflow do Bubble deve preencher `sub`, `name`, `phone` e `roles` a partir do **usuário logado no servidor**, nunca de valores enviados pela página.
A assinatura precisa acontecer **no servidor do Bubble** (backend workflow/plugin); nunca no navegador, para não expor a chave.

## Contrato da página do Bubble com o front

O front roda num iframe da mesma origem do Bubble e conversa por `postMessage`:

```html
<script>
  window.addEventListener('message', function (event) {
    if (event.origin !== window.location.origin) return;
    var type = event.data && event.data.type;
    if (type === 'helpdesk:ready' || type === 'helpdesk:token-request') {
      // obterTokenDoBubble(): devolve o JWT emitido pelo backend workflow do Bubble.
      obterTokenDoBubble().then(function (token) {
        event.source.postMessage({ type: 'helpdesk:session', token: token }, window.location.origin);
      });
    }
    if (type === 'helpdesk:logout') { /* acionar o logout do Bubble */ }
  });
</script>
```

O token vale 5 minutos (`TokenTtlSeconds`). O front renova sozinho: antes de cada chamada, se o token vence em menos de 30 s, e depois de um 401, ele envia `helpdesk:token-request` e espera o host responder.

## Configuração necessária no servidor

- `Cors:AllowedOrigins`: já contém `https://comprover.bubbleapps.io` e `http://localhost:3000`. Acrescente o domínio próprio, se houver.
- `Storage:PublicBaseUrl`: URL **pública HTTPS** da API. As URLs de upload/download são montadas com ela; vazia, qualquer rota que precise assinar link (upload da imagem, ou listar FAQ/chamado que tenha imagem) responde 500 com essa mensagem. Em desenvolvimento já vale `http://localhost:5112/`.
- A API precisa estar em HTTPS público: a página do Bubble bloqueia chamada HTTP (conteúdo misto). O Mongo continua atrás do túnel/rede interna, só a API é exposta.

## Build do front

```powershell
cd front_end\SUPORTE-HELP-DESK-
$env:VITE_API_BASE_URL = "https://SUA-API-PUBLICA"
node scripts\build-bubble.mjs
```

O script recusa URL vazia ou `http://` que não seja localhost, e imprime a URL gravada. Em dev local, digite o token na tela (gerado por `scripts\New-DevToken.ps1 -Role agent`); sem `VITE_API_BASE_URL`, a API é o host da página, porta 5112.

## Regras que o servidor aplica (o front só exibe o erro)

- Fluxo de status: `Recebido → EmTriagem → AguardandoAtendimento` é do Worker; o agente move a partir de `Em Atendimento`.
  Mexer num chamado ainda em triagem devolve 422 ("Transição inválida").
- Status do FAQ acompanha o do chamado: Novo=Pendente, Em Atendimento/Aguardando Solicitante=Em Análise, Resolvido=Resolvido.
  "Solução de Contorno" só existe via `resolution`.
- Chamado Resolvido reaberto exige motivo; chamado Fechado não aceita alterações.
- SLA (`slaDeadline`) aparece depois da classificação pelo Worker; até lá o front mostra "SLA em cálculo".

## Ainda não existe (decisão pendente)

- Reações (emoji) do FAQ: ficam só no navegador de quem usa, sem rota.
- Tempo real: a tela atualiza a cada 20 s. O hub SignalR (`/hubs/tickets`) já existe no backend e pode substituir o polling.
- Foto de perfil: preferência local por usuário; o servidor não guarda foto.
- Sincronização do FAQ com o Comprove (D-12, `FaqSyncEnabled=false`).

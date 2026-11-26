# Deploy do Help Desk no servidor .160

Sobe **API, Worker, RabbitMQ** e o **túnel SSH** até o MongoDB do `.142`. Requer Docker com Compose 2.20 ou mais novo.

## 1. No .142: chave exclusiva para o túnel
No `.160`, dentro de `deploy/`:

```bash
mkdir -p secrets && chmod 700 secrets
ssh-keygen -t ed25519 -N '' -C helpdesk-tunnel -f secrets/tunnel_key
ssh-keyscan -t ed25519 192.168.0.142 > secrets/known_hosts
```

**Confira a identidade antes de continuar:** compare `ssh-keygen -lf secrets/known_hosts` com a saída de `ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub` executada no próprio `.142`.

No `.142`, crie um usuário sem sudo (ex.: `helpdesk-tunnel`) e coloque no `~/.ssh/authorized_keys` dele, em uma única linha:

```
restrict,port-forwarding,permitopen="127.0.0.1:27018",from="192.168.0.160" ssh-ed25519 AAAA... helpdesk-tunnel
```

A chave só permite alcançar a porta do MongoDB: sem terminal e sem outros destinos.

## 2. Configuração
```bash
cp .env.example .env && chmod 600 .env
```
Preencha o `.env`. Use **segredos novos de produção** (não os de desenvolvimento). A `BUBBLE_SIGNING_KEY` é a mesma do workflow do Bubble que assina o token.

## 3. HTTPS (obrigatório para o Bubble)
O navegador do usuário chama a API direto, então ela precisa de HTTPS acessível de onde os usuários estiverem. Escolha uma opção:

| Opção | `COMPOSE_PROFILES` | Precisa |
|---|---|---|
| Cloudflare Tunnel | `tunnel,cloudflare` | Domínio na Cloudflare e `CLOUDFLARE_TUNNEL_TOKEN`; no painel, aponte o hostname para `http://api:8080` |
| Caddy | `tunnel,caddy` | `PUBLIC_DOMAIN` apontando para o servidor e portas 80 e 443 abertas |
| Proxy que já existe | `tunnel` | `API_BIND=0.0.0.0` e firewall liberando a porta só para o proxy |

`PUBLIC_API_URL` é essa URL pública, terminada em `/`. A API não limita requisições por cliente: se for pública, configure o limite no proxy ou na Cloudflare.

## 4. Subir e conferir
```bash
docker compose up -d --build
docker compose ps
curl -s http://127.0.0.1:5112/health
```
`/health` com `Healthy` confirma Mongo e RabbitMQ. O Swagger fica desligado em produção.

## 5. Ligar o aviso por WhatsApp
Depois de testar a abertura e a resolução: preencha `OPENWA_*`, ponha `OPENWA_ENABLED=true` e rode `docker compose up -d worker`. A partir daí, **toda resolução de chamado com celular envia mensagem real**. Detalhes em `docs/adr/ADR-013-aviso-de-resolucao-whatsapp.md`.

## Operação
| Tarefa | Comando |
|---|---|
| Logs | `docker compose logs -f api worker` |
| Filas (inclui falhas, sufixo `.dlq`) | `docker compose exec rabbitmq rabbitmqctl list_queues name messages` |
| Atualizar versão | `docker compose up -d --build` |
| Parar sem apagar dados | `docker compose down` (não use `-v`: apaga as filas) |

- **Não rode junto o Worker de desenvolvimento** apontado ao mesmo banco: os dois publicariam o mesmo outbox, cada um no seu RabbitMQ.
- `rabbitmq-topology.json` é a versão sem usuários de `infra/rabbitmq-definitions.json`: ao criar uma fila, altere os dois.
- A imagem do túnel usa Debian porque, na rede de desenvolvimento, o repositório do Alpine não respondia por HTTPS.

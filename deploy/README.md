# Deploy do Help Desk no servidor .160

Sobe **API, Worker, RabbitMQ** e o **túnel SSH** até o MongoDB do `.142`, por uma stack do **Portainer** apontada para o Git.
Requer Docker standalone (não Swarm, que não faz `build`).

## 1. No .160: chave exclusiva do túnel
Fica **fora do repositório**, em uma pasta do servidor:

```bash
mkdir -p /opt/helpdesk-secrets && chmod 700 /opt/helpdesk-secrets
ssh-keygen -t ed25519 -N '' -C helpdesk-tunnel -f /opt/helpdesk-secrets/tunnel_key
ssh-keyscan -t ed25519 192.168.0.142 > /opt/helpdesk-secrets/known_hosts
cat /opt/helpdesk-secrets/tunnel_key.pub
```

**Confira a identidade antes de continuar:** compare `ssh-keygen -lf /opt/helpdesk-secrets/known_hosts` com a saída de `ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub` executada no próprio `.142`.

No `.142`, crie um usuário sem sudo (`helpdesk-tunnel`) e coloque no `~/.ssh/authorized_keys` dele, em uma única linha:

```
restrict,port-forwarding,permitopen="127.0.0.1:27018",from="192.168.0.160" ssh-ed25519 AAAA... helpdesk-tunnel
```

A chave só permite alcançar a porta do MongoDB: sem terminal e sem outros destinos.

## 2. Variáveis
Copie `gerar-env.sh` para uma pasta do servidor e rode `bash gerar-env.sh`: ele cria um `.env` ali, com chaves novas de produção.
O conteúdo vai nas variáveis da stack (passo 3). Guarde uma cópia segura; o Git não a tem.
A `BUBBLE_SIGNING_KEY` é a mesma do workflow do Bubble que assina o token.

## 3. Stack no Portainer
**Stacks → Add stack**, nome `helpdesk`, método **Repository**:

| Campo | Valor |
|---|---|
| Repository URL | endereço `https://` do repositório |
| Repository reference | `refs/heads/main` |
| Compose path | `deploy/docker-compose.yml` |
| Authentication | usuário do GitHub e um token *fine-grained* só deste repositório, com **Contents: Read-only** |
| Environment variables | **Advanced mode** e cole o conteúdo do `.env` |
| GitOps / atualização automática | desligada: cada deploy é decidido por você |

As imagens são construídas no próprio servidor (`pull_policy: build`), que precisa alcançar o Docker Hub, o `mcr.microsoft.com`, o NuGet e o `deb.debian.org`.
O container `rabbitmq-init` termina com código 0: é o esperado.

## 4. HTTPS (obrigatório para o Bubble)
| Situação | O que fazer |
|---|---|
| `cloudflared` já instalado no servidor | No painel do túnel, aponte o hostname para `HTTP` `127.0.0.1:5112`. Nada nas variáveis |
| `cloudflared` em container | `COMPOSE_PROFILES=cloudflare` e `CLOUDFLARE_TUNNEL_TOKEN`; destino `http://api:8080` |
| Caddy | `COMPOSE_PROFILES=caddy` e `PUBLIC_DOMAIN`, com portas 80 e 443 abertas |
| Proxy que já existe em outra máquina | `API_BIND=0.0.0.0` e firewall liberando a porta só para o proxy |

`PUBLIC_API_URL` é a URL pública, terminada em `/`. A API não limita requisições por cliente: configure o limite no proxy ou na Cloudflare.

Confira no servidor com `curl -s http://127.0.0.1:5112/health` (`Healthy` confirma Mongo e RabbitMQ). O Swagger fica desligado em produção.

## 5. Ligar o aviso por WhatsApp
Depois de testar abertura e resolução: nas variáveis da stack, preencha `OPENWA_API_KEY`, ponha `OPENWA_ENABLED=true` e atualize a stack. A partir daí, **toda resolução de chamado com celular envia mensagem real**. Detalhes em `docs/adr/ADR-013-aviso-de-resolucao-whatsapp.md`.

## Operação
| Tarefa | Como |
|---|---|
| Atualizar versão | `git push` e, na stack, **Pull and redeploy** |
| Logs | Containers → `api` / `worker` → Logs |
| Filas (falhas têm sufixo `.dlq`) | Console do container `rabbitmq`: `rabbitmqctl list_queues name messages` |
| Voltar versão | Reverter o commit no Git e **Pull and redeploy** |

- **Não remova os volumes** ao apagar a stack: o volume do RabbitMQ guarda as filas.
- **Não rode junto o Worker de desenvolvimento** apontado ao mesmo banco: os dois publicariam o mesmo outbox, cada um no seu RabbitMQ.
- `rabbitmq-init/topology.json` é a versão sem usuários de `infra/rabbitmq-definitions.json`: ao criar uma fila, altere os dois.
- A imagem do túnel usa Debian porque, na rede de desenvolvimento, o repositório do Alpine não respondia por HTTPS.

## Sem Portainer (linha de comando)
Com o repositório clonado: gere o `.env` dentro de `deploy/` e rode `docker compose up -d --build`.

#!/usr/bin/env bash
# Cria o .env de produção no servidor: gera as chaves aqui mesmo e não as exibe.
# Cenário: cloudflared já instalado no host e MongoDB no .142 pelo túnel SSH.
set -euo pipefail
cd "$(dirname "$0")"

if [ -e .env ]; then
  echo "Já existe um .env: apague ou renomeie antes de gerar outro (as chaves mudariam)." >&2
  exit 1
fi
command -v openssl >/dev/null || { echo "Instale o openssl." >&2; exit 1; }

# A senha vai dentro de uma URL: caracteres especiais precisam ser codificados.
urlencode() {
  local LC_ALL=C text="$1" out="" char hex i
  for ((i = 0; i < ${#text}; i++)); do
    char="${text:i:1}"
    case "$char" in
      [a-zA-Z0-9.~_-]) out+="$char" ;;
      *)
        printf -v hex '%%%02X' "$(($(printf '%d' "'$char") & 255))"
        out+="$hex"
        ;;
    esac
  done
  printf '%s' "$out"
}

read -r -p "Endereço público da API, sem https:// (ex.: api-helpdesk.seudominio.com): " api_host
[[ "$api_host" =~ ^[A-Za-z0-9.-]+\.[A-Za-z]{2,}$ ]] || { echo "Endereço inválido." >&2; exit 1; }
read -r -s -p "Senha do usuário helpdesk_app no MongoDB (não aparece na tela): " mongo_password
echo
[ -n "$mongo_password" ] || { echo "Senha vazia." >&2; exit 1; }

umask 077
cat > .env <<EOF
TUNNEL_SECRETS_DIR=/opt/helpdesk-secrets

PUBLIC_API_URL=https://${api_host}/

BUBBLE_SIGNING_KEY=$(openssl rand -base64 48 | tr -d '\r\n')
STORAGE_LINK_SIGNING_KEY=$(openssl rand -base64 48 | tr -d '\r\n')

RABBITMQ_USER=helpdesk
RABBITMQ_PASSWORD=$(openssl rand -hex 32 | tr -d '\r\n')

MONGO_CONNECTION_STRING=mongodb://helpdesk_app:$(urlencode "$mongo_password")@mongo-tunnel:27018/helpdesk_database?authSource=admin&directConnection=true
MONGO_TUNNEL_TARGET=helpdesk-tunnel@192.168.0.142
MONGO_TUNNEL_REMOTE_PORT=27018

OPENWA_ENABLED=false
OPENWA_BASE_URL=https://wa.srv1638233.hstgr.cloud
OPENWA_SESSION_ID=cd97229a-d43c-4353-bf5e-9083963ab36b
OPENWA_API_KEY=
EOF

echo ".env criado com permissão 600. Preencha OPENWA_API_KEY apenas quando for ligar o WhatsApp."

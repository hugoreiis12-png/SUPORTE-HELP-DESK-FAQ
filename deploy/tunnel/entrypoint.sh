#!/bin/sh
set -eu

: "${TUNNEL_TARGET:?defina MONGO_TUNNEL_TARGET (usuario@host)}"
for file in tunnel_key known_hosts; do
  [ -f "/run/tunnel/$file" ] || { echo "Arquivo ausente em secrets/: $file" >&2; exit 1; }
done

# A chave montada pode vir com permissões abertas; o ssh só aceita uma cópia restrita.
install -d -o tunnel -m 700 /tmp/ssh
install -o tunnel -m 600 /run/tunnel/tunnel_key /tmp/ssh/key
install -o tunnel -m 600 /run/tunnel/known_hosts /tmp/ssh/known_hosts

# A identidade do servidor precisa estar em known_hosts: nada é aceito automaticamente.
exec setpriv --reuid=tunnel --regid=tunnel --init-groups ssh -N \
  -i /tmp/ssh/key -p "${TUNNEL_SSH_PORT:-22}" \
  -o IdentitiesOnly=yes -o BatchMode=yes \
  -o StrictHostKeyChecking=yes -o UserKnownHostsFile=/tmp/ssh/known_hosts \
  -o ExitOnForwardFailure=yes -o ServerAliveInterval=30 -o ServerAliveCountMax=3 \
  -L "0.0.0.0:27018:127.0.0.1:${TUNNEL_REMOTE_PORT:-27018}" \
  "$TUNNEL_TARGET"

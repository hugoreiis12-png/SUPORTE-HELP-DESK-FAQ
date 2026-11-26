# Túnel SSH até o MongoDB do .142.
FROM debian:bookworm-slim
RUN apt-get update \
 && apt-get install -y --no-install-recommends openssh-client netcat-openbsd \
 && rm -rf /var/lib/apt/lists/* \
 && useradd --create-home tunnel
COPY entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh
ENTRYPOINT ["/entrypoint.sh"]

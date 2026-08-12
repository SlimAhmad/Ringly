FROM coturn/coturn:4.17.2-r0-alpine

USER root

RUN apk add --no-cache openssl && \
    mkdir -p /etc/coturn/certs && \
    openssl req -x509 -newkey rsa:2048 -sha256 -days 365 -nodes \
      -keyout /etc/coturn/certs/turn_server_pkey.pem \
      -out /etc/coturn/certs/turn_server_cert.pem \
      -subj "/CN=coturn.ringly.local"

COPY turnserver.conf /etc/coturn/turnserver.conf

CMD ["-c", "/etc/coturn/turnserver.conf"]

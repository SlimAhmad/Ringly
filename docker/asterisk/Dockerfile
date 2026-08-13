# Asterisk 23.4.1 — satisfies §5.3's >= 20.20/22.10/23.4 requirement for
# StasisBroadcast support (verified via andrius/asterisk's supported-builds
# matrix: github.com/andrius/asterisk).
FROM andrius/asterisk:23.4.1_debian-trixie

USER root

# Self-signed dev TLS cert for WSS (§6 item 1). Dev-only — replace with a
# real certificate (Let's Encrypt or internal CA) for any non-local deployment.
RUN mkdir -p /etc/asterisk/keys && \
    openssl req -x509 -newkey rsa:2048 -nodes -days 365 \
        -keyout /etc/asterisk/keys/asterisk.key \
        -out /etc/asterisk/keys/asterisk.crt \
        -subj "/CN=localhost"

COPY config/http.conf /etc/asterisk/
COPY config/pjsip.conf /etc/asterisk/
COPY config/extensions.conf /etc/asterisk/
COPY config/ari.conf /etc/asterisk/
COPY config/manager.conf /etc/asterisk/
COPY config/modules.conf /etc/asterisk/
COPY config/logger.conf /etc/asterisk/
COPY config/res_pgsql.conf /etc/asterisk/
COPY config/extconfig.conf /etc/asterisk/
COPY config/sorcery.conf /etc/asterisk/
COPY config/websocket_client.conf /etc/asterisk/

RUN chown -R asterisk:asterisk /etc/asterisk/

USER asterisk

CMD ["/usr/sbin/asterisk", "-vvvdddf", "-W", "-U", "asterisk", "-p"]

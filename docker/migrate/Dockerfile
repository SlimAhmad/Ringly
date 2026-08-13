# One-shot migration runner: applies Asterisk's OFFICIAL realtime config
# schema (contrib/ast-db-manage) via Alembic. We sparse-clone the upstream
# Asterisk repo at build time rather than vendoring a hand-copied schema —
# always the real, maintained migrations, never a guessed one.
FROM python:3.12-slim

RUN apt-get update && \
    apt-get install -y --no-install-recommends git ca-certificates && \
    rm -rf /var/lib/apt/lists/*

RUN pip install --no-cache-dir alembic sqlalchemy psycopg2-binary

RUN git clone --depth 1 --filter=blob:none --sparse https://github.com/asterisk/asterisk.git /src && \
    git -C /src sparse-checkout set contrib/ast-db-manage

WORKDIR /src/contrib/ast-db-manage
COPY config.ini ./config.ini
COPY apply_seed.py /apply_seed.py

CMD alembic -c config.ini upgrade head && python /apply_seed.py

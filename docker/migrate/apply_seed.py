"""Applies docker/asterisk/seed-test-endpoint.sql after Alembic's schema migration —
mounted into this container by docker-compose.yml (not baked into the image, so editing
the seed file doesn't require a rebuild). Uses ON CONFLICT DO NOTHING in the SQL itself,
so this is safe to run on every `docker compose up -d`, not just a fresh volume.
"""

import psycopg2

DATABASE_URL = "postgresql://asterisk:asterisk@postgres:5432/asterisk"
SEED_FILE_PATH = "/seed/seed-test-endpoint.sql"

with open(SEED_FILE_PATH, "r", encoding="utf-8") as seed_file:
    seed_sql = seed_file.read()

connection = psycopg2.connect(DATABASE_URL)
try:
    with connection.cursor() as cursor:
        cursor.execute(seed_sql)
    connection.commit()
    print("Applied seed-test-endpoint.sql.")
finally:
    connection.close()

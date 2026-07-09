#!/usr/bin/env bash
set -euo pipefail

CMD="${1:-migrate}"
NETWORK="${ORDEVO_NETWORK:-ordevo_default}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MIGRATIONS_DIR="$SCRIPT_DIR/../backend/db/migrations"
CONF_FILE="$SCRIPT_DIR/../backend/db/flyway.conf"

docker run --rm \
  --network "$NETWORK" \
  -v "$MIGRATIONS_DIR:/flyway/sql:ro" \
  -v "$CONF_FILE:/flyway/conf/flyway.conf:ro" \
  flyway/flyway:11 \
  "$CMD"

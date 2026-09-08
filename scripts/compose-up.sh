#!/usr/bin/env bash
# Builds and starts the container stack from docker/docker-compose.yml, creating docker/.env from
# .env.example with a generated signing key on first use, then waits for the API's health check.
# Variables in the shell override docker/.env, for example:
#   ASPNETCORE_ENVIRONMENT=Development RateLimits__AuthPermitLimit=1000 scripts/compose-up.sh

# shellcheck disable=SC1091
. "$(dirname "${BASH_SOURCE[0]}")/lib/common.sh"

DOCKER_DIR="$REPO_ROOT/docker"
ENV_FILE="$DOCKER_DIR/.env"

if [ ! -f "$ENV_FILE" ]; then
  cp "$DOCKER_DIR/.env.example" "$ENV_FILE"
  echo "created docker/.env from .env.example"
fi
if ! grep -q '^Jwt__SigningKey=.\{32,\}' "$ENV_FILE"; then
  key="$(head -c 48 /dev/urandom | base64 | tr -d '\n')"
  sed -i "s|^Jwt__SigningKey=.*|Jwt__SigningKey=$key|" "$ENV_FILE"
  echo "generated Jwt__SigningKey in docker/.env"
fi

web_port="$(grep -E '^WEB_PORT=' "$ENV_FILE" | cut -d= -f2)"
api_port="$(grep -E '^API_PORT=' "$ENV_FILE" | cut -d= -f2)"
web_url="http://localhost:${WEB_PORT:-${web_port:-4200}}"
api_url="http://localhost:${API_PORT:-${api_port:-5000}}"

docker compose --project-directory "$DOCKER_DIR" up --build --detach --wait "$@"
wait_for_url "$api_url/health" 60
wait_for_url "$web_url/" 30
echo "stack is up: client at $web_url, API at $api_url"
echo "logs: docker compose --project-directory docker logs -f; stop: scripts/compose-down.sh"

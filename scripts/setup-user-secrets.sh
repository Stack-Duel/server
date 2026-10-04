#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"
PROJECT_PATH="$REPO_ROOT/StackDuel.Api/StackDuel.Api.csproj"
SKIP_TOKEN="__SKIP__"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI was not found in PATH." >&2
  exit 1
fi

if [ ! -f "$PROJECT_PATH" ]; then
  echo "Could not find project at $PROJECT_PATH" >&2
  exit 1
fi

read_value() {
  local prompt="$1"
  local default_value="${2-}"
  local secret="${3-false}"
  local value=""

  if [ -n "$default_value" ]; then
    prompt="$prompt [$default_value]"
  fi

  if [ "$secret" = "true" ]; then
    read -r -s -p "$prompt: " value
    echo >&2
  else
    read -r -p "$prompt: " value
  fi

  if [ "$value" = "skip" ] || [ "$value" = "SKIP" ]; then
    printf '%s' "$SKIP_TOKEN"
    return
  fi

  if [ -z "$value" ]; then
    value="$default_value"
  fi

  printf '%s' "$value"
}

set_secret() {
  local key="$1"
  local value="$2"

  if [ "$value" = "$SKIP_TOKEN" ] || [ -z "$value" ]; then
    echo "Skipping $key"
    return
  fi

  dotnet user-secrets set --project "$PROJECT_PATH" "$key" "$value" >/dev/null
  echo "Set $key"
}

echo "Configure StackDuel.Api user secrets from appsettings values."
echo "Press Enter to accept a default value when shown, or type 'skip' to leave a key unchanged."
echo

KEYS=(
  "Auth0:Audience"
  "Auth0:Domain"
  "ConnectionStrings:DefaultConnection"
  "AzureStorage:ConnectionString"
  "AzureStorage:AvatarContainerName"
  "ExecutionEngines:Judge0:Enabled"
  "ExecutionEngines:Judge0:RunWorker"
  "ExecutionEngines:Judge0:BaseUrl"
  "ExecutionEngines:Judge0:ApiKey"
  "ExecutionEngines:Judge0:Host"
  "ExecutionEngines:Judge0:ShouldWait"
  "ExecutionEngines:Judge0:IsEncoded"
  "ExecutionEngines:Judge0:DefaultTimeoutInSeconds"
  "ExecutionEngines:Judge0:UseCallback"
  "ExecutionEngines:Judge0:CallbackSecret"
  "ExecutionEngines:Judge0:PublicBaseUrl"
)

PROMPTS=(
  "Auth0 Audience (your API's identifier in Auth0)"
  "Auth0 Domain (e.g. your-tenant.us.auth0.com)"
  "Connection string"
  "Azure Storage ConnectionString (Azurite emulator or a real storage account)"
  "Azure Storage avatar container name"
  "Judge0 Enabled (true/false)"
  "Judge0 RunWorker (true/false)"
  "Judge0 BaseUrl"
  "Judge0 ApiKey (from your RapidAPI Judge0 CE subscription)"
  "Judge0 Host"
  "Judge0 ShouldWait (true/false)"
  "Judge0 IsEncoded (true/false)"
  "Judge0 timeout in seconds"
  "Judge0 UseCallback (true/false, off unless you've confirmed RapidAPI delivers callbacks)"
  "Judge0 CallbackSecret (shared secret embedded in the callback_url query string)"
  "Judge0 PublicBaseUrl (publicly reachable base URL Judge0 can call back to)"
)

DEFAULTS=(
  ""
  ""
  "Host=localhost;Port=5432;Database=stackduel;Username=myuser;Password=mypassword"
  "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;"
  "avatars"
  "true"
  "true"
  "https://judge0-ce.p.rapidapi.com"
  ""
  "judge0-ce.p.rapidapi.com"
  "false"
  "true"
  "10"
  "false"
  ""
  ""
)

SECRETS=(
  "false"
  "false"
  "false"
  "true"
  "false"
  "false"
  "false"
  "false"
  "true"
  "false"
  "false"
  "false"
  "false"
  "false"
  "true"
  "false"
)

for i in "${!KEYS[@]}"; do
  value="$(read_value "${PROMPTS[$i]}" "${DEFAULTS[$i]}" "${SECRETS[$i]}")"
  set_secret "${KEYS[$i]}" "$value"
done

transport="$(read_value "MessageBus Transport (RabbitMQ/AzureServiceBus)" "RabbitMQ")"
set_secret "MessageBus:Transport" "$transport"

if [ "$transport" = "AzureServiceBus" ]; then
  value="$(read_value "Azure Service Bus ConnectionString" "" "true")"
  set_secret "MessageBus:AzureServiceBus:ConnectionString" "$value"
else
  value="$(read_value "RabbitMQ Host" "localhost")"
  set_secret "MessageBus:RabbitMQ:Host" "$value"

  value="$(read_value "RabbitMQ VirtualHost" "/")"
  set_secret "MessageBus:RabbitMQ:VirtualHost" "$value"

  value="$(read_value "RabbitMQ Username" "guest")"
  set_secret "MessageBus:RabbitMQ:Username" "$value"

  value="$(read_value "RabbitMQ Password" "guest" "true")"
  set_secret "MessageBus:RabbitMQ:Password" "$value"
fi

echo
echo "Done. User secrets have been applied to StackDuel.Api."
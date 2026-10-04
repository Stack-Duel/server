#!/usr/bin/env bash
# Deploys PUBLISH_DIR to the Azure App Service named by AZURE_WEBAPP_NAME.
# Mirrors what the `azure/webapps-deploy` action does for the Scrum environment:
# a Kudu zip deploy authenticated with the Web Deploy publish profile credentials.
set -euo pipefail

: "${AZURE_WEBAPP_NAME:?Missing AZURE_WEBAPP_NAME}"
: "${AZURE_WEBAPP_PUBLISH_PROFILE:?Missing AZURE_WEBAPP_PUBLISH_PROFILE}"
: "${PUBLISH_DIR:?Missing PUBLISH_DIR}"

msdeploy_profile=$(grep -o '<publishProfile [^>]*publishMethod="MSDeploy"[^>]*/>' <<<"${AZURE_WEBAPP_PUBLISH_PROFILE}")

if [ -z "${msdeploy_profile}" ]; then
  echo "Could not find an MSDeploy publish profile in AZURE_WEBAPP_PUBLISH_PROFILE"
  exit 1
fi

publish_url=$(grep -oP 'publishUrl="\K[^"]+' <<<"${msdeploy_profile}")
user_name=$(grep -oP 'userName="\K[^"]+' <<<"${msdeploy_profile}")
user_pwd=$(grep -oP 'userPWD="\K[^"]+' <<<"${msdeploy_profile}")
scm_host="${publish_url%%:*}"

work_dir=$(mktemp -d)
zip_path="${work_dir}/publish.zip"

(cd "${PUBLISH_DIR}" && zip -rq "${zip_path}" .)

echo "Deploying ${AZURE_WEBAPP_NAME} from ${PUBLISH_DIR} via ${scm_host}..."

curl -fsS -X POST \
  -u "${user_name}:${user_pwd}" \
  -H "Content-Type: application/zip" \
  --data-binary "@${zip_path}" \
  "https://${scm_host}/api/zipdeploy"

rm -rf "${work_dir}"

echo "Deployment to ${AZURE_WEBAPP_NAME} completed."

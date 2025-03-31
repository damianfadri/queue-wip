#!/bin/bash

# trap "kill 0" EXIT

# Setup the proxy and wait for it to connect
# kubectl proxy &
# timeout 10s bash -c "until curl http://127.0.0.1:8001/; do sleep 1; done"

# Grab the Kubernetes openapi schema from the API
$SCHEMA=$(kubectl get --raw /openapi/v2)

# Get the schema
$SPEC=$(echo "${SCHEMA}" | jq '.definitions."io.k8s.apimachinery.pkg.apis.meta.v1.ObjectMeta"')

# Recursively iterate over the schema and replace all $ref entries
# with their real schema
$DEPTH=0
do {
  $REFS=$(echo "${SPEC}" | jq --raw-output '.. | objects | ."$ref" //empty')

  echo $REFS
  foreach ($REF in $REFS) {

    $REF_KEY=$($REF -replace '#/definitions/', '')

    $REF_OBJECT=$(echo "${SCHEMA}" | jq ".definitions.""${REF_KEY}""")

    $SPEC=$(echo "${SPEC}" | jq "walk(if type == ""object"" and .""$ref"" == ""${REF}"" then . = ${REF_OBJECT} else . end)")

    echo $SPEC
  }

  $DEPTH++
} until ($DEPTH -gt 1 -and -not $REFS)

# Delete unwanted / unsupported keys and convert to YAML
echo "${SPEC}" \
| jq 'del(.. | ."x-kubernetes-patch-merge-key"?, ."x-kubernetes-patch-strategy"?, ."x-kubernetes-group-version-kind"?)' \
| yq r --prettyPrint - > "spec.yaml"
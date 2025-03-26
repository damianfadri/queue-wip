# Grab the Kubernetes OpenAPI schema from the API
$Schema = kubectl get --raw /openapi/v2

# Extract the StatefulSetSpec
$Spec = $Schema.definitions."io.k8s.api.apps.v1.StatefulSetSpec"

# Recursively iterate over the schema and replace all $ref entries
$Depth = 0
do {
    # Find all $ref entries in the schema
    $Refs = $Spec | Select-String -Pattern '"\$ref": "(.*?)"' | ForEach-Object { $_.Matches.Groups[1].Value }

    foreach ($Ref in $Refs) {
        Write-Host "${Depth}: ${Ref}"

        # Extract the ref key and fetch the referenced object
        $RefKey = $Ref -replace '#/definitions/', ''
        $RefObject = $Schema.definitions.$RefKey

        # Replace the $ref with the actual schema object
        $Spec = $Spec | ConvertTo-Json -Depth 10 | ForEach-Object {
            $_ -replace """$ref"": ""${Ref}""", $RefObject | ConvertFrom-Json
        }
    }

    $Depth++
} until ($Depth -gt 1 -and -not $Refs)

# Clean up unwanted keys and convert to YAML
$Spec | ConvertTo-Json -Depth 10 | ForEach-Object {
    $_ -replace '"x-kubernetes-patch-merge-key"?:.*?,', '' -replace '"x-kubernetes-patch-strategy"?:.*?,', '' -replace '"x-kubernetes-group-version-kind"?:.*?,', ''
} | ConvertFrom-Json | yq r --prettyPrint | Set-Content -Path "spec.yaml"
[CmdletBinding()]
param([string]$RepositoryRoot)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
}

$deploymentRoot = Join-Path $RepositoryRoot 'src\Hosts\deployment'
$failures = [System.Collections.Generic.List[string]]::new()

function Require-File([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $failures.Add("Missing deployment artifact: $Path")
        return ''
    }
    return [IO.File]::ReadAllText($Path)
}

$ingress = Require-File (Join-Path $deploymentRoot 'docker-compose.ingress.yml')
$deploymentShell = Require-File (Join-Path $RepositoryRoot 'src\Hosts\scripts\Deploy-ServerBundles.sh')
$deploymentPowerShell = Require-File (Join-Path $RepositoryRoot 'src\Hosts\scripts\Deploy-ServerBundles.ps1')
$caddy = Require-File (Join-Path $deploymentRoot 'Caddyfile')
$caddyDockerfile = Require-File (Join-Path $deploymentRoot 'Dockerfile')
$imageWorkflow = Require-File (Join-Path $RepositoryRoot '.github\workflows\container-images.yml')
$sharedExamplePath = Join-Path $deploymentRoot '.env.shared.example'
$sharedExample = Require-File $sharedExamplePath
$operatorSharedPath = Join-Path $deploymentRoot '.env.shared'
$operatorShared = Require-File $operatorSharedPath
$sharedValues = @{}
foreach ($line in $sharedExample -split "`r?`n") {
    if ($line -match '^(?<name>[A-Z0-9_]+)=(?<value>.*)$') { $sharedValues[$Matches.name] = $Matches.value }
}
$operatorSharedValues = @{}
foreach ($line in $operatorShared -split "`r?`n") {
    if ($line -match '^(?<name>[A-Z0-9_]+)=(?<value>.*)$') { $operatorSharedValues[$Matches.name] = $Matches.value }
}
if (([regex]::Matches($ingress, '"(?:80:80|443:443)/tcp"')).Count -ne 2 -or $ingress -notmatch '"80:80/tcp"' -or $ingress -notmatch '"443:443/tcp"') { $failures.Add('Ingress must publish exactly TCP 80 and TCP 443.') }
if ($ingress -notmatch '(?ms)^  caddy:\r?\n.*?^      - \.\/Caddyfile:/etc/caddy/Caddyfile:ro\s*$') { $failures.Add('The running Caddy service must mount the configured Caddyfile.') }
if ($caddy -notmatch '(?ms)^\(security_headers\)\s*\{\s+tls\s*\{.*?dns cloudflare \{env\.CLOUDFLARE_API_TOKEN\}\s+resolvers 1\.1\.1\.1\s+\}') { $failures.Add('Caddy DNS challenges must use the configured public resolver.') }
if ($ingress -notmatch '(?m)^x-caddy-runtime: &caddy-runtime\s*$' -or
    ([regex]::Matches($ingress, '(?m)^    <<: \*caddy-runtime\s*$')).Count -ne 2 -or
    $ingress -notmatch '(?ms)^  caddy-config-validator:\r?\n.*?^    profiles: \[validation\]\s*$' -or
    $ingress -notmatch '(?ms)^  caddy-config-validator:\r?\n.*?^    network_mode: none\s*$' -or
    $deploymentShell -notmatch 'compose ingress run --rm --no-deps caddy-config-validator caddy validate' -or
    $deploymentPowerShell -notmatch "'caddy-config-validator', 'caddy', 'validate'") {
    $failures.Add('Caddy validation must reuse its runtime configuration without joining the fixed-address proxy networks.')
}
if ($ingress -match '443/udp|docker\.sock|agent-runtime') { $failures.Add('Ingress must not expose HTTP/3, the Docker socket, or the agent runtime.') }
if ($ingress -notmatch '(?m)^      ingress-egress: \{\}\s*$' -or $ingress -notmatch '(?m)^  test-proxy:\r?\n    name: vitarish-test-proxy\r?\n    internal: true\s*$' -or $ingress -notmatch '(?m)^  production-proxy:\r?\n    name: vitarish-production-proxy\r?\n    internal: true\s*$') { $failures.Add('Ingress must isolate both proxy networks and use its dedicated egress network.') }
if ($caddy -match '(?im)reverse_proxy[^\r\n]*agent-runtime') { $failures.Add('Caddy must not route publicly to agent-runtime.') }
$requiredIngressVariables = @(
    'TEST_PUBLIC_HOST', 'TEST_PATIENTS_HOST', 'TEST_PLATFORM_ADMIN_HOST', 'TEST_PM_HOST', 'TEST_KNOWLEDGE_QA_HOST', 'TEST_LANDING_HOST', 'TEST_WILDCARD_HOST', 'TEST_CLINIC_WILDCARD_HOST',
    'PRODUCTION_PUBLIC_HOST', 'PRODUCTION_PATIENTS_HOST', 'PRODUCTION_PLATFORM_ADMIN_HOST', 'PRODUCTION_PM_HOST', 'PRODUCTION_KNOWLEDGE_QA_HOST', 'PRODUCTION_LANDING_HOST', 'PRODUCTION_WILDCARD_HOST', 'PRODUCTION_CLINIC_WILDCARD_HOST',
    'VITARISH_ACME_EMAIL')
foreach ($variable in $requiredIngressVariables) {
    $caddyVariablePattern = '\{\$' + [regex]::Escape($variable) + '\}'
    if ($ingress -notmatch "(?m)\b$([regex]::Escape($variable))\b" -or ($variable -ne 'VITARISH_ACME_EMAIL' -and $caddy -notmatch $caddyVariablePattern)) {
        $failures.Add("Ingress is missing required hostname variable $variable.")
    }
}
if ($ingress -notmatch '(?m)\bCLOUDFLARE_API_TOKEN:\s*\$\{VITARISH_CLOUDFLARE_API_TOKEN:\?') { $failures.Add('Ingress is missing the required Cloudflare DNS API token binding.') }
if ($caddy -notmatch '(?m)^\s*dns cloudflare \{env\.CLOUDFLARE_API_TOKEN\}\s*$' -or $caddyDockerfile -notmatch '(?m)^RUN xcaddy build v2\.10\.0 --with github\.com/caddy-dns/cloudflare\s*$') { $failures.Add('Wildcard ingress is missing the pinned Cloudflare-enabled Caddy build.') }
if ($imageWorkflow -notmatch 'VITARISH_CADDY_IMAGE=%s@%s') { $failures.Add('The release image manifest must publish the shared Caddy image variable consumed by ingress.') }
if ($imageWorkflow -notmatch 'aistor_reference="quay\.io/minio/aistor/minio:latest"' -or $imageWorkflow -notmatch 'VITARISH_AISTOR_IMAGE=quay\.io/minio/aistor/minio@%s') { $failures.Add('The release image manifest must resolve and publish the immutable MinIO AIStor image digest.') }
if ($imageWorkflow -notmatch 'NEXT_PUBLIC_API_BASE_URL=') { $failures.Add('Web images must use portable same-origin API paths rather than a build-time localhost URL.') }
if ($sharedExample -notmatch '(?m)^VITARISH_CLOUDFLARE_API_TOKEN=' -or $sharedExample -notmatch '(?m)^VITARISH_ACME_EMAIL=') { $failures.Add('The shared server environment must declare the ingress credentials.') }
if ($sharedExample -notmatch '(?im)^VITARISH_.*_IMAGE=' -or $sharedExample -match '(?im)^VITARISH_.*_IMAGE=.*:latest\s*$') { $failures.Add('The shared server environment must declare version-pinned common images.') }
$postgresImage = $sharedValues.Values | Where-Object { $_ -match '^postgres:' } | Select-Object -First 1
$postgresImageMatch = [regex]::Match([string]$postgresImage, '^postgres:(?<major>\d+)(?:\.\d+)?$')
if (-not $postgresImageMatch.Success -or [int]$postgresImageMatch.Groups['major'].Value -lt 18) { $failures.Add('The shared PostgreSQL image must be version 18 or later for the persistent data-directory layout.') }
if ($sharedExample -notmatch '(?m)^VITARISH_AISTOR_IMAGE=quay\.io/minio/aistor/minio@sha256:0{64}$') { $failures.Add('The shared example must declare the digest-pinned MinIO AIStor image.') }
if (-not $sharedValues.ContainsKey('VITARISH_DEFAULT_RESPONSE_MODEL') -or [string]::IsNullOrWhiteSpace($sharedValues['VITARISH_DEFAULT_RESPONSE_MODEL'])) { $failures.Add('The shared environment must declare the default intelligence response model.') }
foreach ($placeholder in [regex]::Matches($ingress, '\$\{(?<name>VITARISH_[A-Z0-9_]+):\?')) {
    $name = $placeholder.Groups['name'].Value
    if (-not $sharedValues.ContainsKey($name)) { $failures.Add("$sharedExamplePath is missing required variable $name.") }
}

$routeTargets = @{
    'PUBLIC_HOST' = @('main:8080', 'private-practice-owner:3000')
    'PATIENTS_HOST' = @('patients:3001')
    'PLATFORM_ADMIN_HOST' = @('platform-admin:3003')
    'PM_HOST' = @('pm:3004')
    'KNOWLEDGE_QA_HOST' = @('knowledge-qa:3001')
    'LANDING_HOST' = @('landing:3002')
}
$requiredPublishedImages = @('caddy', 'main', 'agent-runtime', 'private-practice-owner', 'patients', 'platform-admin', 'pm', 'knowledge-qa', 'landing')
foreach ($imageName in $requiredPublishedImages) {
    if ($imageWorkflow -notmatch "(?m)^\s*- name: $([regex]::Escape($imageName))\s*$") { $failures.Add("CI image workflow is missing the $imageName image entry.") }
}

foreach ($environment in @('test', 'production')) {
    $prefix = $environment.ToUpperInvariant()
    $proxySubnetVariable = "VITARISH_${prefix}_PROXY_SUBNET"
    $ingressAddressVariable = "VITARISH_${prefix}_INGRESS_IPV4"
    $composePath = Join-Path $deploymentRoot "docker-compose.$environment.yml"
    $compose = Require-File $composePath
    if ($compose -match 'VITARISH_API_ORIGIN') { $failures.Add("$environment bundle sets an unused Web API origin; route same-origin API paths through ingress.") }
    if ($compose -notmatch "(?m)^name: vitarish-$environment\s*$") { $failures.Add("$environment bundle has the wrong Compose project name.") }
    if ($compose -notmatch '(?m)^      ASPNETCORE_ENVIRONMENT: Production\s*$' -or $compose -notmatch '(?m)^      Vitarish__Security__Tls__RequireHttps: "true"\s*$') { $failures.Add("$environment Main must explicitly use strict Production HTTPS configuration.") }
    if ($compose -notmatch '(?m)^      Vitarish__Intelligence__DefaultResponseModel: \$\{VITARISH_DEFAULT_RESPONSE_MODEL:\?VITARISH_DEFAULT_RESPONSE_MODEL is required\}\s*$') { $failures.Add("$environment Main must receive its required default intelligence response model from the shared environment.") }
    if ($compose -match '(?im)^\s*image:\s*[^\r\n]*:latest\s*$') { $failures.Add("$environment bundle contains a mutable latest image.") }
    foreach ($imageLine in [regex]::Matches($compose, '(?im)^\s*image:\s*[^\r\n]+$')) {
        if ($imageLine.Value -notmatch '^\s*image:\s*"?\$\{[A-Z0-9_]+:\?[^}]+\}"?\s*$') { $failures.Add("$environment bundle contains an image that is not required through an environment variable.") }
    }
    if ($compose -match '(?im)^\s*ports:\s*') { $failures.Add("$environment bundle publishes an application or state-service port.") }
    if ($compose -notmatch "(?m)^    volumes: \[$environment-postgres:/var/lib/postgresql\]\s*$") { $failures.Add("$environment PostgreSQL must persist its versioned data directory through the PostgreSQL parent volume.") }
    foreach ($service in @('main', 'private-practice-owner', 'patients', 'platform-admin', 'pm', 'knowledge-qa', 'landing', 'agent-runtime', 'postgres', 'redis', 'aistor', 'elasticsearch')) {
        if ($compose -notmatch "(?m)^\s{2}$([regex]::Escape($service)):\s*$") { $failures.Add("$environment bundle is missing service $service.") }
    }
    foreach ($stateService in @('postgres', 'redis', 'aistor', 'elasticsearch')) {
        if ($compose -notmatch "(?ms)^  ${stateService}:\s*.*?^\s{4}healthcheck:\s*") { $failures.Add("$environment service $stateService is missing a readiness healthcheck.") }
    }
    foreach ($applicationService in @('main', 'private-practice-owner', 'patients', 'platform-admin', 'pm', 'knowledge-qa', 'landing', 'agent-runtime')) {
        if ($compose -notmatch "(?ms)^  ${applicationService}:\s*.*?^\s{4}healthcheck:\s*") { $failures.Add("$environment application service $applicationService is missing a readiness healthcheck.") }
    }
    $agentRuntimeSection = [regex]::Match($compose, '(?ms)^  agent-runtime:\r?\n(?<body>.*?)(?=^  (?:postgres|redis|aistor|elasticsearch):\r?\n)').Groups['body'].Value
    $openAiKeyPattern = '\$\{VITARISH_' + [regex]::Escape($prefix) + '_OPENAI_API_KEY:\?'
    if ($agentRuntimeSection -notmatch $openAiKeyPattern) { $failures.Add("$environment agent-runtime must receive its required OpenAI API key from the operator environment.") }
    if ($agentRuntimeSection -notmatch '(?m)^    networks:\s*\[application, egress\]\s*$') { $failures.Add("$environment agent-runtime must use its private application network and provider egress network.") }
    if ($agentRuntimeSection -notmatch '(?s)/api/internal/intelligence-agent/health.*AGENT_RUNTIME_SHARED_SECRET') { $failures.Add("$environment agent-runtime must use its authenticated health endpoint.") }
    foreach ($dependency in @('postgres', 'redis', 'aistor', 'elasticsearch')) {
        if ($compose -notmatch "(?ms)^\s{6}$([regex]::Escape($dependency)):\s*\{\s*condition:\s*service_healthy\s*\}") { $failures.Add("$environment Main is missing a healthy dependency gate for $dependency.") }
    }
    if ($compose -notmatch "(?m)^  application:\s*\{\s*name: vitarish-$environment-application, internal: true\s*\}") { $failures.Add("$environment application network must be private and environment-owned.") }
    if ($compose -notmatch "(?m)^  proxy:\s*\{\s*name: vitarish-$environment-proxy, external: true\s*\}") { $failures.Add("$environment proxy network must be environment-owned and shared with ingress.") }
    $trustedProxyPattern = '(?m)^\s+Vitarish__Security__TrustedProxyNetworks__0:\s*\$\{' + [regex]::Escape($ingressAddressVariable) + ':\?[^}]+\}/32\s*$'
    if ($compose -notmatch $trustedProxyPattern) { $failures.Add("$environment Main must trust only its Caddy ingress IPv4 address.") }
    foreach ($volume in @('postgres', 'redis', 'aistor', 'elasticsearch')) {
        if ($compose -notmatch "(?m)^  ${environment}-${volume}:\s*\{\s*name: vitarish-${environment}-${volume}\s*\}") { $failures.Add("$environment volume $volume is not uniquely owned.") }
    }
    if ($compose -notmatch '(?m)^    command: server /mnt/data --license /minio\.license\s*$' -or
        $compose -notmatch '(?m)^        source: \./minio\.license\s*$' -or
        $compose -notmatch '(?m)^        target: /minio\.license\s*$' -or
        $compose -notmatch '(?m)^        read_only: true\s*$' -or
        $compose -notmatch '(?m)^    healthcheck: \{ test: \[CMD, mc, ready, local\]') {
        $failures.Add("$environment MinIO AIStor must mount its license read-only and provide a readiness healthcheck.")
    }
    $examplePath = Join-Path $deploymentRoot ".env.$environment.example"
    $example = Require-File $examplePath
    if ($example -notmatch '(?im)^VITARISH_.*_IMAGE=') { $failures.Add("$examplePath must declare image references.") }
    if ($example -match '(?im)^VITARISH_.*_IMAGE=.*:latest\s*$') { $failures.Add("$examplePath contains a mutable latest image reference.") }
    $exampleValues = @{}
    foreach ($line in $example -split "`r?`n") {
        if ($line -match '^(?<name>[A-Z0-9_]+)=(?<value>.*)$') { $exampleValues[$Matches.name] = $Matches.value }
    }
    foreach ($placeholder in [regex]::Matches($compose, '\$\{(?<name>VITARISH_[A-Z0-9_]+):\?')) {
        $name = $placeholder.Groups['name'].Value
        if (-not $sharedValues.ContainsKey($name) -and -not $exampleValues.ContainsKey($name)) { $failures.Add("$examplePath or $sharedExamplePath is missing required variable $name.") }
        if ($sharedValues.ContainsKey($name) -and $exampleValues.ContainsKey($name)) { $failures.Add("$name must have one owner and cannot be repeated in $examplePath.") }
    }
    $proxySubnetBinding = '\$\{' + [regex]::Escape($proxySubnetVariable) + ':\?'
    $proxyNetworkPattern = '(?m)^  {0}-proxy:\r?\n    name: vitarish-{0}-proxy\r?\n    internal: true\r?\n    ipam:\r?\n      config:\r?\n        - subnet: {1}' -f [regex]::Escape($environment), $proxySubnetBinding
    if ($ingress -notmatch $proxyNetworkPattern) { $failures.Add("Ingress must allocate the $environment proxy network from its operator-supplied subnet.") }
    $ingressAddressBinding = '\$\{' + [regex]::Escape($ingressAddressVariable) + ':\?[^}]+\}'
    if ($ingress -notmatch "(?m)^\s+ipv4_address:\s*$ingressAddressBinding\s*$") { $failures.Add("Ingress must use the operator-supplied $environment IPv4 address.") }

    if (-not $operatorSharedValues.ContainsKey($proxySubnetVariable) -or -not $operatorSharedValues.ContainsKey($ingressAddressVariable)) {
        $failures.Add("$operatorSharedPath must set both proxy subnet and ingress IPv4 inputs.")
    }
    else {
        try {
            $operatorNetwork = [System.Net.IPNetwork]::Parse($operatorSharedValues[$proxySubnetVariable])
            $operatorIngressAddress = [System.Net.IPAddress]::Parse($operatorSharedValues[$ingressAddressVariable])
            if ($operatorNetwork.BaseAddress.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork -or
                $operatorIngressAddress.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork -or
                -not $operatorNetwork.Contains($operatorIngressAddress)) {
                $failures.Add("$operatorSharedPath ingress IPv4 address must belong to its proxy subnet.")
            }
        }
        catch {
            $failures.Add("$operatorSharedPath must contain a valid proxy subnet and IPv4 address.")
        }
    }

    foreach ($route in $routeTargets.Keys) {
        $hostVariable = "${prefix}_${route}"
        $caddyHostPattern = '\{\$' + [regex]::Escape($hostVariable) + '\}'
        $composeHostVariable = "VITARISH_$hostVariable"
        if ($compose -notmatch "\b$([regex]::Escape($composeHostVariable))\b" -or $caddy -notmatch $caddyHostPattern) { $failures.Add("$environment route host $hostVariable is not wired in both Compose and Caddy.") }
        foreach ($target in $routeTargets[$route]) {
            $alias = "$environment-$target"
            if ($caddy -notmatch "reverse_proxy[^\r\n]*$([regex]::Escape($alias))") { $failures.Add("$environment route $hostVariable is missing Caddy target $alias.") }
        }
    }
    $apiImportName = "${environment}_api"
    $apiImportCount = [regex]::Matches($caddy, "(?m)^\s+import $([regex]::Escape($apiImportName))\s*$").Count
    $mainAlias = "$environment-main:8080"
    $apiSnippetPattern = "(?ms)^\($([regex]::Escape($apiImportName))\)\s*\{\s*@api path /api/\*\s*reverse_proxy @api $([regex]::Escape($mainAlias))\s*\}"
    if ($apiImportCount -ne 8 -or $caddy -notmatch $apiSnippetPattern) { $failures.Add("$environment Caddy routes must send same-origin API traffic from every application hostname to Main.") }
    foreach ($wildcardHostVariable in @("${prefix}_WILDCARD_HOST", "${prefix}_CLINIC_WILDCARD_HOST")) {
        $hostBinding = [regex]::Escape('{$' + $wildcardHostVariable + '}')
        $redirectPattern = '(?ms)^http://' + $hostBinding + '\s*\{\s*redir https://\{host\}\{uri\} 308\s*\}'
        $upstreamPattern = '(?ms)^https://' + $hostBinding + '\s*\{\s*import security_headers\s+import ' + [regex]::Escape($apiImportName) + '\s+reverse_proxy ' + [regex]::Escape("$environment-private-practice-owner:3000") + '\s*\}'
        if ($caddy -notmatch $redirectPattern -or $caddy -notmatch $upstreamPattern) {
            $failures.Add("$environment wildcard route $wildcardHostVariable must redirect HTTP and proxy HTTPS through the owner Web and Main API.")
        }
    }
}

if ($failures.Count) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}
Write-Output 'Deployment bundle integrity passed.'

[CmdletBinding()]
param(
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
}
$profileRoot = Join-Path $RepositoryRoot 'docs\security\network-questionnaire-v1.0'
$planPath = Join-Path $profileRoot 'implementation-plan.md'
$matrixPath = Join-Path $profileRoot 'requirements-matrix.md'
$configPath = Join-Path $profileRoot 'evaluated-configuration.md'
$transportInventoryPath = Join-Path $profileRoot 'evaluated-transport-and-cryptography.md'
$updateRunbookPath = Join-Path $profileRoot 'manual-update-runbook.md'
$composePath = Join-Path $RepositoryRoot 'src\Hosts\docker-compose.product-evaluated-v1.0.yml'
$caddyPath = Join-Path $RepositoryRoot 'src\Hosts\caddy\Caddyfile.product-evaluated-v1.0'
$healthContractPath = Join-Path $RepositoryRoot 'src\Hosts\Main\Infrastructure\Health\MainDependencyHealthCheck.cs'
$dockerIgnorePath = Join-Path $RepositoryRoot '.dockerignore'

$failures = [System.Collections.Generic.List[string]]::new()
function Require-Text([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        $failures.Add("Missing $Description`: $Path")
        return ''
    }
    return [IO.File]::ReadAllText($Path)
}

$plan = Require-Text $planPath 'implementation plan'
$matrix = Require-Text $matrixPath 'requirements matrix'
$implementationMatrix = Require-Text (Join-Path $profileRoot 'implementation-matrix.md') 'implementation matrix'
$deploymentMatrix = Require-Text (Join-Path $profileRoot 'deployment-evidence-matrix.md') 'deployment-evidence matrix'
$config = Require-Text $configPath 'evaluated configuration'
$transportInventory = Require-Text $transportInventoryPath 'transport and cryptography inventory'
$updateRunbook = Require-Text $updateRunbookPath 'manual update runbook'
$compose = Require-Text $composePath 'network compose manifest'
$caddy = Require-Text $caddyPath 'network Caddyfile'
$healthContract = Require-Text $healthContractPath 'Main readiness contract'
$dockerIgnore = Require-Text $dockerIgnorePath 'Docker build ignore rules'

$planIds = [regex]::Matches($plan, '(?m)^\|\s*([A-Z]+-\d+)(?:\s*/\s*([A-Z]+-\d+))?\s*\|') |
    ForEach-Object { $_.Groups[1].Value; if ($_.Groups[2].Success) { $_.Groups[2].Value } } |
    Sort-Object -Unique
$matrixIds = [regex]::Matches($matrix, '(?m)^\|\s*([A-Z]+-\d+)(?:\s*/\s*([A-Z]+-\d+))?\s*\|') |
    ForEach-Object { $_.Groups[1].Value; if ($_.Groups[2].Success) { $_.Groups[2].Value } } |
    Sort-Object -Unique

if ($planIds.Count -eq 0) { $failures.Add('No stable requirement IDs found in the implementation plan.') }
foreach ($id in $planIds) {
    if ($matrixIds -notcontains $id) { $failures.Add("Requirement $id is missing from requirements-matrix.md.") }
    if ($implementationMatrix -notmatch [regex]::Escape($id)) { $failures.Add("Requirement $id is missing from implementation-matrix.md.") }
    if ($deploymentMatrix -notmatch [regex]::Escape($id)) { $failures.Add("Requirement $id is missing from deployment-evidence-matrix.md.") }
}

foreach ($required in @(
    'implementation-matrix.md', 'deployment-evidence-matrix.md', 'security-target.md',
    'toe-inventory.md', 'topology.md', 'deployment-evidence-plan.md',
    'evaluated-transport-and-cryptography.md', 'manual-update-runbook.md',
    'failure-recovery-matrix.md'
)) {
    if (-not (Test-Path -LiteralPath (Join-Path $profileRoot $required) -PathType Leaf)) {
        $failures.Add("Missing Stage 0 artifact: $required")
    }
}
$membershipScript = Join-Path $RepositoryRoot 'src\Hosts\scripts\Get-EvaluatedMembershipAnalysis.ps1'
if (-not (Test-Path -LiteralPath $membershipScript -PathType Leaf)) {
    $failures.Add("Missing read-only membership analysis helper: $membershipScript")
}

if ($compose -match '(?im)image:\s*[^\r\n]*:latest|443/udp|ports:\s*\[?[^\r\n]*5432|ports:\s*\[?[^\r\n]*6379') {
    $failures.Add('Network profile contains mutable latest images, UDP 443, or published state-service ports.')
}
foreach ($requiredText in @(
    'vitarish-product-evaluated-v1-0', 'VITARISH_MAIN_IMAGE',
    'VITARISH_OWNER_WEB_IMAGE', 'VITARISH_PATIENTS_WEB_IMAGE', 'VITARISH_PLATFORM_ADMIN_WEB_IMAGE',
    'VITARISH_PM_WEB_IMAGE', 'VITARISH_KNOWLEDGE_QA_WEB_IMAGE', 'VITARISH_LANDING_WEB_IMAGE',
    'VITARISH_AGENT_RUNTIME_IMAGE', 'VITARISH_CADDY_IMAGE', 'internal: true', 'egress:',
    'VITARISH_IDENTITY_AUTHORITY', 'VITARISH_DATA_PROTECTION_PASSWORD', 'VITARISH_AUTH_ATTEMPT_KEY_PEPPER',
    'VITARISH_SMS_API_KEY', 'VITARISH_SMTP_PASSWORD', 'VITARISH_OPENAI_API_KEY',
    'VITARISH_AGENT_RUNTIME_SHARED_SECRET',
    'http://agent-runtime:3003',
    'http://main:8080/api/modules/intelligence/internal/agent/tools/execute'
)) {
    if ($compose -notmatch [regex]::Escape($requiredText)) { $failures.Add("Compose manifest lacks required boundary text: $requiredText") }
}
foreach ($requiredText in @(
    'protocols tls1.2 tls1.2', 'X-Content-Type-Options', 'Content-Security-Policy',
    'ciphers TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256', 'curves secp256r1', 'alpn h1 h2',
    'health_interval 10s', 'health_timeout 5s',
    'reverse_proxy web-private-practice-owner:3000',
    'http://{$VITARISH_PUBLIC_HOST:?VITARISH_PUBLIC_HOST is required}',
    'http://{$VITARISH_PATIENTS_HOST:?VITARISH_PATIENTS_HOST is required}',
    'http://{$VITARISH_PLATFORM_ADMIN_HOST:?VITARISH_PLATFORM_ADMIN_HOST is required}',
    'http://{$VITARISH_PM_HOST:?VITARISH_PM_HOST is required}',
    'http://{$VITARISH_KNOWLEDGE_QA_HOST:?VITARISH_KNOWLEDGE_QA_HOST is required}',
    'http://{$VITARISH_LANDING_HOST:?VITARISH_LANDING_HOST is required}'
)) {
    if ($caddy -notmatch [regex]::Escape($requiredText)) { $failures.Add("Caddyfile lacks required profile control: $requiredText") }
}
if ($caddy -match '(?im)h3|http/3|443/udp') { $failures.Add('Caddyfile enables or references HTTP/3/QUIC in the TLS 1.2-only profile.') }
if ($caddy -match '(?im)agent-runtime') { $failures.Add('Caddyfile must not expose the server-only agent runtime publicly.') }
if ($dockerIgnore -notmatch '(?m)^\*\*/appsettings\.json\s*$') { $failures.Add('Docker build context must exclude local appsettings.json files.') }
$readinessPathMatch = [regex]::Match($healthContract, '(?m)^\s*public const string ReadinessPath\s*=\s*"(?<path>/[^"]+)";')
if (-not $readinessPathMatch.Success) {
    $failures.Add('Main readiness contract does not declare a source-owned absolute readiness path.')
} else {
    $readinessPath = $readinessPathMatch.Groups['path'].Value
    if ($caddy -notmatch "(?m)^\s*health_uri\s+$([regex]::Escape($readinessPath))\s*$") {
        $failures.Add("Caddyfile does not probe the source-owned Main readiness path: $readinessPath")
    }
}
$restartPolicyMatch = [regex]::Match($compose, '(?m)^x-bounded-runtime-restart:\s*&bounded-runtime-restart\s*"(?<policy>[^"]+)"\s*$')
if (-not $restartPolicyMatch.Success) {
    $failures.Add('Compose manifest does not declare the source-owned bounded runtime restart policy anchor.')
} else {
    $restartPolicy = $restartPolicyMatch.Groups['policy'].Value
    if ($restartPolicy -ne 'on-failure:5') {
        $failures.Add("Compose bounded runtime restart policy is not the selected value: $restartPolicy")
    }
}

$servicesMatch = [regex]::Match($compose, '(?ms)^services:\s*\r?\n(?<services>.*?)(?=^networks:\s*$)')
$services = if ($servicesMatch.Success) { $servicesMatch.Groups['services'].Value } else { '' }
$runtimeServices = @(
    'caddy', 'main', 'web-private-practice-owner', 'web-patients', 'web-platform-admin',
    'web-vitarish-pm', 'web-knowledge-qa-demo', 'web-vitarish-landing', 'agent-runtime',
    'postgres', 'redis'
)
$mainService = [regex]::Match($services, '(?ms)^  main:\r?\n(?<body>.*?)(?=^  [a-z0-9-]+:\r?$|\z)')
if (-not $mainService.Success -or $mainService.Groups['body'].Value -notmatch '(?m)^    networks:\s*\[application, egress\]\s*$') { $failures.Add('Main must use the application and dedicated egress networks.') }
foreach ($service in [regex]::Matches($services, '(?ms)^  (?<name>[a-z0-9-]+):\r?\n(?<body>.*?)(?=^  [a-z0-9-]+:\r?$|\z)')) {
    if ($runtimeServices -contains $service.Groups['name'].Value -and $service.Groups['body'].Value -notmatch '(?m)^    restart:\s*\*bounded-runtime-restart\s*$') {
        $failures.Add("Runtime service lacks bounded restart supervision: $($service.Groups['name'].Value)")
    }
    if ($service.Groups['name'].Value -ne 'main' -and $service.Groups['body'].Value -match '(?m)^    networks:\s*\[[^\]]*\begress\b[^\]]*\]\s*$') {
        $failures.Add("Only Main may use the dedicated egress network: $($service.Groups['name'].Value)")
    }
}
if ($compose -match '(?ms)^  egress:\r?\n(?<body>.*?)(?=^  [a-z0-9-]+:\s*$|\z)' -and $Matches.body -match '(?m)^    internal:\s*true\s*$') { $failures.Add('The dedicated egress network must not be internal.') }

foreach ($requiredText in @(
    'TLS 1.2 only', 'same-machine', 'separate-machine',
    'TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256', 'P-256', 'basicConstraints CA=TRUE',
    'Server Authentication', 'native trusted-chain', 'CR-1', 'CR-2', 'CR-3',
    'TSF-4', 'TSF-5', 'CR-4/TSF-6 not triggered'
)) {
    if ($transportInventory -notmatch [regex]::Escape($requiredText)) {
        $failures.Add("Transport/cryptography inventory lacks required declaration: $requiredText")
    }
}
foreach ($requiredText in @(
    'authorized administrator or developer', 'versioned container image reference',
    'forward-only', 'previous known-good', 'does not check for updates',
    'update file''s signature'
)) {
    if ($updateRunbook -notmatch [regex]::Escape($requiredText)) {
        $failures.Add("Manual update runbook lacks required boundary: $requiredText")
    }
}

$sourcePaths = & rg --files (Join-Path $RepositoryRoot 'src') -g '*.cs' -g '!**/bin/**' -g '!**/obj/**' -g '!**/Migrations/**' -g '!**/node_modules/**'
if ($LASTEXITCODE -notin 0, 1) {
    $failures.Add('Source-file discovery failed while checking transport enforcement.')
}
$source = ($sourcePaths | ForEach-Object { [IO.File]::ReadAllText($_) }) -join "`n"
if ($source -match 'DangerousAcceptAnyServerCertificateValidator|ServerCertificateCustomValidationCallback\s*=') {
    $failures.Add('Source contains a certificate-validation bypass or custom callback assignment.')
}
foreach ($requiredText in @(
    'SslProtocols.Tls12', 'CheckCertificateRevocationList = true',
    'AddSecureHttpClientDefaults', 'client.SslProtocols = SslProtocols.Tls12',
    'client.CheckCertificateRevocation = true'
)) {
    if ($source -notmatch [regex]::Escape($requiredText)) {
        $failures.Add("Source lacks selected transport enforcement: $requiredText")
    }
}

$baseFiles = Get-ChildItem (Join-Path $RepositoryRoot 'src\Modules\Base') -Recurse -File -Include *.cs -ErrorAction SilentlyContinue
foreach ($file in $baseFiles) {
    if ([IO.File]::ReadAllText($file.FullName) -match 'Vitarish\.Authentication\.(Contracts|Application)|Authentication.*Csrf|Csrf.*Authentication') {
        $failures.Add("Base must not reference Authentication CSRF/contracts: $($file.FullName)")
    }
}

foreach ($requiredDecision in @(
    'TLS 1.2 only', 'Retain normalized source IP', 'Restricted mode',
    'no HA claim', 'Phone/SMS OTP'
)) {
    if ($config -notmatch [regex]::Escape($requiredDecision)) {
        $failures.Add("Evaluated configuration lacks the selected Stage 0 baseline: $requiredDecision")
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error -Message $_ -ErrorAction Continue }
    exit 1
}

Write-Output "Network questionnaire plan integrity passed: $($planIds.Count) stable IDs and frozen profile controls verified."

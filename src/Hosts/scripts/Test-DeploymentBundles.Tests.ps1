[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$gate = Join-Path $PSScriptRoot 'Test-DeploymentBundles.ps1'
$sourceDeployment = Join-Path $repositoryRoot 'src\Hosts\deployment'
$sourceImageWorkflow = Join-Path $repositoryRoot '.github\workflows\container-images.yml'
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) "vitarish-deployment-gate-$PID"
$caseRoot = Join-Path $fixtureRoot 'src\Hosts\deployment'
$passed = 0

function Reset-Fixture {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $caseRoot -Force | Out-Null
    foreach ($name in @('Caddyfile', 'Dockerfile', 'docker-compose.ingress.yml', 'docker-compose.test.yml', 'docker-compose.production.yml', '.env.shared.example', '.env.test.example', '.env.production.example')) {
        Copy-Item -LiteralPath (Join-Path $sourceDeployment $name) -Destination (Join-Path $caseRoot $name)
    }
    Copy-Item -LiteralPath (Join-Path $caseRoot '.env.shared.example') -Destination (Join-Path $caseRoot '.env.shared')
    $workflowDestination = Join-Path $fixtureRoot '.github\workflows\container-images.yml'
    New-Item -ItemType Directory -Path (Split-Path $workflowDestination) -Force | Out-Null
    Copy-Item -LiteralPath $sourceImageWorkflow -Destination $workflowDestination
    $scriptDestination = Join-Path $fixtureRoot 'src\Hosts\scripts'
    New-Item -ItemType Directory -Path $scriptDestination -Force | Out-Null
    foreach ($name in @('Deploy-ServerBundles.ps1', 'Deploy-ServerBundles.sh')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $scriptDestination $name)
    }
}

function Invoke-Gate([bool]$ExpectFailure, [string]$Name) {
    $output = & pwsh -NoProfile -File $gate -RepositoryRoot $fixtureRoot 2>&1 | Out-String
    $failed = $LASTEXITCODE -ne 0
    if ($failed -ne $ExpectFailure) {
        throw "Fixture '$Name' expected failure=$ExpectFailure but observed failure=$failed. Output: $output"
    }
    $script:passed++
}

try {
    Reset-Fixture
    Invoke-Gate $false 'valid-bundles'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.production.yml'
    (Get-Content -Raw $path) -replace '(?m)^        required: false$', '        required: true' | Set-Content -NoNewline $path
    Invoke-Gate $true 'production-mediatr-file-remains-optional'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.ingress.yml'
    $ingress = Get-Content -Raw $path
    $ingress = [regex]::Replace($ingress, '(?ms)(^  caddy:\r?\n.*?^    volumes:\r?\n)(      - \.\/Caddyfile:/etc/caddy/Caddyfile:ro\r?\n)', '$1')
    Set-Content -LiteralPath $path -Value $ingress -NoNewline
    Invoke-Gate $true 'caddy-service-mounts-configured-file'

    Reset-Fixture
    $path = Join-Path $caseRoot 'Caddyfile'
    (Get-Content -Raw $path) -replace '(?m)^        resolvers 1\.1\.1\.1\r?\n', '' | Set-Content -NoNewline $path
    Invoke-Gate $true 'caddy-dns-challenge-uses-public-resolver'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.ingress.yml'
    (Get-Content -Raw $path) -replace '    network_mode: none', '    network_mode: ingress-egress' | Set-Content -NoNewline $path
    Invoke-Gate $true 'caddy-validator-uses-isolated-network-mode'

    Reset-Fixture
    $path = Join-Path $caseRoot '.env.shared'
    (Get-Content -Raw $path) -replace '(?m)^VITARISH_TEST_INGRESS_IPV4=10\.240\.0\.2$', 'VITARISH_TEST_INGRESS_IPV4=10.241.0.2' | Set-Content -NoNewline $path
    Invoke-Gate $true 'ingress-address-outside-proxy-subnet'

    Reset-Fixture
    $path = Join-Path $caseRoot '.env.shared.example'
    (Get-Content -Raw $path) -replace '(?m)^VITARISH_TEST_INGRESS_IPV4=10\.240\.0\.2$', 'VITARISH_TEST_INGRESS_IPV4=10.241.0.2' | Set-Content -NoNewline $path
    Invoke-Gate $false 'valid-operator-values-override-invalid-example-values'

    Reset-Fixture
    Remove-Item -LiteralPath (Join-Path $caseRoot '.env.shared')
    Invoke-Gate $true 'missing-operator-shared-environment'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace '\$\{VITARISH_TEST_INGRESS_IPV4:\?VITARISH_TEST_INGRESS_IPV4 is required\}/32', '172.16.0.0/12' | Set-Content -NoNewline $path
    Invoke-Gate $true 'broad-forwarded-header-trust'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace '(?m)^      OPENAI_API_KEY:.*\r?\n', '' | Set-Content -NoNewline $path
    Invoke-Gate $true 'missing-agent-provider-credential'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace '(?m)^    networks: \[application, egress\]\s*$', '    networks: [application]' | Set-Content -NoNewline $path
    Invoke-Gate $true 'agent-runtime-without-provider-egress'

    Reset-Fixture
    $path = Join-Path $caseRoot 'Caddyfile'
    (Get-Content -Raw $path) -replace '(?m)^    import test_api\r?\n', '' | Set-Content -NoNewline $path
    Invoke-Gate $true 'application-host-without-main-api-route'

    Reset-Fixture
    $path = Join-Path $fixtureRoot '.github\workflows\container-images.yml'
    (Get-Content -Raw $path) -replace '(?m)^\s+build-args:.*NEXT_PUBLIC_API_BASE_URL=.*\r?\n', '' | Set-Content -NoNewline $path
    Invoke-Gate $true 'web-images-with-nonportable-api-origin'

    Reset-Fixture
    $path = Join-Path $fixtureRoot '.github\workflows\container-images.yml'
    (Get-Content -Raw $path) -replace 'VITARISH_CADDY_IMAGE=%s@%s', 'VITARISH_TEST_CADDY_IMAGE=%s@%s' | Set-Content -NoNewline $path
    Invoke-Gate $true 'caddy-release-variable-mismatch'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace '(?m)^    image: \$\{VITARISH_TEST_MAIN_IMAGE[^\r\n]+$', '    image: nginx:latest' | Set-Content -NoNewline $path
    Invoke-Gate $true 'mutable-image'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace 'vitarish-test-postgres', 'vitarish-production-postgres' | Set-Content -NoNewline $path
    Invoke-Gate $true 'cross-environment-volume-reuse'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace 'vitarish-test-proxy', 'vitarish-production-proxy' | Set-Content -NoNewline $path
    Invoke-Gate $true 'cross-environment-network-reuse'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    $publishedPort = '  postgres:' + [Environment]::NewLine + '    ports:' + [Environment]::NewLine + '      - "5432:5432"' + [Environment]::NewLine
    (Get-Content -Raw $path) -replace '(?m)^  postgres:\r?\n', $publishedPort | Set-Content -NoNewline $path
    Invoke-Gate $true 'published-datastore-port'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace 'postgres: \{ condition: service_healthy \}', 'postgres: { condition: service_started }' | Set-Content -NoNewline $path
    Invoke-Gate $true 'missing-health-dependency'

    Reset-Fixture
    $path = Join-Path $caseRoot 'docker-compose.test.yml'
    (Get-Content -Raw $path) -replace 'test-postgres:/var/lib/postgresql', 'test-postgres:/var/lib/postgresql/data' | Set-Content -NoNewline $path
    Invoke-Gate $true 'postgres-volume-target-matches-major-version'

    Reset-Fixture
    $path = Join-Path $caseRoot '.env.shared.example'
    (Get-Content -Raw $path) -replace '(?m)^VITARISH_POSTGRES_IMAGE=postgres:18\.0$', 'VITARISH_POSTGRES_IMAGE=postgres:17.6' | Set-Content -NoNewline $path
    Invoke-Gate $true 'postgres-image-major-matches-volume-layout'

    Reset-Fixture
    $path = Join-Path $caseRoot 'Caddyfile'
    (Get-Content -Raw $path) -replace '(?m)^.*\{\$TEST_PATIENTS_HOST\}.*\r?\n', '' | Set-Content -NoNewline $path
    Invoke-Gate $true 'missing-route-host'

    Reset-Fixture
    $path = Join-Path $caseRoot 'Caddyfile'
    (Get-Content -Raw $path) -replace '(?m)^https://\{\$PRODUCTION_CLINIC_WILDCARD_HOST\} \{', 'https://{$MISSING_CLINIC_WILDCARD_HOST} {' | Set-Content -NoNewline $path
    Invoke-Gate $true 'missing-clinic-wildcard-route'

    Reset-Fixture
    $path = Join-Path $caseRoot 'Caddyfile'
    Add-Content -LiteralPath $path -Value "`r`nhttps://`{`$TEST_PUBLIC_HOST`}`r`n{`r`n    reverse_proxy test-agent-runtime:3003`r`n}`r`n"
    Invoke-Gate $true 'public-agent-runtime-route'

    Write-Output "Deployment bundle fixtures passed ($passed cases)."
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
}
$global:LASTEXITCODE = 0

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$deploymentSource = Join-Path $repositoryRoot 'src/Hosts/deployment'
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$fixtureRoot = Join-Path $temporaryRoot "vitarish-server-deployment-$PID"
$fixtureDeployment = Join-Path $fixtureRoot 'src/Hosts/deployment'
$fixtureManifest = Join-Path $fixtureRoot 'release-images.env'

function Reset-Fixture {
    if (Test-Path -LiteralPath $fixtureRoot) { Remove-Item -LiteralPath $fixtureRoot -Recurse -Force }
    New-Item -ItemType Directory -Path $fixtureDeployment -Force | Out-Null
    foreach ($name in @('Caddyfile', 'Dockerfile', 'docker-compose.ingress.yml', 'docker-compose.test.yml', 'docker-compose.production.yml', '.env.shared.example', '.env.test.example', '.env.production.example')) {
        Copy-Item -LiteralPath (Join-Path $deploymentSource $name) -Destination (Join-Path $fixtureDeployment $name)
    }
    foreach ($name in @('shared', 'test', 'production')) {
        $content = [IO.File]::ReadAllText((Join-Path $fixtureDeployment ".env.$name.example"))
        $content = $content -replace 'registry\.example\.invalid', 'registry.vitarish.com' -replace '0{64}', ('1' * 64) -replace 'example\.invalid', 'vitarish.com' -replace 'replace-with-[^\r\n]+', 'fixture-value'
        [IO.File]::WriteAllText((Join-Path $fixtureDeployment ".env.$name"), $content)
    }
    [IO.File]::WriteAllBytes((Join-Path $fixtureDeployment 'minio.license'), [byte[]]@())
    $imageLines = foreach ($name in @('shared', 'test', 'production')) {
        Get-Content -LiteralPath (Join-Path $fixtureDeployment ".env.$name") | Where-Object { $_ -match '^VITARISH_[A-Z0-9_]+_IMAGE=' }
    }
    [IO.File]::WriteAllLines($fixtureManifest, [string[]]$imageLines)
    $workflowDirectory = Join-Path $fixtureRoot '.github/workflows'
    New-Item -ItemType Directory -Path $workflowDirectory -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $repositoryRoot '.github/workflows/container-images.yml') -Destination (Join-Path $workflowDirectory 'container-images.yml')
    $scriptsDirectory = Join-Path $fixtureRoot 'src/Hosts/scripts'
    New-Item -ItemType Directory -Path $scriptsDirectory -Force | Out-Null
    foreach ($name in @('Deploy-ServerBundles.sh', 'Deploy-ServerBundles.ps1')) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination (Join-Path $scriptsDirectory $name)
    }
}

function Invoke-Case([bool]$ExpectedFailure, [string]$Name) {
    $output = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Deploy-ServerBundles.ps1') -ValidateOnly -ReleaseManifest $fixtureManifest -RepositoryRoot $fixtureRoot 2>&1 | Out-String
    $failed = $LASTEXITCODE -ne 0
    if ($failed -ne $ExpectedFailure) { throw "Deployment case $Name expected failure=$ExpectedFailure but observed failure=$failed. $output" }
}

try {
    Reset-Fixture
    Invoke-Case $false 'valid-operator-values'

    Reset-Fixture
    Remove-Item -LiteralPath (Join-Path $fixtureDeployment 'minio.license')
    Invoke-Case $true 'missing-license-file'

    Reset-Fixture
    $path = Join-Path $fixtureDeployment '.env.test'
    ([IO.File]::ReadAllText($path) -replace '(?m)^VITARISH_TEST_MAIN_IMAGE=.*$', ('VITARISH_TEST_MAIN_IMAGE=registry.vitarish.com/vitarish/main@sha256:' + ('2' * 64))) | Set-Content -LiteralPath $path -NoNewline
    Invoke-Case $true 'image-differs-from-release-manifest'

    Reset-Fixture
    $path = Join-Path $fixtureDeployment '.env.shared'
    ([IO.File]::ReadAllText($path) -replace '(?m)^VITARISH_CADDY_IMAGE=.*$', 'VITARISH_CADDY_IMAGE=registry.vitarish.com/vitarish/caddy:latest') | Set-Content -LiteralPath $path -NoNewline
    Invoke-Case $true 'mutable-caddy-image'

    Reset-Fixture
    $path = Join-Path $fixtureDeployment '.env.shared'
    ([IO.File]::ReadAllText($path) -replace '(?m)^VITARISH_PRODUCTION_CLINIC_WILDCARD_HOST=.*\r?\n', '') | Set-Content -LiteralPath $path -NoNewline
    Invoke-Case $true 'missing-clinic-wildcard-host'

    Reset-Fixture
    $output = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Deploy-ServerBundles.ps1') -ReleaseManifest $fixtureManifest -RepositoryRoot $fixtureRoot 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0 -or $output -notmatch 'Provide the public ingress IPv4 address') {
        throw 'Deployment without a target public ingress address did not stop before startup.'
    }

    Reset-Fixture
    $output = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Deploy-ServerBundles.ps1') -ValidateOnly -Environment Test -ReleaseManifest $fixtureManifest -RepositoryRoot $fixtureRoot 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) { throw "Single-environment update preflight failed with both environments configured. $output" }

    Remove-Item -LiteralPath (Join-Path $fixtureDeployment '.env.production')
    $output = & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Deploy-ServerBundles.ps1') -ValidateOnly -Environment Test -ReleaseManifest $fixtureManifest -RepositoryRoot $fixtureRoot 2>&1 | Out-String
    if ($LASTEXITCODE -eq 0) { throw 'Single-environment update preflight ignored the other environment.' }

    Write-Output 'Server deployment preflight cases passed (8 cases).'
}
finally {
    $resolved = [IO.Path]::GetFullPath($fixtureRoot)
    if (-not $resolved.StartsWith($temporaryRoot, [StringComparison]::OrdinalIgnoreCase) -or
        [IO.Path]::GetFileName($resolved) -ne "vitarish-server-deployment-$PID") {
        throw 'Unexpected deployment fixture cleanup path.'
    }
    if (Test-Path -LiteralPath $resolved) { Remove-Item -LiteralPath $resolved -Recurse -Force }
}

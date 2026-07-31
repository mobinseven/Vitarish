[CmdletBinding()]
param(
    [switch]$Provider,
    [switch]$KeepServices
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\.."))
$composeFile = Join-Path $repoRoot "src\Hosts\docker-compose.main.yml"
$testProject = Join-Path $repoRoot "tests\Modules\Intelligence.Tests\Intelligence.Tests.csproj"

if ([string]::IsNullOrWhiteSpace($env:REDIS_PASSWORD)) {
    throw "REDIS_PASSWORD must be supplied; the harness never invents or prints credentials."
}

Push-Location $repoRoot
try {
    docker compose -f $composeFile up -d postgres redis
    if ($LASTEXITCODE -ne 0) { throw "PostgreSQL/Redis startup failed." }

    dotnet test $testProject -c Release --no-restore --logger "console;verbosity=minimal" `
        --filter "FullyQualifiedName~Intelligence.Tests.Agents|FullyQualifiedName~Intelligence.Tests.Contracts"
    if ($LASTEXITCODE -ne 0) { throw "Deterministic Intelligence tests failed." }

    if ($Provider) {
        if ([string]::IsNullOrWhiteSpace($env:AGENT_RUNTIME_BASE_URL)) {
            throw "AGENT_RUNTIME_BASE_URL must be supplied when -Provider is used."
        }
        dotnet test $testProject -c Release --no-restore --logger "console;verbosity=minimal" `
            --filter "FullyQualifiedName~AgentRuntimeHttpRunnerTests"
        if ($LASTEXITCODE -ne 0) { throw "Provider hot tests failed." }
    }
}
finally {
    if (-not $KeepServices) {
        docker compose -f $composeFile stop postgres redis | Out-Null
    }
    Pop-Location
}

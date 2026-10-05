[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$AppName,

    [Parameter(Mandatory = $false)]
    [string]$ApiBaseUrl
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$webRoot = $PSScriptRoot
$appsRoot = Join-Path $webRoot 'apps'

if (-not (Test-Path -LiteralPath $appsRoot -PathType Container)) {
    throw "The apps directory was not found at '$appsRoot'."
}

$apps = @(
    Get-ChildItem -LiteralPath $appsRoot -Directory |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'package.json') -PathType Leaf } |
        Sort-Object Name
)

if ($apps.Count -eq 0) {
    throw "No npm apps were found under '$appsRoot'."
}

if (-not (Get-Command npm -CommandType Application -ErrorAction SilentlyContinue)) {
    throw 'npm was not found. Install Node.js and ensure npm is available on PATH.'
}

if (-not (Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found. Install the .NET SDK and ensure dotnet is available on PATH.'
}

$devCertificatePath = Join-Path $env:TEMP 'vitarish-dev-cert.pem'
& dotnet dev-certs https --export-path $devCertificatePath --format Pem --no-password
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $devCertificatePath -PathType Leaf)) {
    throw "The .NET HTTPS development certificate could not be exported to '$devCertificatePath'."
}

$env:NODE_EXTRA_CA_CERTS = $devCertificatePath
$env:NODE_USE_SYSTEM_CA = '1'
$nodeOptions = [Environment]::GetEnvironmentVariable('NODE_OPTIONS', 'Process')
if ($nodeOptions -notmatch '(^|\s)--use-system-ca(\s|$)') {
    $env:NODE_OPTIONS = (($nodeOptions, '--use-system-ca' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' ')
}

$selectedApp = $null

if ($AppName) {
    $selectedApp = $apps | Where-Object { $_.Name -eq $AppName } | Select-Object -First 1
    if (-not $selectedApp) {
        throw "App '$AppName' not found. Available apps: $($apps.Name -join ', ')"
    }
} else {
    Write-Host 'Select a Web app to run in development mode:'
    for ($index = 0; $index -lt $apps.Count; $index++) {
        Write-Host ("  [{0}] {1}" -f ($index + 1), $apps[$index].Name)
    }

    do {
        $selection = Read-Host "Enter a number (1-$($apps.Count))"
        $selectedIndex = 0
        $isValidSelection = [int]::TryParse($selection, [ref]$selectedIndex) -and
            $selectedIndex -ge 1 -and
            $selectedIndex -le $apps.Count

        if (-not $isValidSelection) {
            Write-Warning 'Enter one of the displayed numbers.'
        }
    } until ($isValidSelection)

    $selectedApp = $apps[$selectedIndex - 1]
}

if (-not $ApiBaseUrl -and $selectedApp.Name -ne 'agent-runtime') {
    $defaultApiBaseUrl = 'https://localhost:7272'
    $ApiBaseUrl = Read-Host "Main API origin proxied at /api [$defaultApiBaseUrl]"
    if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
        $ApiBaseUrl = $defaultApiBaseUrl
    }
}

if (-not [string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    # The browser calls /api on the app's own origin, as in production; Next proxies it to
    # Main (scripts/api-proxy-rewrites.mjs) so SameSite=Lax cookies stay first-party.
    $env:NEXT_PUBLIC_API_BASE_URL = ''
    $env:VITARISH_API_PROXY_TARGET = $ApiBaseUrl
}

if ($selectedApp.Name -eq 'agent-runtime') {
    if ([string]::IsNullOrWhiteSpace($env:AGENT_RUNTIME_SHARED_SECRET) -and
        -not [string]::IsNullOrWhiteSpace($env:VITARISH_AGENT_RUNTIME_SECRET)) {
        $env:AGENT_RUNTIME_SHARED_SECRET = $env:VITARISH_AGENT_RUNTIME_SECRET
    }

    if ([string]::IsNullOrWhiteSpace($env:OPENAI_API_KEY)) {
        throw 'OPENAI_API_KEY must be set in the current environment when running agent-runtime.'
    }

    if ([string]::IsNullOrWhiteSpace($env:AGENT_RUNTIME_SHARED_SECRET)) {
        throw 'AGENT_RUNTIME_SHARED_SECRET or VITARISH_AGENT_RUNTIME_SECRET must be set in the current environment when running agent-runtime.'
    }
}

Write-Host "Starting '$($selectedApp.Name)' in development mode..."
Write-Host "App directory: $($selectedApp.FullName)"

Push-Location $selectedApp.FullName
try {
    & npm run dev
    if ($LASTEXITCODE -ne 0) {
        throw "npm run dev failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}

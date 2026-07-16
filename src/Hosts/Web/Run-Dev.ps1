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

if (-not $ApiBaseUrl) {
    $defaultApiBaseUrl = 'https://localhost:7272'
    $ApiBaseUrl = Read-Host "NEXT_PUBLIC_API_BASE_URL [$defaultApiBaseUrl]"
    if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
        $ApiBaseUrl = $defaultApiBaseUrl
    }
}

$package = Get-Content (Join-Path $selectedApp.FullName 'package.json') -Raw | ConvertFrom-Json
if (-not $package.name) {
    throw "App '$($selectedApp.Name)' does not define a package name."
}

$env:NEXT_PUBLIC_API_BASE_URL = $ApiBaseUrl

Write-Host "Starting '$($selectedApp.Name)' in development mode..."
Push-Location $webRoot
try {
    & npm run dev --workspace $package.name
    if ($LASTEXITCODE -ne 0) {
        throw "npm run dev failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}

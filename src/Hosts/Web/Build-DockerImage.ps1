[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$AppName,

    [Parameter(Mandatory = $false)]
    [string]$ImageTag,

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

if (-not (Get-Command docker -CommandType Application -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI was not found. Install Docker and ensure docker is available on PATH.'
}

if ([string]::IsNullOrWhiteSpace($env:HEROUI_AUTH_TOKEN)) {
    throw 'HEROUI_AUTH_TOKEN must be set in the current environment because the app Dockerfiles require it as a BuildKit secret.'
}

$selectedApp = $null

if ($AppName) {
    $selectedApp = $apps | Where-Object { $_.Name -eq $AppName } | Select-Object -First 1
    if (-not $selectedApp) {
        throw "App '$AppName' not found. Available apps: $($apps.Name -join ', ')"
    }
} else {
    Write-Host 'Select a Web app to build:'
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

$dockerfile = Join-Path $selectedApp.FullName 'Dockerfile'
if (-not (Test-Path -LiteralPath $dockerfile -PathType Leaf)) {
    throw "App '$($selectedApp.Name)' does not have a Dockerfile at '$dockerfile'."
}

if (-not $ImageTag) {
    $defaultImageTag = "vitarish/$($selectedApp.Name):latest"
    $ImageTag = Read-Host "Image tag [$defaultImageTag]"
    if ([string]::IsNullOrWhiteSpace($ImageTag)) {
        $ImageTag = $defaultImageTag
    }
}

if (-not $ApiBaseUrl) {
    $defaultApiBaseUrl = 'https://localhost:7272'
    $ApiBaseUrl = Read-Host "NEXT_PUBLIC_API_BASE_URL [$defaultApiBaseUrl]"
    if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
        $ApiBaseUrl = $defaultApiBaseUrl
    }
}

Write-Host "Building '$ImageTag' from app '$($selectedApp.Name)'..."
& docker build `
    --file $dockerfile `
    --tag $ImageTag `
    --build-arg "NEXT_PUBLIC_API_BASE_URL=$ApiBaseUrl" `
    --secret 'id=HEROUI_AUTH_TOKEN,env=HEROUI_AUTH_TOKEN' `
    $webRoot

if ($LASTEXITCODE -ne 0) {
    throw "Docker build failed with exit code $LASTEXITCODE."
}

Write-Host "Built Docker image '$ImageTag'."

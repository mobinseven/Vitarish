[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$webRoot = $PSScriptRoot
$appsRoot = Join-Path $webRoot 'apps'
$apps = @(
    Get-ChildItem -LiteralPath $appsRoot -Directory |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'Dockerfile') -PathType Leaf } |
        Sort-Object Name
)

if ($apps.Count -eq 0) {
    throw "No Docker-enabled apps were found under '$appsRoot'."
}

if (-not (Get-Command docker -CommandType Application -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI was not found. Install Docker and ensure docker is available on PATH.'
}

if ([string]::IsNullOrWhiteSpace($env:HEROUI_AUTH_TOKEN)) {
    throw 'HEROUI_AUTH_TOKEN must be set in the current environment because the app Dockerfiles require it as a BuildKit secret.'
}

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
$defaultImageTag = "vitarish/$($selectedApp.Name):latest"
$imageTag = Read-Host "Image tag [$defaultImageTag]"
if ([string]::IsNullOrWhiteSpace($imageTag)) {
    $imageTag = $defaultImageTag
}

$defaultApiBaseUrl = 'https://localhost:7272'
$apiBaseUrl = Read-Host "NEXT_PUBLIC_API_BASE_URL [$defaultApiBaseUrl]"
if ([string]::IsNullOrWhiteSpace($apiBaseUrl)) {
    $apiBaseUrl = $defaultApiBaseUrl
}

$dockerfile = Join-Path $selectedApp.FullName 'Dockerfile'

Write-Host "Building '$imageTag' from app '$($selectedApp.Name)'..."
& docker build `
    --file $dockerfile `
    --tag $imageTag `
    --build-arg "NEXT_PUBLIC_API_BASE_URL=$apiBaseUrl" `
    --secret 'id=HEROUI_AUTH_TOKEN,env=HEROUI_AUTH_TOKEN' `
    $webRoot

if ($LASTEXITCODE -ne 0) {
    throw "Docker build failed with exit code $LASTEXITCODE."
}

Write-Host "Built Docker image '$imageTag'."

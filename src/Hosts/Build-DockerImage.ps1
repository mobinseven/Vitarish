[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [ValidateSet('E2e', 'Main')]
    [string]$HostName,

    [Parameter(Mandatory = $false)]
    [string]$ImageTag,

    [Parameter(Mandatory = $false)]
    [string]$ModuleBuildProfile = 'All'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$hostsRoot = $PSScriptRoot
$repositoryRoot = Split-Path (Split-Path $hostsRoot -Parent) -Parent
$hosts = @('E2e', 'Main')

if (-not (Get-Command docker -CommandType Application -ErrorAction SilentlyContinue)) {
    throw 'Docker CLI was not found. Install Docker and ensure docker is available on PATH.'
}

if (-not $HostName) {
    Write-Host 'Select a host to build:'
    for ($index = 0; $index -lt $hosts.Count; $index++) {
        Write-Host ("  [{0}] {1}" -f ($index + 1), $hosts[$index])
    }

    do {
        $selection = Read-Host "Enter a number (1-$($hosts.Count))"
        $selectedIndex = 0
        $isValidSelection = [int]::TryParse($selection, [ref]$selectedIndex) -and
            $selectedIndex -ge 1 -and
            $selectedIndex -le $hosts.Count

        if (-not $isValidSelection) {
            Write-Warning 'Enter one of the displayed numbers.'
        }
    } until ($isValidSelection)

    $HostName = $hosts[$selectedIndex - 1]
}

$hostRoot = Join-Path $hostsRoot $HostName
$dockerfile = Join-Path $hostRoot 'Dockerfile'

if (-not (Test-Path -LiteralPath $dockerfile -PathType Leaf)) {
    throw "Host '$HostName' does not have a Dockerfile at '$dockerfile'."
}

if (-not $ImageTag) {
    $defaultImageTag = "vitarish/$($HostName.ToLowerInvariant()):latest"
    $ImageTag = Read-Host "Image tag [$defaultImageTag]"
    if ([string]::IsNullOrWhiteSpace($ImageTag)) {
        $ImageTag = $defaultImageTag
    }
}

$dockerBuildArguments = @(
    '--file', $dockerfile,
    '--tag', $ImageTag
)

if ($HostName -eq 'Main') {
    $dockerBuildArguments += @('--build-arg', "MODULE_BUILD_PROFILE=$ModuleBuildProfile")
}

$dockerBuildArguments += $repositoryRoot

Write-Host "Building '$ImageTag' from host '$HostName'..."
& docker build @dockerBuildArguments

if ($LASTEXITCODE -ne 0) {
    throw "Docker build failed with exit code $LASTEXITCODE."
}

Write-Host "Built Docker image '$ImageTag'."

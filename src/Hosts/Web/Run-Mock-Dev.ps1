[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$AppName,

    [Parameter(Mandatory = $false)]
    [ValidateSet('ready', 'empty', 'loading', 'error')]
    [string]$Scenario = 'ready'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$webRoot = $PSScriptRoot
$mockServerScript = Join-Path $webRoot 'scripts/mock-app-server.mjs'

if (-not (Test-Path -LiteralPath $mockServerScript -PathType Leaf)) {
    throw "The mock app server script was not found at '$mockServerScript'."
}

if (-not (Get-Command npm -CommandType Application -ErrorAction SilentlyContinue)) {
    throw 'npm was not found. Install Node.js and ensure npm is available on PATH.'
}

$mockApps = @(
    'private-practice-owner',
    'patients',
    'platform-admin',
    'knowledge-qa-demo',
    'vitarish-landing'
)

if ($AppName) {
    if ($mockApps -notcontains $AppName) {
        throw "Mock app '$AppName' is not supported. Available apps: $($mockApps -join ', ')"
    }
} else {
    Write-Host 'Select a Web app to run in mock development mode:'
    for ($index = 0; $index -lt $mockApps.Count; $index++) {
        Write-Host ("  [{0}] {1}" -f ($index + 1), $mockApps[$index])
    }

    do {
        $selection = Read-Host "Enter a number (1-$($mockApps.Count))"
        $selectedIndex = 0
        $isValidSelection = [int]::TryParse($selection, [ref]$selectedIndex) -and
            $selectedIndex -ge 1 -and
            $selectedIndex -le $mockApps.Count

        if (-not $isValidSelection) {
            Write-Warning 'Enter one of the displayed numbers.'
        }
    } until ($isValidSelection)

    $AppName = $mockApps[$selectedIndex - 1]
}

$workspaceName = "@vitarish/$AppName"
Write-Host "Starting '$workspaceName' in mock development mode (scenario: $Scenario)..."
Write-Host "App directory: $(Join-Path $webRoot "apps/$AppName")"

Push-Location $webRoot
try {
    & npm run dev:mock:$AppName -- --scenario $Scenario
    if ($LASTEXITCODE -ne 0) {
        throw "Mock development server failed with exit code $LASTEXITCODE."
    }
} finally {
    Pop-Location
}

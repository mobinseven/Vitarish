[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$AppName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$webRoot = $PSScriptRoot
$repositoryRoot = Resolve-Path (Join-Path $webRoot '..\..\..')
$appsRoot = Join-Path $webRoot 'apps'
$e2eProject = Join-Path $repositoryRoot 'src\Hosts\E2e\E2e.csproj'
$apiOrigin = 'https://localhost:7272'

if (-not (Test-Path -LiteralPath $appsRoot -PathType Container)) {
    throw "The apps directory was not found at '$appsRoot'."
}

if (-not (Test-Path -LiteralPath $e2eProject -PathType Leaf)) {
    throw "The E2E host project was not found at '$e2eProject'."
}

$apps = @(
    Get-ChildItem -LiteralPath $appsRoot -Directory |
        Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName 'package.json') -PathType Leaf } |
        Sort-Object Name
)

if ($apps.Count -eq 0) {
    throw "No npm apps were found under '$appsRoot'."
}

$npmCommand = Get-Command npm -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $npmCommand) {
    throw 'npm was not found. Install Node.js and ensure npm is available on PATH.'
}

$dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $dotnetCommand) {
    throw '.NET was not found. Install the .NET SDK and ensure dotnet is available on PATH.'
}

$selectedApp = $null

if ($AppName) {
    $selectedApp = $apps | Where-Object { $_.Name -eq $AppName } | Select-Object -First 1
    if (-not $selectedApp) {
        throw "App '$AppName' not found. Available apps: $($apps.Name -join ', ')"
    }
} else {
    Write-Host 'Select a Web app to run with the E2E host:'
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

$packagePath = Join-Path $selectedApp.FullName 'package.json'
$package = Get-Content -LiteralPath $packagePath -Raw | ConvertFrom-Json
$devScript = [string]$package.scripts.dev
if ([string]::IsNullOrWhiteSpace($devScript)) {
    throw "App '$($selectedApp.Name)' does not define an npm dev script."
}

$portMatch = [regex]::Match($devScript, '(?:^|\s)(?:--port|-p)\s+(\d{1,5})(?:\s|$)')
if (-not $portMatch.Success) {
    throw "Could not determine the development port from '$packagePath'. Expected the dev script to declare --port."
}

$appPort = [int]$portMatch.Groups[1].Value
if ($appPort -lt 1 -or $appPort -gt 65535) {
    throw "The development port '$appPort' for app '$($selectedApp.Name)' is outside the valid range."
}

$webOrigin = "http://localhost:$appPort"
$devCertificatePath = Join-Path $env:TEMP 'vitarish-dev-cert.pem'
& $dotnetCommand.Source dev-certs https --export-path $devCertificatePath --format Pem --no-password
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $devCertificatePath -PathType Leaf)) {
    throw "The .NET HTTPS development certificate could not be exported to '$devCertificatePath'."
}

function New-ProcessEnvironment {
    $environment = @{}
    foreach ($entry in [Environment]::GetEnvironmentVariables('Process').GetEnumerator()) {
        $environment[[string]$entry.Key] = [string]$entry.Value
    }

    return $environment
}

function Stop-ProcessTree {
    param(
        [Parameter(Mandatory = $false)]
        [System.Diagnostics.Process]$Process
    )

    if (-not $Process) {
        return
    }

    $Process.Refresh()
    if ($Process.HasExited) {
        return
    }

    & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null

    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        Start-Sleep -Milliseconds 100
        $Process.Refresh()
    } while (-not $Process.HasExited -and [DateTime]::UtcNow -lt $deadline)

    if (-not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
}

$childEnvironmentKeys = @(
    'NODE_EXTRA_CA_CERTS',
    'NODE_USE_SYSTEM_CA',
    'NODE_OPTIONS',
    'ASPNETCORE_ENVIRONMENT',
    'ASPNETCORE_URLS',
    'VITARISH_E2E_API_ORIGIN',
    'VITARISH_E2E_WEB_ORIGIN',
    'VITARISH_E2E_PASSWORD',
    'NEXT_PUBLIC_API_BASE_URL',
    'VITARISH_E2E_USERNAME',
    'OPENAI_API_KEY',
    'AGENT_RUNTIME_SHARED_SECRET',
    'VITARISH_AGENT_RUNTIME_SECRET'
)
$originalChildEnvironment = @{}
foreach ($key in $childEnvironmentKeys) {
    $originalChildEnvironment[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}

function Set-ChildEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [System.Collections.IDictionary]$Environment
    )

    foreach ($key in $childEnvironmentKeys) {
        $value = $Environment[$key]
        [Environment]::SetEnvironmentVariable($key, $value, 'Process')
    }
}

function Restore-ChildEnvironment {
    foreach ($key in $childEnvironmentKeys) {
        [Environment]::SetEnvironmentVariable($key, $originalChildEnvironment[$key], 'Process')
    }
}

$password = [string]$env:VITARISH_E2E_PASSWORD
if ([string]::IsNullOrWhiteSpace($password)) {
    $passwordBytes = New-Object byte[] 24
    $randomNumberGenerator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $randomNumberGenerator.GetBytes($passwordBytes)
    } finally {
        $randomNumberGenerator.Dispose()
    }

    $password = "{0}!Aa1" -f [Convert]::ToBase64String($passwordBytes)
}

if ($password.Length -lt 12) {
    throw 'VITARISH_E2E_PASSWORD must be at least 12 characters.'
}
$baseEnvironment = New-ProcessEnvironment
$baseEnvironment['NODE_EXTRA_CA_CERTS'] = $devCertificatePath
$baseEnvironment['NODE_USE_SYSTEM_CA'] = '1'
$nodeOptions = [string]$baseEnvironment['NODE_OPTIONS']
if ($nodeOptions -notmatch '(^|\s)--use-system-ca(\s|$)') {
    $baseEnvironment['NODE_OPTIONS'] = (($nodeOptions, '--use-system-ca' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }) -join ' ')
}

$e2eEnvironment = $baseEnvironment.Clone()
$e2eEnvironment['ASPNETCORE_ENVIRONMENT'] = 'E2E'
$e2eEnvironment['ASPNETCORE_URLS'] = $apiOrigin
$e2eEnvironment['VITARISH_E2E_API_ORIGIN'] = $apiOrigin
$e2eEnvironment['VITARISH_E2E_WEB_ORIGIN'] = $webOrigin
$e2eEnvironment['VITARISH_E2E_PASSWORD'] = $password

$appEnvironment = $baseEnvironment.Clone()
$appEnvironment['NEXT_PUBLIC_API_BASE_URL'] = $apiOrigin
$appEnvironment['VITARISH_E2E_USERNAME'] = 'e2e-owner'
$appEnvironment['VITARISH_E2E_PASSWORD'] = $password

if ($selectedApp.Name -eq 'agent-runtime') {
    if ([string]::IsNullOrWhiteSpace($appEnvironment['AGENT_RUNTIME_SHARED_SECRET']) -and
        -not [string]::IsNullOrWhiteSpace($appEnvironment['VITARISH_AGENT_RUNTIME_SECRET'])) {
        $appEnvironment['AGENT_RUNTIME_SHARED_SECRET'] = $appEnvironment['VITARISH_AGENT_RUNTIME_SECRET']
    }

    if ([string]::IsNullOrWhiteSpace($appEnvironment['OPENAI_API_KEY'])) {
        throw 'OPENAI_API_KEY must be set in the current environment when running agent-runtime.'
    }

    if ([string]::IsNullOrWhiteSpace($appEnvironment['AGENT_RUNTIME_SHARED_SECRET'])) {
        throw 'AGENT_RUNTIME_SHARED_SECRET or VITARISH_AGENT_RUNTIME_SECRET must be set in the current environment when running agent-runtime.'
    }
}

$e2eProcess = $null
$appProcess = $null
$script:stopRequested = $false
$cancelKeyPressHandler = [ConsoleCancelEventHandler]{
    param($sender, $eventArgs)
    $eventArgs.Cancel = $true
    $script:stopRequested = $true
}

try {
    [Console]::add_CancelKeyPress($cancelKeyPressHandler)

    Write-Host "Starting E2E host at $apiOrigin..."
    Set-ChildEnvironment $e2eEnvironment
    try {
        $e2eProcess = Start-Process `
            -FilePath $dotnetCommand.Source `
            -ArgumentList @('run', '--project', $e2eProject, '--no-launch-profile', '--urls', $apiOrigin) `
            -WorkingDirectory $repositoryRoot `
            -NoNewWindow `
            -PassThru
    } finally {
        Restore-ChildEnvironment
    }

    Write-Host "Starting '$($selectedApp.Name)' at $webOrigin..."
    Set-ChildEnvironment $appEnvironment
    try {
        $appProcess = Start-Process `
            -FilePath $npmCommand.Source `
            -ArgumentList @('run', 'dev') `
            -WorkingDirectory $selectedApp.FullName `
            -NoNewWindow `
            -PassThru
    } finally {
        Restore-ChildEnvironment
    }

    Write-Host 'Both processes are running. Press Ctrl+C to stop the E2E host and the selected app.'

    while (-not $script:stopRequested) {
        foreach ($processInfo in @(
                @{ Label = 'E2E host'; Process = $e2eProcess },
                @{ Label = $selectedApp.Name; Process = $appProcess }
            )) {
            $processInfo.Process.Refresh()
            if ($processInfo.Process.HasExited) {
                throw "$($processInfo.Label) exited with code $($processInfo.Process.ExitCode). The other process will be stopped."
            }
        }

        Start-Sleep -Milliseconds 250
    }
} finally {
    [Console]::remove_CancelKeyPress($cancelKeyPressHandler)
    Stop-ProcessTree $appProcess
    Stop-ProcessTree $e2eProcess
    Restore-ChildEnvironment
}

[CmdletBinding()]
param(
    [ValidateSet('Both', 'Test', 'Production')]
    [string]$Environment = 'Both',
    [switch]$ValidateOnly,
    [ValidateRange(60, 3600)]
    [int]$ReadinessTimeoutSeconds = 600,
    [string]$PublicIPv4,
    [string]$ReleaseManifest,
    [string]$RepositoryRoot
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
}
$deploymentRoot = Join-Path $RepositoryRoot 'src/Hosts/deployment'
$sharedFile = Join-Path $deploymentRoot '.env.shared'
$allEnvironments = @('test', 'production')
$selectedEnvironments = if ($Environment -eq 'Both') { @('test', 'production') } else { @($Environment.ToLowerInvariant()) }
if ([string]::IsNullOrWhiteSpace($ReleaseManifest)) { throw 'Provide the release image manifest path with -ReleaseManifest.' }

function Invoke-Docker([string[]]$Arguments, [string]$Step) {
    & docker @Arguments 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "$Step failed. Run the corresponding Docker command directly on the server for details." }
}

function Get-OperatorValues([string]$Path, [bool]$RequireOwnerOnly = $true) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing operator environment file: $Path" }
    if ($RequireOwnerOnly -and -not $IsWindows) {
        $mode = [int][IO.File]::GetUnixFileMode($Path)
        $publicBits = [int]([IO.UnixFileMode]::GroupRead -bor [IO.UnixFileMode]::GroupWrite -bor
            [IO.UnixFileMode]::GroupExecute -bor [IO.UnixFileMode]::OtherRead -bor
            [IO.UnixFileMode]::OtherWrite -bor [IO.UnixFileMode]::OtherExecute)
        if (($mode -band $publicBits) -ne 0) { throw "Restrict operator environment file permissions to its owner: $Path" }
    }
    $values = @{}
    foreach ($line in [IO.File]::ReadAllLines($Path)) {
        if ($line -match '^(?<name>VITARISH_[A-Z0-9_]+)=(?<value>.*)$') {
            if ($values.ContainsKey($Matches.name)) { throw "Duplicate operator variable $($Matches.name) in $Path." }
            if ($Matches.value -match '^(replace-with-|<[^>]+>)') { throw "Replace the example value for $($Matches.name) in $Path." }
            $values[$Matches.name] = $Matches.value
        }
    }
    return $values
}

function Assert-ReleaseImage([hashtable]$Values, [string]$Name) {
    if (-not $Values.ContainsKey($Name) -or $Values[$Name] -notmatch '@sha256:(?!0{64}$)[0-9a-f]{64}$') {
        throw "$Name must use a non-placeholder immutable image digest from the release manifest."
    }
}

function Get-ComposeArguments([string]$Bundle) {
    $args = @('compose', '--env-file', $sharedFile)
    if ($Bundle -ne 'ingress') { $args += @('--env-file', (Join-Path $deploymentRoot ".env.$Bundle")) }
    return $args + @('-f', (Join-Path $deploymentRoot "docker-compose.$Bundle.yml"))
}

function Assert-HostDns([string]$Hostname, [System.Net.IPAddress]$IngressAddress) {
    if ($Hostname.StartsWith('*.')) { $Hostname = 'deployment-probe.' + $Hostname.Substring(2) }
    try { $resolvedAddresses = [System.Net.Dns]::GetHostAddresses($Hostname) }
    catch { throw "DNS lookup failed for $Hostname." }
    if ($resolvedAddresses.Count -eq 0 -or @($resolvedAddresses | Where-Object { -not $_.Equals($IngressAddress) }).Count -ne 0) {
        throw "DNS for $Hostname does not point exclusively to the configured public ingress IPv4 address."
    }
}

function Test-PublicHost([string]$Hostname, [DateTime]$Deadline, [bool]$AllowClientError) {
    if ($Hostname.StartsWith('*.')) { $Hostname = 'deployment-probe.' + $Hostname.Substring(2) }
    $uri = [Uri]"https://$Hostname/"
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $handler.AllowAutoRedirect = $false
    $handler.UseProxy = $false
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(15)
    try {
        while ([DateTime]::UtcNow -lt $Deadline) {
            try {
                $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Head, $uri)
                try {
                    $response = $client.Send($request)
                    try {
                        $status = [int]$response.StatusCode
                        if (($status -lt 400 -or ($AllowClientError -and $status -lt 500)) -and
                            $response.Headers.Contains('Strict-Transport-Security')) {
                            Write-Host "HTTPS ready: $Hostname ($status)"
                            return
                        }
                    }
                    finally { $response.Dispose() }
                }
                finally { $request.Dispose() }
            }
            catch [System.Net.Http.HttpRequestException] { }
            catch [System.Threading.Tasks.TaskCanceledException] { }
            Start-Sleep -Seconds 10
        }
    }
    finally { $client.Dispose(); $handler.Dispose() }
    throw "HTTPS readiness failed for $Hostname. Check DNS, ACME, ingress logs, and upstream health."
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Docker CLI is required on the target server.' }
Invoke-Docker @('info', '--format', '{{.ServerVersion}}') 'Docker daemon preflight'
Invoke-Docker @('compose', 'version') 'Docker Compose preflight'

$sharedValues = Get-OperatorValues $sharedFile
$releaseValues = Get-OperatorValues $ReleaseManifest $false
Assert-ReleaseImage $sharedValues 'VITARISH_CADDY_IMAGE'
$operatorImageValues = @{}
foreach ($name in @($sharedValues.Keys | Where-Object { $_.EndsWith('_IMAGE') })) { $operatorImageValues[$name] = $sharedValues[$name] }
foreach ($item in $allEnvironments) {
    $values = Get-OperatorValues (Join-Path $deploymentRoot ".env.$item")
    $imageNames = @($values.Keys | Where-Object { $_ -match '^VITARISH_(TEST|PRODUCTION)_[A-Z0-9_]+_IMAGE$' })
    if ($imageNames.Count -eq 0) { throw "The $item operator environment has no application images." }
    foreach ($name in $imageNames) {
        Assert-ReleaseImage $values $name
        $operatorImageValues[$name] = $values[$name]
    }
}
foreach ($name in $operatorImageValues.Keys) {
    if (-not $releaseValues.ContainsKey($name) -or $operatorImageValues[$name] -cne $releaseValues[$name]) {
        throw "$name differs from the release image manifest."
    }
}

foreach ($item in $allEnvironments) {
    $prefix = "VITARISH_$($item.ToUpperInvariant())_"
    foreach ($name in @($sharedValues.Keys | Where-Object { $_.StartsWith($prefix) -and $_.EndsWith('_HOST') })) {
        $hostValue = $sharedValues[$name]
        if ([string]::IsNullOrWhiteSpace($hostValue) -or $hostValue.Contains('example.invalid') -or
            $hostValue -notmatch '^(\*\.)?[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)+$' -or
            ($name.EndsWith('WILDCARD_HOST') -ne $hostValue.StartsWith('*.'))) {
            throw "Set $name to a valid public DNS hostname before deployment."
        }
    }
}

& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Test-DeploymentBundles.ps1') -RepositoryRoot $RepositoryRoot
if ($LASTEXITCODE -ne 0) { throw 'Deployment bundle integrity gate failed.' }

$bundles = @('ingress') + $selectedEnvironments
foreach ($bundle in @('ingress', 'test', 'production')) {
    $compose = Get-ComposeArguments $bundle
    Invoke-Docker ($compose + @('config', '--quiet')) "$bundle Compose configuration"
}
if ($ValidateOnly) {
    Write-Host 'Deployment preflight passed. No containers were changed.'
    return
}

if ($Environment -ne 'Both') {
    $otherEnvironment = @($allEnvironments | Where-Object { $_ -notin $selectedEnvironments })[0]
    $otherCompose = Get-ComposeArguments $otherEnvironment
    $runningArguments = $otherCompose + @('ps', '--status', 'running', '-q')
    $running = & docker @runningArguments 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $running) {
        throw 'Deploy both environments on a new server before using a single-environment update.'
    }
}

$ingressAddress = $null
if ($PublicIPv4 -notmatch '^\d{1,3}(\.\d{1,3}){3}$' -or
    -not [System.Net.IPAddress]::TryParse($PublicIPv4, [ref]$ingressAddress) -or
    $ingressAddress.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork) {
    throw 'Provide the public ingress IPv4 address with -PublicIPv4 before deployment.'
}
foreach ($item in $allEnvironments) {
    $prefix = "VITARISH_$($item.ToUpperInvariant())_"
    foreach ($name in @($sharedValues.Keys | Where-Object { $_.StartsWith($prefix) -and $_.EndsWith('_HOST') })) {
        Assert-HostDns $sharedValues[$name] $ingressAddress
    }
}

foreach ($bundle in $bundles) {
    $compose = Get-ComposeArguments $bundle
    Write-Host "Pulling $bundle images..."
    Invoke-Docker ($compose + @('pull')) "$bundle image pull"
}
$ingressCompose = Get-ComposeArguments 'ingress'
Invoke-Docker ($ingressCompose + @('run', '--rm', '--no-deps', 'caddy', 'caddy', 'validate', '--config', '/etc/caddy/Caddyfile', '--adapter', 'caddyfile')) 'Caddy configuration validation'

foreach ($bundle in $bundles) {
    $compose = Get-ComposeArguments $bundle
    Write-Host "Starting $bundle..."
    Invoke-Docker ($compose + @('up', '-d', '--wait', '--wait-timeout', [string]$ReadinessTimeoutSeconds)) "$bundle startup and container readiness"
}

$deadline = [DateTime]::UtcNow.AddSeconds($ReadinessTimeoutSeconds)
foreach ($item in $allEnvironments) {
    $prefix = "VITARISH_$($item.ToUpperInvariant())_"
    foreach ($name in @($sharedValues.Keys | Where-Object { $_.StartsWith($prefix) -and $_.EndsWith('_HOST') } | Sort-Object)) {
        Test-PublicHost $sharedValues[$name] $deadline $name.EndsWith('WILDCARD_HOST')
    }
}
Write-Host 'Server deployment and HTTPS readiness passed.'

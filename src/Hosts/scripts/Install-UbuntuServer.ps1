[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^root@(?:[0-9]{1,3}\.){3}[0-9]{1,3}$')][string]$SshTarget,
    [Parameter(Mandatory)][ValidatePattern('^[0-9]{1,3}(?:\.[0-9]{1,3}){3}$')][string]$PublicIPv4,
    [ValidatePattern('^[A-Za-z0-9-]+$')][string]$RegistryUser,
    [Parameter(Mandatory)][string]$ReleaseManifest,
    [switch]$ValidateOnly,
    [ValidateRange(60, 3600)][int]$ReadinessTimeoutSeconds = 600
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$deploymentRoot = Join-Path $repositoryRoot 'src/Hosts/deployment'
$manifest = (Resolve-Path -LiteralPath $ReleaseManifest -ErrorAction Stop).Path
$manifestName = Split-Path -Leaf $manifest
if ($manifestName -notmatch '^release-images-[0-9a-f]{40}\.env$') { throw 'Use the release manifest for the exact commit.' }
$address = $null
if (-not [System.Net.IPAddress]::TryParse($PublicIPv4, [ref]$address) -or
    $address.AddressFamily -ne [System.Net.Sockets.AddressFamily]::InterNetwork -or
    $SshTarget -ne "root@$PublicIPv4") { throw 'SSH target must be root at the public ingress IPv4 address.' }
foreach ($command in @('ssh', 'scp', 'pwsh')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Missing operator command: $command" }
}

$files = @(
    (Join-Path $deploymentRoot 'Caddyfile'),
    (Join-Path $deploymentRoot 'docker-compose.ingress.yml'),
    (Join-Path $deploymentRoot 'docker-compose.test.yml'),
    (Join-Path $deploymentRoot 'docker-compose.production.yml'),
    (Join-Path $deploymentRoot '.env.shared'),
    (Join-Path $deploymentRoot '.env.test'),
    (Join-Path $deploymentRoot '.env.production'),
    $manifest
)
foreach ($file in $files) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing deployment input: $file" }
}

& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Test-DeploymentBundles.ps1') -RepositoryRoot $repositoryRoot
if ($LASTEXITCODE -ne 0) { throw 'Repository deployment gate failed.' }

$sshOptions = @('-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=yes')
& ssh @sshOptions $SshTarget 'install -m 0700 -d /opt/vitarish/deployment /opt/vitarish/scripts'
if ($LASTEXITCODE -ne 0) { throw 'Could not prepare remote directories.' }
& scp @sshOptions @files "${SshTarget}:/opt/vitarish/deployment/"
if ($LASTEXITCODE -ne 0) { throw 'Could not copy deployment inputs.' }
$remoteScripts = @(
    (Join-Path $PSScriptRoot 'Setup-UbuntuServer.sh'),
    (Join-Path $PSScriptRoot 'Prepare-UbuntuRelease.sh'),
    (Join-Path $PSScriptRoot 'Deploy-ServerBundles.sh')
)
& scp @sshOptions @remoteScripts "${SshTarget}:/opt/vitarish/scripts/"
if ($LASTEXITCODE -ne 0) { throw 'Could not copy server commands.' }
& ssh @sshOptions $SshTarget 'chmod 600 /opt/vitarish/deployment/.env.shared /opt/vitarish/deployment/.env.test /opt/vitarish/deployment/.env.production; chmod 700 /opt/vitarish/scripts/*.sh'
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict remote operator inputs.' }
$registryPreparation = if ($RegistryUser) {
    "bash /opt/vitarish/scripts/Prepare-UbuntuRelease.sh $RegistryUser /root/.config/vitarish/ghcr-token"
} else {
    'bash /opt/vitarish/scripts/Prepare-UbuntuRelease.sh'
}
& ssh @sshOptions $SshTarget $registryPreparation
if ($LASTEXITCODE -ne 0) { throw 'Remote Docker or registry preparation failed.' }

$remoteDeploy = "bash /opt/vitarish/scripts/Deploy-ServerBundles.sh --release-manifest /opt/vitarish/deployment/$manifestName --public-ipv4 $PublicIPv4 --readiness-timeout $ReadinessTimeoutSeconds"
if ($ValidateOnly) { $remoteDeploy += ' --validate-only' }
& ssh @sshOptions $SshTarget $remoteDeploy
if ($LASTEXITCODE -ne 0) { throw 'Remote deployment failed. Inspect the server output before retrying.' }

[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^root@(?:[0-9]{1,3}\.){3}[0-9]{1,3}$')][string]$SshTarget,
    [ValidatePattern('^[A-Za-z0-9-]+$')][string]$RegistryUser
)

$ErrorActionPreference = 'Stop'
foreach ($command in @('ssh', 'scp')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Missing operator command: $command" }
}

$remoteScripts = @(
    (Join-Path $PSScriptRoot 'Setup-UbuntuServer.sh'),
    (Join-Path $PSScriptRoot 'Prepare-UbuntuRelease.sh')
)
foreach ($script in $remoteScripts) {
    if (-not (Test-Path -LiteralPath $script -PathType Leaf)) { throw "Missing server bootstrap script: $script" }
}

$sshOptions = @('-o', 'BatchMode=yes', '-o', 'StrictHostKeyChecking=yes')
& ssh @sshOptions $SshTarget 'install -m 0700 -d /opt/vitarish/scripts'
if ($LASTEXITCODE -ne 0) { throw 'Could not prepare the server bootstrap directory.' }
& scp @sshOptions @remoteScripts "${SshTarget}:/opt/vitarish/scripts/"
if ($LASTEXITCODE -ne 0) { throw 'Could not copy the server bootstrap scripts.' }
& ssh @sshOptions $SshTarget 'chmod 700 /opt/vitarish/scripts/Setup-UbuntuServer.sh /opt/vitarish/scripts/Prepare-UbuntuRelease.sh'
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict the server bootstrap scripts.' }

$registryPreparation = if ($RegistryUser) {
    "bash /opt/vitarish/scripts/Prepare-UbuntuRelease.sh $RegistryUser /root/.config/vitarish/ghcr-token"
} else {
    'bash /opt/vitarish/scripts/Prepare-UbuntuRelease.sh'
}
& ssh @sshOptions $SshTarget $registryPreparation
if ($LASTEXITCODE -ne 0) { throw 'Docker or registry bootstrap failed.' }

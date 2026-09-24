#!/usr/bin/env bash
set -euo pipefail

if [[ $EUID -ne 0 ]] || { (($# != 0 && $# != 2)); }; then
    printf 'Usage: sudo bash %s [REGISTRY_USER TOKEN_FILE]\n' "$0" >&2
    exit 2
fi

if (($# == 0)); then
    credential_file=/root/.docker/config.json
else
    registry_user=$1
    token_file=$2
    [[ $registry_user =~ ^[A-Za-z0-9-]+$ ]] || { printf 'Invalid registry user.\n' >&2; exit 2; }
    [[ $token_file == /* ]] || { printf 'Use an absolute token file path.\n' >&2; exit 2; }
    credential_file=$token_file
fi
[[ -f $credential_file && ! -L $credential_file ]] || { printf 'Registry credential file is missing or is a symbolic link.\n' >&2; exit 1; }
mode=$(stat -c %a "$credential_file")
(( (8#$mode & 077) == 0 )) || { printf 'Restrict the registry credential file to its owner.\n' >&2; exit 1; }
[[ $(stat -c %u "$credential_file") == 0 ]] || { printf 'The registry credential file must be owned by root.\n' >&2; exit 1; }
if (($# == 0)) && ! grep -q 'ghcr.io' "$credential_file"; then
    printf 'Docker has no GHCR login.\n' >&2
    exit 1
fi

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
bash "$script_dir/Setup-UbuntuServer.sh"
if (($# == 2)); then
    docker login ghcr.io --username "$registry_user" --password-stdin < "$token_file" >/dev/null
fi
printf 'Docker and registry credentials are ready.\n'

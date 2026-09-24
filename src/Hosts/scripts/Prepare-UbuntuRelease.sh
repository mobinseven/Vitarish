#!/usr/bin/env bash
set -euo pipefail

if (($# != 2)) || [[ $EUID -ne 0 ]]; then
    printf 'Usage: sudo bash %s REGISTRY_USER TOKEN_FILE\n' "$0" >&2
    exit 2
fi

registry_user=$1
token_file=$2
[[ $registry_user =~ ^[A-Za-z0-9-]+$ ]] || { printf 'Invalid registry user.\n' >&2; exit 2; }
[[ $token_file == /root/.config/vitarish/ghcr-token ]] || { printf 'Use the protected server token path.\n' >&2; exit 2; }
[[ -f $token_file && ! -L $token_file ]] || { printf 'Registry token file is missing or is a symbolic link.\n' >&2; exit 1; }
mode=$(stat -c %a "$token_file")
(( (8#$mode & 077) == 0 )) || { printf 'Restrict the registry token file to its owner.\n' >&2; exit 1; }
[[ $(stat -c %u "$token_file") == 0 ]] || { printf 'The registry token file must be owned by root.\n' >&2; exit 1; }

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
bash "$script_dir/Setup-UbuntuServer.sh"
docker login ghcr.io --username "$registry_user" --password-stdin < "$token_file" >/dev/null
printf 'Docker and registry access are ready.\n'

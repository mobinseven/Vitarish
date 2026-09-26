#!/usr/bin/env bash
set -euo pipefail

if [[ $EUID -ne 0 ]]; then
    printf 'Run this setup as root so it can install Docker Engine and Compose.\n' >&2
    exit 1
fi

# shellcheck source=/dev/null
source /etc/os-release
if [[ ${ID:-} != ubuntu || ( ${VERSION_ID:-} != 20.04 && ${VERSION_ID:-} != 24.04 ) ]]; then
    printf 'This setup supports Ubuntu 20.04 and 24.04 only.\n' >&2
    exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
    conflicting_packages=(docker.io docker-compose docker-compose-v2 docker-doc docker-buildx podman-docker containerd runc)
    for package in "${conflicting_packages[@]}"; do
        if dpkg-query -W -f='${db:Status-Abbrev}' "$package" 2>/dev/null | grep -q '^ii'; then
            printf 'Conflicting package %s is installed. Resolve it before installing Docker Engine.\n' "$package" >&2
            exit 1
        fi
    done

    apt-get update
    apt-get install -y ca-certificates curl
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
    chmod a+r /etc/apt/keyrings/docker.asc

    cat > /etc/apt/sources.list.d/docker.sources <<EOF
Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: ${UBUNTU_CODENAME:-$VERSION_CODENAME}
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: /etc/apt/keyrings/docker.asc
EOF

    apt-get update
    apt-get install -y --no-install-recommends docker-ce docker-ce-cli containerd.io docker-compose-plugin
fi

if ! docker compose version >/dev/null 2>&1; then
    apt-get update
    apt-get install -y --no-install-recommends docker-compose-plugin
fi

systemctl enable --now docker
docker compose version
printf 'Docker host prerequisites are ready. Run the release preflight on the operator machine before deploying.\n'

#!/usr/bin/env bash
set -euo pipefail

usage() {
    printf 'Usage: %s --release-manifest PATH --public-ipv4 ADDRESS [--validate-only] [--environment both|test|production] [--readiness-timeout SECONDS]\n' "$0" >&2
    exit 2
}

manifest=
public_ipv4=
environment=both
readiness_timeout=600
validate_only=false
while (($#)); do
    case $1 in
        --release-manifest) (($# >= 2)) || usage; manifest=$2; shift 2 ;;
        --public-ipv4) (($# >= 2)) || usage; public_ipv4=$2; shift 2 ;;
        --environment) (($# >= 2)) || usage; environment=$2; shift 2 ;;
        --readiness-timeout) (($# >= 2)) || usage; readiness_timeout=$2; shift 2 ;;
        --validate-only) validate_only=true; shift ;;
        *) usage ;;
    esac
done
[[ -f $manifest && $public_ipv4 =~ ^[0-9]{1,3}(\.[0-9]{1,3}){3}$ ]] || usage
manifest=$(realpath -- "$manifest")
IFS=. read -r -a ipv4_octets <<< "$public_ipv4"
for octet in "${ipv4_octets[@]}"; do ((10#$octet <= 255)) || usage; done
[[ $environment == both || $environment == test || $environment == production ]] || usage
[[ $readiness_timeout =~ ^[0-9]+$ ]] && ((readiness_timeout >= 60 && readiness_timeout <= 3600)) || usage

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
deployment_dir=$(cd -- "$script_dir/../deployment" && pwd)
cd "$deployment_dir"
shared=.env.shared
test_env=.env.test
production_env=.env.production
license=minio.license

fail() { printf '%s\n' "$*" >&2; exit 1; }
value() { sed -n "s/^$2=//p" "$1" | head -n 1; }
compose() {
    local bundle=$1
    shift
    if [[ $bundle == ingress ]]; then
        docker compose --env-file "$shared" -f "docker-compose.$bundle.yml" "$@"
    else
        docker compose --env-file "$shared" --env-file ".env.$bundle" -f "docker-compose.$bundle.yml" "$@"
    fi
}

docker info --format '{{.ServerVersion}}' >/dev/null || fail 'Docker daemon is unavailable.'
docker compose version >/dev/null || fail 'Docker Compose is unavailable.'
for file in "$shared" "$test_env" "$production_env"; do
    [[ -f $file ]] || fail "Missing operator file: $file"
    mode=$(stat -c %a "$file")
    (( (8#$mode & 077) == 0 )) || fail "Restrict $file to its owner."
    if grep -Eq '^[A-Z0-9_]+=(replace-with-|<|.*example\.invalid|.*@sha256:0{64}$)' "$file"; then
        fail "Replace placeholder values in $file."
    fi
    duplicate=$(cut -d= -f1 "$file" | grep '^VITARISH_' | sort | uniq -d | head -n 1)
    [[ -z $duplicate ]] || fail "Duplicate operator variable $duplicate in $file."
done
[[ -f $license && ! -L $license && -r $license ]] || fail 'A readable MinIO AIStor license file is required.'
license_owner=$(stat -c %u "$license")
license_mode=$(stat -c %a "$license")
[[ $license_owner == 0 ]] || fail 'The MinIO AIStor license must be owned by root.'
(( (8#$license_mode & 077) == 0 )) || fail 'Restrict the MinIO AIStor license to its owner.'

for name in VITARISH_TEST_CLINIC_WILDCARD_HOST VITARISH_PRODUCTION_CLINIC_WILDCARD_HOST; do
    [[ -n $(value "$shared" "$name") ]] || fail "Missing $name in $shared."
done

for file in "$shared" "$test_env" "$production_env"; do
    while IFS='=' read -r name reference; do
        [[ $name == VITARISH_*_IMAGE ]] || continue
        if [[ $name == VITARISH_CADDY_IMAGE || $name == VITARISH_AISTOR_IMAGE || $name =~ ^VITARISH_(TEST|PRODUCTION)_[A-Z0-9_]+_IMAGE$ ]]; then
            [[ $reference =~ @sha256:[0-9a-f]{64}$ && ! $reference =~ @sha256:0{64}$ ]] || fail "$name lacks an immutable release digest."
        fi
        [[ $(value "$manifest" "$name") == "$reference" ]] || fail "$name differs from the release manifest."
    done < "$file"
done

for bundle in ingress test production; do
    compose "$bundle" config --quiet >/dev/null || fail "$bundle Compose configuration failed."
done

if [[ $validate_only == true ]]; then
    printf 'Deployment preflight passed. No containers were changed.\n'
    exit 0
fi

if [[ $environment != both ]]; then
    other=test
    [[ $environment == test ]] && other=production
    [[ -n $(compose "$other" ps --status running -q) ]] || fail 'Deploy both environments on a new server before a single-environment update.'
fi

while IFS='=' read -r name host; do
    [[ $name =~ ^VITARISH_(TEST|PRODUCTION)_[A-Z0-9_]+_HOST$ ]] || continue
    [[ $host =~ ^(\*\.)?[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)+$ ]] || fail "Invalid hostname in $name."
    if [[ $name == *_WILDCARD_HOST ]]; then
        [[ $host == \*.* ]] || fail "Expected a wildcard in $name."
        host=deployment-probe.${host#*.}
    else
        [[ $host != \*.* ]] || fail "Unexpected wildcard in $name."
    fi
    addresses=$(getent ahostsv4 "$host" | awk '{print $1}' | sort -u)
    [[ $addresses == "$public_ipv4" ]] || fail "DNS for $host does not resolve exclusively to $public_ipv4."
done < "$shared"

bundles=(ingress)
if [[ $environment == both ]]; then bundles+=(test production); else bundles+=("$environment"); fi
for bundle in "${bundles[@]}"; do compose "$bundle" pull --quiet; done
compose ingress run --rm --no-deps caddy caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile >/dev/null
for bundle in "${bundles[@]}"; do
    compose "$bundle" up -d --wait --wait-timeout "$readiness_timeout"
done

deadline=$((SECONDS + readiness_timeout))
while IFS='=' read -r name host; do
    [[ $name =~ ^VITARISH_(TEST|PRODUCTION)_[A-Z0-9_]+_HOST$ ]] || continue
    wildcard=false
    if [[ $host == \*.* ]]; then host=deployment-probe.${host#*.}; wildcard=true; fi
    while :; do
        headers=$(curl -sSI --max-time 15 --noproxy '*' "https://$host/" 2>/dev/null || true)
        status=$(printf '%s\n' "$headers" | awk 'toupper($1) ~ /^HTTP\// {code=$2} END {print code+0}')
        has_hsts=$(printf '%s\n' "$headers" | awk 'tolower($1) == "strict-transport-security:" {found=1} END {print found+0}')
        if ((has_hsts == 1 && status >= 200)) && { ((status < 400)) || [[ $wildcard == true && $status -lt 500 ]]; }; then
            printf 'HTTPS ready: %s\n' "$host"
            break
        fi
        ((SECONDS < deadline)) || fail "HTTPS readiness failed for $host."
        sleep 10
    done
done < "$shared"
printf 'Server deployment and HTTPS readiness passed.\n'

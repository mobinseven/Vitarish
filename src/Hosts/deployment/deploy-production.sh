#!/usr/bin/env bash
set -Eeuo pipefail

commit=${1:?Commit SHA is required.}
run_id=${2:?Workflow run ID is required.}
[[ $commit =~ ^[0-9a-f]{40}$ && $run_id =~ ^[1-9][0-9]*$ ]] || { printf 'Invalid workflow release identity.\n' >&2; exit 2; }

deployment=/opt/vitarish/deployment
incoming="$deployment/.workflow-incoming/$commit-$run_id"
backup="$deployment/.workflow-backups/$commit-$run_id"
release="$deployment/.workflow-release-$commit-$run_id.env"
state="$deployment/.workflow-deployed.json"
plan="$incoming/plan.json"
tracked=(Caddyfile docker-compose.ingress.yml docker-compose.test.yml docker-compose.production.yml)
dependencies=(postgres redis aistor elasticsearch)

command -v python3 >/dev/null && command -v docker >/dev/null && command -v curl >/dev/null || {
  printf 'Python, Docker, and curl are required on the production host.\n' >&2; exit 1;
}
docker info --format '{{.ServerVersion}}' >/dev/null
docker compose version >/dev/null

for file in .env.shared .env.production .env.test minio.license; do
  path="$deployment/$file"
  [[ -f $path && ! -L $path && $(stat -c %u "$path") == 0 ]] || { printf 'Missing or unsafe protected server file: %s\n' "$file" >&2; exit 1; }
  mode=$(stat -c %a "$path")
  (( (8#$mode & 077) == 0 )) || { printf 'Restrict protected server file: %s\n' "$file" >&2; exit 1; }
done
[[ -f $plan && -f $incoming/release.env ]] || { printf 'Release plan or image references are missing.\n' >&2; exit 1; }
for file in "${tracked[@]}"; do [[ -f $incoming/$file && ! -L $incoming/$file ]] || { printf 'Tracked deployment file is missing: %s\n' "$file" >&2; exit 1; }; done

python3 - "$plan" "$incoming/release.env" "$commit" "$run_id" <<'PY'
import json, re, sys
from pathlib import Path
plan = json.loads(Path(sys.argv[1]).read_text())
services = plan.get('services')
images = plan.get('images')
allowed = {'main', 'private-practice-owner', 'patients', 'platform-admin', 'pm', 'knowledge-qa', 'landing', 'agent-runtime', 'caddy'}
if (plan.get('commit') != sys.argv[3] or plan.get('runId') != sys.argv[4] or
    not isinstance(services, list) or not services or len(services) != len(set(services)) or not set(services) <= allowed or
    not isinstance(images, list) or not re.fullmatch(r'/[A-Za-z0-9/_-]+', plan.get('readinessPath', ''))):
    raise SystemExit('Invalid production release plan.')
expected = []
for image in images:
    if not isinstance(image, dict) or image.get('name') not in services or not re.fullmatch(r'VITARISH_[A-Z0-9_]+_IMAGE', image.get('variable', '')) or not re.fullmatch(r'ghcr\.io/[a-z0-9-]+/vitarish/[a-z0-9-]+@sha256:[0-9a-f]{64}', image.get('reference', '')):
        raise SystemExit('Invalid published image reference.')
    expected.append(image['variable'] + '=' + image['reference'])
if len(expected) != len(set(expected)) or Path(sys.argv[2]).read_text().splitlines() != expected:
    raise SystemExit('Published image references differ from the release plan.')
PY

mapfile -t services < <(python3 -c 'import json,sys; print("\n".join(json.load(open(sys.argv[1]))["services"]))' "$plan")
readiness_path=$(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["readinessPath"])' "$plan")

compose() { docker compose --env-file "$deployment/.env.shared" --env-file "$deployment/.env.production" --env-file "$release" -f "$deployment/docker-compose.production.yml" "$@"; }
compose_previous() { docker compose --env-file "$deployment/.env.shared" --env-file "$deployment/.env.production" -f "$deployment/docker-compose.production.yml" "$@"; }
ingress() { docker compose --env-file "$deployment/.env.shared" --env-file "$release" -f "$deployment/docker-compose.ingress.yml" "$@"; }
ingress_previous() { docker compose --env-file "$deployment/.env.shared" -f "$deployment/docker-compose.ingress.yml" "$@"; }
selected() { local item; for item in "${services[@]}"; do [[ $item == "$1" ]] && return 0; done; return 1; }
was_running() { local item; for item in "${previous_running[@]}"; do [[ $item == "$1" ]] && return 0; done; return 1; }

previous_running_text=$(docker ps --filter label=com.docker.compose.project=vitarish-production --format '{{.Label "com.docker.compose.service"}}')
previous_ingress_text=$(docker ps --filter label=com.docker.compose.project=vitarish-ingress --format '{{.Label "com.docker.compose.service"}}')
previous_running=()
if [[ -n $previous_running_text ]]; then mapfile -t previous_running <<< "$previous_running_text"; fi
previous_ingress=false
[[ $previous_ingress_text == *caddy* ]] && previous_ingress=true
if ((${#previous_running[@]})) && [[ ! -f $deployment/docker-compose.production.yml ]]; then
  printf 'A running production project has no previous Compose configuration.\n' >&2; exit 1
fi
if [[ $previous_ingress == true && ! -f $deployment/docker-compose.ingress.yml ]]; then
  printf 'A running ingress has no previous Compose configuration.\n' >&2; exit 1
fi

mkdir -m 0700 -p "$backup"
for file in "${tracked[@]}" .env.shared .env.production .workflow-deployed.json; do
  if [[ -e $deployment/$file || -L $deployment/$file ]]; then
    [[ -f $deployment/$file && ! -L $deployment/$file ]] || { printf 'Unsafe existing deployment file: %s\n' "$file" >&2; exit 1; }
    cp -a "$deployment/$file" "$backup/$file"
    printf '%s\n' "$file" >> "$backup/existed"
  else
    printf '%s\n' "$file" >> "$backup/missing"
  fi
done

attempted_apps=()
attempted_caddy=false
updated_production=
updated_shared=
rollback() {
  result=$?
  trap - ERR
  set +e
  restore_failed=false
  if ((${#attempted_apps[@]})); then compose stop "${attempted_apps[@]}" >/dev/null || restore_failed=true; fi
  if [[ $attempted_caddy == true && $previous_ingress == false ]]; then ingress stop caddy >/dev/null || restore_failed=true; fi
  while IFS= read -r file; do cp -a "$backup/$file" "$deployment/$file" || restore_failed=true; done < "$backup/existed"
  while IFS= read -r file; do rm -f -- "$deployment/$file" || restore_failed=true; done < "$backup/missing"
  rm -f -- "$release"
  [[ -z $updated_production ]] || rm -f -- "$updated_production"
  [[ -z $updated_shared ]] || rm -f -- "$updated_shared"
  if [[ $attempted_caddy == true && $previous_ingress == true ]]; then ingress_previous up -d --wait --no-deps --force-recreate caddy || restore_failed=true; fi
  previously_affected=()
  for service in "${attempted_apps[@]}"; do if was_running "$service"; then previously_affected+=("$service"); fi; done
  if ((${#previously_affected[@]})); then compose_previous up -d --wait --no-deps "${previously_affected[@]}" || restore_failed=true; fi
  if [[ $restore_failed == true ]]; then
    printf 'Production rollback failed; backup remains at %s. Inspect the host immediately.\n' "$backup" >&2
  else
    rm -rf -- "$backup" "$incoming"
    printf 'Previous production configuration and running services restored.\n' >&2
  fi
  exit "$result"
}
trap rollback ERR

install -m 0600 "$incoming/release.env" "$release"
for file in "${tracked[@]}"; do install -m 0644 "$incoming/$file" "$deployment/$file"; done
ingress config --quiet
compose config --quiet
docker compose --env-file "$deployment/.env.shared" --env-file "$deployment/.env.test" -f "$deployment/docker-compose.test.yml" config --quiet

python3 - "$deployment" "$release" <<'PY'
import json, re, subprocess, sys
from pathlib import Path
root = Path(sys.argv[1])
for name in ('.env.shared', '.env.production', '.env.test'):
    lines = (root / name).read_text().splitlines()
    if any(re.search(r'(replace-with-|example\.invalid|@sha256:0{64}$)', line) for line in lines):
        raise SystemExit('Protected server environment contains a placeholder.')
    keys = [line.partition('=')[0] for line in lines if line.startswith('VITARISH_')]
    if len(keys) != len(set(keys)):
        raise SystemExit('Protected server environment has duplicate variables.')
for bundle, envs in [('ingress', ['.env.shared', Path(sys.argv[2])]), ('production', ['.env.shared', '.env.production', Path(sys.argv[2])])]:
    command = ['docker', 'compose']
    for env in envs: command += ['--env-file', str(root / env) if isinstance(env, str) else str(env)]
    command += ['-f', str(root / ('docker-compose.' + bundle + '.yml')), 'config', '--format', 'json']
    config = json.loads(subprocess.check_output(command, text=True))
    for service in config['services'].values():
        image = service.get('image', '')
        if ('/vitarish/' in image or '/aistor/' in image) and not re.search(r'@sha256:(?!0{64}$)[0-9a-f]{64}$', image):
            raise SystemExit('A production or ingress image lacks an immutable digest.')
PY

ingress pull caddy
ingress run --interactive=false --rm --no-deps caddy-config-validator caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile
attempted_caddy=true
if selected caddy; then
  ingress up -d --wait --wait-timeout 600 --no-deps --force-recreate --pull never caddy
else
  ingress up -d --wait --wait-timeout 600 --no-deps --no-recreate --pull never caddy
fi

compose pull "${dependencies[@]}"
if [[ -f $backup/docker-compose.production.yml ]] && ! cmp -s "$backup/docker-compose.production.yml" "$deployment/docker-compose.production.yml"; then
  compose up -d --wait --wait-timeout 600 --no-deps --pull never "${dependencies[@]}"
else
  compose up -d --wait --wait-timeout 600 --no-deps --no-recreate --pull never "${dependencies[@]}"
fi

running_text=$(compose ps --status running --services)
running=()
if [[ -n $running_text ]]; then mapfile -t running <<< "$running_text"; fi
mapfile -t declared < <(compose config --services)
for service in "${declared[@]}"; do
  if [[ ! " ${dependencies[*]} " =~ " $service " ]] && { selected "$service" || [[ ! " ${running[*]} " =~ " $service " ]]; }; then
    attempted_apps+=("$service")
  fi
done
if ((${#attempted_apps[@]})); then
  compose pull "${attempted_apps[@]}"
  compose up -d --wait --wait-timeout 600 --no-deps --pull never "${attempted_apps[@]}"
fi

public_host=$(sed -n 's/^VITARISH_PRODUCTION_PUBLIC_HOST=//p' "$deployment/.env.shared")
[[ $public_host =~ ^[A-Za-z0-9.-]+$ && -n $public_host ]] || { printf 'Invalid production public hostname.\n' >&2; false; }
readiness_headers=$(mktemp)
if ! readiness_status=$(curl --fail --silent --show-error --retry 5 --retry-delay 5 --max-time 30 --resolve "$public_host:443:127.0.0.1" --proto '=https' --tlsv1.2 --dump-header "$readiness_headers" --write-out '%{http_code}' "https://$public_host$readiness_path" --output /dev/null); then
  if grep -iq '^server: caddy' "$readiness_headers"; then responder=caddy; elif grep -iq '^server: kestrel' "$readiness_headers"; then responder=main; else responder=unknown; fi
  rm -f "$readiness_headers"
  internal_readiness=$(compose exec -T patients node -e 'fetch("http://production-main:8080" + process.argv[1], { signal: AbortSignal.timeout(5000) }).then(response => console.log(`${response.status} ${response.headers.get("server")?.toLowerCase() === "kestrel" ? "main" : "unknown"}`)).catch(() => console.log("unavailable unknown"))' "$readiness_path" 2>/dev/null | tr -d '\r' | tail -n 1 || true)
  [[ $internal_readiness =~ ^([0-9]{3}|unavailable)\ (main|unknown)$ ]] || internal_readiness='unavailable unknown'
  printf 'Production readiness returned HTTP %s from %s.\n' "${readiness_status:-000}" "$responder" >&2
  printf 'Private Main readiness returned %s.\n' "$internal_readiness" >&2
  false
fi
rm -f "$readiness_headers"
probe_route() {
  local variable=$1 host code headers
  host=$(sed -n "s/^${variable}=//p" "$deployment/.env.shared")
  [[ $host =~ ^[A-Za-z0-9.-]+$ && -n $host ]] || { printf 'Invalid production route hostname.\n' >&2; return 1; }
  headers=$(mktemp)
  code=$(curl --silent --show-error --retry 5 --retry-delay 5 --max-time 30 --proto '=https' --tlsv1.2 --dump-header "$headers" --output /dev/null --write-out '%{http_code}' "https://$host/")
  case "$code" in 2*|301|302|303|307|308) ;; *) rm -f "$headers"; printf 'HTTPS route probe failed: %s.\n' "$variable" >&2; return 1 ;; esac
  grep -iq '^strict-transport-security:' "$headers" || { rm -f "$headers"; printf 'HSTS missing for production route: %s.\n' "$variable" >&2; return 1; }
  rm -f "$headers"
}
if selected caddy || ((${#services[@]} == 9)); then
  for variable in VITARISH_PRODUCTION_PUBLIC_HOST VITARISH_PRODUCTION_PATIENTS_HOST VITARISH_PRODUCTION_PLATFORM_ADMIN_HOST VITARISH_PRODUCTION_PM_HOST VITARISH_PRODUCTION_KNOWLEDGE_QA_HOST VITARISH_PRODUCTION_LANDING_HOST; do probe_route "$variable"; done
else
  for service in "${attempted_apps[@]}"; do
    case $service in
      main|private-practice-owner|agent-runtime) probe_route VITARISH_PRODUCTION_PUBLIC_HOST ;;
      patients) probe_route VITARISH_PRODUCTION_PATIENTS_HOST ;;
      platform-admin) probe_route VITARISH_PRODUCTION_PLATFORM_ADMIN_HOST ;;
      pm) probe_route VITARISH_PRODUCTION_PM_HOST ;;
      knowledge-qa) probe_route VITARISH_PRODUCTION_KNOWLEDGE_QA_HOST ;;
      landing) probe_route VITARISH_PRODUCTION_LANDING_HOST ;;
    esac
  done
fi

updated_production=$(mktemp "$deployment/.env.production.XXXXXX")
updated_shared=$(mktemp "$deployment/.env.shared.XXXXXX")
cp -a "$deployment/.env.production" "$updated_production"
cp -a "$deployment/.env.shared" "$updated_shared"
while IFS='=' read -r key reference; do
  [[ -n $key ]] || continue
  if [[ $key == VITARISH_CADDY_IMAGE ]]; then target=$updated_shared; else target=$updated_production; fi
  grep -q "^${key}=" "$target" || { printf 'Protected image variable is missing: %s\n' "$key" >&2; false; }
  sed -i "s|^${key}=.*|${key}=${reference}|" "$target"
done < "$release"
chmod 0600 "$updated_production" "$updated_shared"
mv -f "$updated_production" "$deployment/.env.production"
mv -f "$updated_shared" "$deployment/.env.shared"
updated_production=
updated_shared=

python3 - "$deployment" "$state" "$plan" <<'PY'
import hashlib, json, os, sys, tempfile
from pathlib import Path
root = Path(sys.argv[1])
plan = json.loads(Path(sys.argv[3]).read_text())
files = ['Caddyfile', 'docker-compose.ingress.yml', 'docker-compose.test.yml', 'docker-compose.production.yml', '.env.shared', '.env.production']
record = {'commit': plan['commit'], 'runId': plan['runId'], 'services': plan['services'],
          'sha256': {name: hashlib.sha256((root / name).read_bytes()).hexdigest() for name in files}}
with tempfile.NamedTemporaryFile(mode='w', dir=root, prefix='.workflow-deployed.', delete=False) as output:
    os.fchmod(output.fileno(), 0o600)
    json.dump(record, output, sort_keys=True)
    output.write('\n')
    temporary = output.name
os.replace(temporary, sys.argv[2])
PY

trap - ERR
rm -f -- "$release" || printf 'Temporary release file cleanup needs attention.\n' >&2
rm -rf -- "$backup" "$incoming" || printf 'Release staging cleanup needs attention.\n' >&2
printf 'Production commit %s passed Compose health, readiness, and HTTPS route probes.\n' "$commit"

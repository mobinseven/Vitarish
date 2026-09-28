#!/usr/bin/env bash
set -Eeuo pipefail

action=${1:?Release completion action is required.}
commit=${2:?Commit SHA is required.}
run_id=${3:?Workflow run ID is required.}
[[ $action == finalize || $action == rollback ]] || { printf 'Invalid release completion action.\n' >&2; exit 2; }
[[ $commit =~ ^[0-9a-f]{40}$ && $run_id =~ ^[1-9][0-9]*$ ]] || { printf 'Invalid workflow release identity.\n' >&2; exit 2; }

deployment=/opt/vitarish/deployment
incoming="$deployment/.workflow-incoming/$commit-$run_id"
backup="$deployment/.workflow-backups/$commit-$run_id"
release="$deployment/.workflow-release-$commit-$run_id.env"
state="$deployment/.workflow-deployed.json"

command -v python3 >/dev/null && command -v docker >/dev/null || { printf 'Python and Docker are required on the production host.\n' >&2; exit 1; }
[[ -d $backup && ! -L $backup && $(stat -c %u "$backup") == 0 && $(stat -c %a "$backup") == 700 ]] || {
  printf 'The matching root-owned rollback snapshot is missing or unsafe.\n' >&2; exit 1;
}
for file in "$backup/existed" "$backup/missing" "$backup/previous-running" "$backup/previous-ingress" "$backup/restore-dependencies" "$backup/declared-services" "$backup/dependencies" "$backup/attempted-apps"; do
  [[ -f $file && ! -L $file && $(stat -c %u "$file") == 0 && $(stat -c %a "$file") == 600 ]] || {
    printf 'Rollback snapshot metadata is missing or unsafe.\n' >&2; exit 1;
  }
done
[[ -f $state && ! -L $state && $(stat -c %u "$state") == 0 ]] || { printf 'The current workflow release record is missing or unsafe.\n' >&2; exit 1; }

python3 - "$deployment" "$backup" "$state" "$commit" "$run_id" <<'PY'
import hashlib, json, re, sys
from pathlib import Path

root, backup, state_path = map(Path, sys.argv[1:4])
commit, run_id = sys.argv[4:6]
expected = {'Caddyfile', 'docker-compose.ingress.yml', 'docker-compose.test.yml', 'docker-compose.production.yml', '.env.shared', '.env.production'}
record = json.loads(state_path.read_text())
if record.get('commit') != commit or record.get('runId') != run_id or set(record.get('sha256', {})) != expected:
    raise SystemExit('The remote commit/run identity does not match this browser-evidence workflow.')
known = expected | {'.workflow-deployed.json'}
existed = (backup / 'existed').read_text().splitlines()
missing = (backup / 'missing').read_text().splitlines()
if len(existed) != len(set(existed)) or len(missing) != len(set(missing)) or set(existed) & set(missing) or set(existed) | set(missing) != known:
    raise SystemExit('The rollback snapshot file manifest is invalid.')
for name in existed:
    path = backup / name
    if name not in known or not path.is_file() or path.is_symlink() or path.stat().st_uid != 0:
        raise SystemExit('A rollback snapshot file is missing or unsafe.')
for name, digest in record['sha256'].items():
    current = root / name
    if current.exists():
        if current.is_symlink():
            raise SystemExit('A deployed configuration file is unsafe.')
        current_digest = hashlib.sha256(current.read_bytes()).hexdigest()
        previous_digest = hashlib.sha256((backup / name).read_bytes()).hexdigest() if name in existed else None
        if current_digest != digest and current_digest != previous_digest:
            raise SystemExit('A protected deployment file changed after browser evidence capture.')
    elif name not in missing:
        raise SystemExit('A protected deployment file is unexpectedly missing.')

def services(name, service_set):
    values = (backup / name).read_text().splitlines()
    values = [value for value in values if value]
    if len(values) != len(set(values)) or any(value not in service_set for value in values):
        raise SystemExit('A rollback service manifest is invalid.')

declared = [value for value in (backup / 'declared-services').read_text().splitlines() if value]
dependencies = [value for value in (backup / 'dependencies').read_text().splitlines() if value]
if not declared or len(declared) != len(set(declared)) or any(not re.fullmatch(r'[a-z][a-z0-9-]*', value) for value in declared):
    raise SystemExit('The declared service manifest is invalid.')
if len(dependencies) != len(set(dependencies)) or not set(dependencies) <= set(declared):
    raise SystemExit('The dependency service manifest is invalid.')
services('previous-running', set(declared))
services('attempted-apps', set(declared) - set(dependencies))
if (backup / 'previous-ingress').read_text().strip() not in {'true', 'false'} or (backup / 'restore-dependencies').read_text().strip() not in {'true', 'false'}:
    raise SystemExit('A rollback state flag is invalid.')
PY

if [[ $action == finalize ]]; then
  rm -f -- "$release"
  rm -rf -- "$incoming"
  rm -rf -- "$backup"
  printf 'Production commit %s passed browser acceptance; rollback snapshot finalized.\n' "$commit"
  exit 0
fi

compose_current() { docker compose --env-file "$deployment/.env.shared" --env-file "$deployment/.env.production" -f "$deployment/docker-compose.production.yml" "$@"; }
compose_previous() { docker compose --env-file "$deployment/.env.shared" --env-file "$deployment/.env.production" -f "$deployment/docker-compose.production.yml" "$@"; }
ingress_current() { docker compose --env-file "$deployment/.env.shared" -f "$deployment/docker-compose.ingress.yml" "$@"; }
ingress_previous() { docker compose --env-file "$deployment/.env.shared" -f "$deployment/docker-compose.ingress.yml" "$@"; }

attempted_apps=()
previous_running=()
dependencies=()
while IFS= read -r service; do [[ -z $service ]] || attempted_apps+=("$service"); done < "$backup/attempted-apps"
while IFS= read -r service; do [[ -z $service ]] || previous_running+=("$service"); done < "$backup/previous-running"
while IFS= read -r service; do [[ -z $service ]] || dependencies+=("$service"); done < "$backup/dependencies"
previous_ingress=$(<"$backup/previous-ingress")
restore_dependencies=$(<"$backup/restore-dependencies")

if ((${#attempted_apps[@]})); then compose_current stop "${attempted_apps[@]}"; fi
if [[ $restore_dependencies == true ]] && ((${#dependencies[@]})); then compose_current stop "${dependencies[@]}"; fi
if [[ $previous_ingress == false ]]; then ingress_current stop caddy; fi

while IFS= read -r file; do [[ $file == .workflow-deployed.json ]] || cp -a -- "$backup/$file" "$deployment/$file"; done < "$backup/existed"
while IFS= read -r file; do [[ $file == .workflow-deployed.json ]] || rm -f -- "$deployment/$file"; done < "$backup/missing"
rm -f -- "$release"

if [[ $restore_dependencies == true ]]; then
  previously_running_dependencies=()
  for service in "${dependencies[@]}"; do
    if [[ " ${previous_running[*]} " == *" $service "* ]]; then previously_running_dependencies+=("$service"); fi
  done
  if ((${#previously_running_dependencies[@]})); then
    compose_previous up -d --wait --wait-timeout 600 --no-deps "${previously_running_dependencies[@]}"
  fi
fi

previously_affected_apps=()
for service in "${attempted_apps[@]}"; do
  if [[ " ${previous_running[*]} " == *" $service "* ]]; then previously_affected_apps+=("$service"); fi
done
if ((${#previously_affected_apps[@]})); then
  compose_previous up -d --wait --wait-timeout 600 --no-deps "${previously_affected_apps[@]}"
fi

if [[ $previous_ingress == true ]]; then
  ingress_previous up -d --wait --wait-timeout 600 --no-deps --force-recreate caddy
fi

if [[ -f $backup/.workflow-deployed.json ]]; then
  restored_state="$state.rollback-$run_id"
  install -m 0600 -- "$backup/.workflow-deployed.json" "$restored_state"
  mv -f -- "$restored_state" "$state"
else
  rm -f -- "$state"
fi

rm -rf -- "$incoming"
rm -rf -- "$backup"
printf 'Browser acceptance failed for production commit %s; the matching previous release was restored. Volumes and data were retained.\n' "$commit" >&2

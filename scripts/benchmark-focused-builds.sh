#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: scripts/benchmark-focused-builds.sh [--runs N] [--output-dir DIR] [--threshold-percent N]

Runs controlled Main host focused-build benchmarks for All, Appointments, Medical, and Custom.

Environment overrides:
  RUNS                         Number of timed no-op builds per profile (default: 3; minimum: 3)
  MATERIAL_THRESHOLD_PERCENT   Improvement threshold for the markdown summary (default: 10)
  OUTPUT_DIR                   Directory for logs and reports (default: artifacts/focused-build-benchmarks/<timestamp>)
USAGE
}

RUNS="${RUNS:-3}"
MATERIAL_THRESHOLD_PERCENT="${MATERIAL_THRESHOLD_PERCENT:-10}"
OUTPUT_DIR="${OUTPUT_DIR:-}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --runs)
      RUNS="${2:-}"
      shift 2
      ;;
    --output-dir)
      OUTPUT_DIR="${2:-}"
      shift 2
      ;;
    --threshold-percent)
      MATERIAL_THRESHOLD_PERCENT="${2:-}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if ! [[ "$RUNS" =~ ^[0-9]+$ ]] || (( RUNS < 3 )); then
  echo "RUNS must be an integer greater than or equal to 3." >&2
  exit 2
fi

if ! [[ "$MATERIAL_THRESHOLD_PERCENT" =~ ^[0-9]+([.][0-9]+)?$ ]]; then
  echo "MATERIAL_THRESHOLD_PERCENT must be numeric." >&2
  exit 2
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="src/Hosts/Main/Main.csproj"
PROJECT_FULL_PATH="$ROOT_DIR/$PROJECT_PATH"

if [[ ! -f "$PROJECT_FULL_PATH" ]]; then
  echo "Project not found: $PROJECT_FULL_PATH" >&2
  exit 1
fi

if [[ -z "$OUTPUT_DIR" ]]; then
  timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
  OUTPUT_DIR="$ROOT_DIR/artifacts/focused-build-benchmarks/$timestamp"
elif [[ "$OUTPUT_DIR" != /* ]]; then
  OUTPUT_DIR="$ROOT_DIR/$OUTPUT_DIR"
fi

mkdir -p "$OUTPUT_DIR/logs" "$OUTPUT_DIR/restore-graphs"

CSV_PATH="$OUTPUT_DIR/results.csv"
SUMMARY_PATH="$OUTPUT_DIR/summary.md"
COMMANDS_PATH="$OUTPUT_DIR/commands.txt"

cat > "$CSV_PATH" <<'CSV'
profile,run,elapsed_seconds,project_count,log_path
CSV
: > "$COMMANDS_PATH"

profiles=("All" "Appointments" "Medical" "Custom")

declare -A profile_properties
profile_properties[All]="-p:ModuleBuildProfile=All"
profile_properties[Appointments]="-p:ModuleBuildProfile=Appointments"
profile_properties[Medical]="-p:ModuleBuildProfile=Medical"
profile_properties[Custom]="-p:ModuleBuildProfile=Custom -p:EnableUsersModule=true -p:EnableAppointmentsModule=true"

run_dotnet() {
  local log_path="$1"
  shift
  (
    cd "$ROOT_DIR"
    "$@"
  ) > "$log_path" 2>&1
}

write_command() {
  local label="$1"
  shift
  printf '%s:' "$label" >> "$COMMANDS_PATH"
  printf ' %q' "$@" >> "$COMMANDS_PATH"
  printf '\n' >> "$COMMANDS_PATH"
}

elapsed_seconds() {
  local start_ns="$1"
  local end_ns="$2"
  python3 - "$start_ns" "$end_ns" <<'PY'
import sys
start = int(sys.argv[1])
end = int(sys.argv[2])
print(f"{(end - start) / 1_000_000_000:.3f}")
PY
}

profile_average() {
  local profile="$1"
  python3 - "$CSV_PATH" "$profile" <<'PY'
import csv
import sys
path, profile = sys.argv[1], sys.argv[2]
values = []
with open(path, newline='', encoding='utf-8') as handle:
    for row in csv.DictReader(handle):
        if row['profile'] == profile:
            values.append(float(row['elapsed_seconds']))
if not values:
    print('0.000')
else:
    print(f"{sum(values) / len(values):.3f}")
PY
}

profile_project_count() {
  local profile="$1"
  python3 - "$CSV_PATH" "$profile" <<'PY'
import csv
import sys
path, profile = sys.argv[1], sys.argv[2]
with open(path, newline='', encoding='utf-8') as handle:
    for row in csv.DictReader(handle):
        if row['profile'] == profile:
            print(row['project_count'])
            break
PY
}

compare_to_all() {
  local focused_average="$1"
  local all_average="$2"
  python3 - "$focused_average" "$all_average" "$MATERIAL_THRESHOLD_PERCENT" <<'PY'
import sys
focused = float(sys.argv[1])
all_average = float(sys.argv[2])
threshold = float(sys.argv[3])
if all_average <= 0:
    print('n/a,n/a')
else:
    improvement = ((all_average - focused) / all_average) * 100
    material = 'yes' if improvement >= threshold else 'no'
    print(f"{improvement:.1f}%,{material}")
PY
}

count_projects() {
  local profile="$1"
  local graph_path="$OUTPUT_DIR/restore-graphs/$profile.json"
  local graph_log="$OUTPUT_DIR/logs/$profile-restore-graph.log"
  local properties=( ${profile_properties[$profile]} )

  run_dotnet "$graph_log" dotnet msbuild "$PROJECT_PATH" -t:GenerateRestoreGraphFile "-p:RestoreGraphOutputPath=$graph_path" "${properties[@]}"

  python3 - "$graph_path" <<'PY'
import json
import sys
path = sys.argv[1]
with open(path, encoding='utf-8') as handle:
    data = json.load(handle)
projects = data.get('Projects', {})
if isinstance(projects, dict):
    print(len(projects))
elif isinstance(projects, list):
    print(len(projects))
else:
    print(0)
PY
}

echo "Focused build benchmark output: $OUTPUT_DIR"

for profile in "${profiles[@]}"; do
  properties=( ${profile_properties[$profile]} )
  build_command=(dotnet build "$PROJECT_PATH" "${properties[@]}")
  noop_command=(dotnet build "$PROJECT_PATH" "${properties[@]}" --no-restore)

  write_command "$profile warm-up" "${build_command[@]}"
  write_command "$profile timed" "${noop_command[@]}"

  echo "[$profile] Counting project graph..."
  project_count="$(count_projects "$profile")"

  echo "[$profile] Warm-up restore/build..."
  run_dotnet "$OUTPUT_DIR/logs/$profile-warmup.log" "${build_command[@]}"

  for run_number in $(seq 1 "$RUNS"); do
    log_path="$OUTPUT_DIR/logs/$profile-run-$run_number.log"
    echo "[$profile] Timed no-op build $run_number/$RUNS..."
    start_ns="$(date +%s%N)"
    run_dotnet "$log_path" "${noop_command[@]}"
    end_ns="$(date +%s%N)"
    seconds="$(elapsed_seconds "$start_ns" "$end_ns")"
    printf '%s,%s,%s,%s,%s\n' "$profile" "$run_number" "$seconds" "$project_count" "$log_path" >> "$CSV_PATH"
  done
done

all_average="$(profile_average All)"

{
  echo '# Focused build benchmark summary'
  echo
  echo "- Project: \`$PROJECT_PATH\`"
  echo "- Timed no-op builds per profile: \`$RUNS\`"
  echo "- Material improvement threshold: \`${MATERIAL_THRESHOLD_PERCENT}%\`"
  echo "- Output directory: \`$OUTPUT_DIR\`"
  echo
  echo '| Profile | Project count | Average no-op seconds | Improvement vs All | Material improvement? |'
  echo '| --- | ---: | ---: | ---: | :---: |'
  for profile in "${profiles[@]}"; do
    average="$(profile_average "$profile")"
    project_count="$(profile_project_count "$profile")"
    if [[ "$profile" == "All" ]]; then
      improvement="baseline"
      material="baseline"
    else
      comparison="$(compare_to_all "$average" "$all_average")"
      improvement="${comparison%,*}"
      material="${comparison#*,}"
    fi
    echo "| $profile | $project_count | $average | $improvement | $material |"
  done
  echo
  echo '## Commands'
  echo
  echo '```text'
  cat "$COMMANDS_PATH"
  echo '```'
  echo
  echo '## Notes'
  echo
  echo '- Each profile runs one restore/build warm-up before timed no-op builds.'
  echo '- Timed builds use `--no-restore` to keep cache state consistent after warm-up.'
  echo '- Project counts come from MSBuild restore graph generation for the same profile properties.'
  echo '- A focused profile materially improves inner-loop time only when its average no-op build time improves over `All` by at least the configured threshold.'
} > "$SUMMARY_PATH"

echo "Benchmark complete."
echo "CSV: $CSV_PATH"
echo "Summary: $SUMMARY_PATH"

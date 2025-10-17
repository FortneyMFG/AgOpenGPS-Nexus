#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <case.json> [<case.json> ...]" >&2
  exit 1
fi

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
HARNESS_ROOT=$(cd -- "${SCRIPT_DIR}/.." && pwd)

for case_file in "$@"; do
  echo "[sim-harness] TODO: execute guidance orchestrator scenario ${case_file}" >&2
  # Placeholder: orchestrator integration pending.
  if [[ ! -f "${case_file}" ]]; then
    echo "Case file ${case_file} not found" >&2
    exit 2
  fi
  # Future implementation will spawn sidecar + orchestrator and record metrics.
  jq '.' "${case_file}" >/dev/null
  echo "[sim-harness] Validated JSON for ${case_file}" >&2
done

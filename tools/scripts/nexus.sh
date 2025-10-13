#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: nexus.sh <command> [options]

Commands:
  run <target> [-- <args>...]   Run a Nexus host (core, agio, ui) via dotnet run.
  sim [-- <args>...]            Launch the composite simulation host.
  help                          Show this help text.

Environment overrides:
  NEXUS_CORE_PROJECT  Relative path to the Core host .csproj.
  NEXUS_AGIO_PROJECT  Relative path to the AgIO host .csproj.
  NEXUS_UI_PROJECT    Relative path to the UI .csproj.
  NEXUS_SIM_PROJECT   Relative path to the simulation entry .csproj.
  DOTNET              dotnet executable to invoke (default: dotnet).
USAGE
}

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

declare -A defaults=(
  [core]="Nexus Source Code/src/Aog.Core.Host/Aog.Core.Host.csproj"
  [agio]="Nexus Source Code/src/Aog.Agio.Host/Aog.Agio.Host.csproj"
  [ui]="Nexus Source Code/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj"
  [sim]="Nexus Source Code/src/Aog.Core.SimHost/Aog.Core.SimHost.csproj"
)

resolve_project() {
  local target="$1"
  local env_var="NEXUS_${target^^}_PROJECT"
  local override="${!env_var-}"
  if [[ -n "${override}" ]]; then
    printf '%s\n' "${override}"
  else
    printf '%s\n' "${defaults[$target]}"
  fi
}

require_project() {
  local target="$1"
  local project_rel
  project_rel="$(resolve_project "${target}")"
  local project_path="${repo_root}/${project_rel}"
  if [[ ! -f "${project_path}" ]]; then
    >&2 printf 'error: expected project for "%s" at %s\n' "${target}" "${project_path}"
    >&2 printf '       override via %s environment variable if needed.\n' "NEXUS_${target^^}_PROJECT"
    exit 1
  fi
  printf '%s\n' "${project_path}"
}

ensure_dotnet() {
  local dotnet_cmd
  dotnet_cmd="${DOTNET:-dotnet}"
  if ! command -v "${dotnet_cmd}" >/dev/null 2>&1; then
    >&2 printf 'error: dotnet CLI not found (DOTNET=%s).\n' "${dotnet_cmd}"
    exit 1
  fi
}

run_target() {
  local target="$1"
  shift || true
  ensure_dotnet
  local project_path
  project_path="$(require_project "${target}")"
  local dotnet_cmd="${DOTNET:-dotnet}"
  if [[ $# -gt 0 ]]; then
    "${dotnet_cmd}" run --project "${project_path}" -- "$@"
  else
    "${dotnet_cmd}" run --project "${project_path}"
  fi
}

if [[ $# -lt 1 ]]; then
  usage
  exit 1
fi

command="$1"
shift || true

case "${command}" in
  run)
    if [[ $# -lt 1 ]]; then
      >&2 printf 'error: missing target for run command.\n'
      usage
      exit 1
    fi
    target="$1"
    shift || true
    case "${target}" in
      core|agio|ui)
        run_target "${target}" "$@"
        ;;
      *)
        >&2 printf 'error: unknown run target "%s". Expected core, agio, or ui.\n' "${target}"
        exit 1
        ;;
    esac
    ;;
  sim)
    run_target "sim" "$@"
    ;;
  help|-h|--help)
    usage
    ;;
  *)
    >&2 printf 'error: unknown command "%s".\n' "${command}"
    usage
    exit 1
    ;;
 esac

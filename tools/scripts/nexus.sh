#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: nexus.sh <command> [options]

Commands:
  run <target> [-- <args>...]   Run a Nexus host (core, agio, ui) via dotnet run.
  sim [-- <args>...]            Launch the composite simulation host.
  plugin <cmd> [options]        Plugin manifest tooling (lint, capabilities).
  guardrails [-- <args>...]     Run guardrail regression tests (retention, replay, crash).
  help                          Show this help text.

Environment overrides:
  NEXUS_CORE_PROJECT  Relative path to the Core host .csproj.
  NEXUS_AGIO_PROJECT  Relative path to the AgIO host .csproj.
  NEXUS_UI_PROJECT    Relative path to the UI .csproj.
  NEXUS_SIM_PROJECT   Relative path to the simulation entry .csproj.
  NEXUS_PLUGIN_TOOL_PROJECT
                       Relative path to the plugin compliance tool .csproj.
  DOTNET              dotnet executable to invoke (default: dotnet).
USAGE
}

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

declare -A defaults=(
  [core]="Nexus SourceCode/src/Aog.Core.Host/Aog.Core.Host.csproj"
  [agio]="Nexus SourceCode/src/Aog.Agio/Aog.Agio.csproj"
  [ui]="Nexus SourceCode/src/Aog.UI.Avalonia/Aog.UI.Avalonia.csproj"
  # Simulation work shares the Core host entry point until a dedicated SimHost
  # project lands. Override via NEXUS_SIM_PROJECT when that host exists.
  [sim]="Nexus SourceCode/src/Aog.Core.Host/Aog.Core.Host.csproj"
)

plugin_tool_default="Nexus SourceCode/tools/Aog.Tools.PluginCompliance/Aog.Tools.PluginCompliance.csproj"

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

run_guardrails() {
  ensure_dotnet
  local dotnet_cmd="${DOTNET:-dotnet}"
  local solution_path="${repo_root}/Nexus SourceCode/Nexus.sln"
  if [[ ! -f "${solution_path}" ]]; then
    >&2 printf 'error: expected solution at %s\n' "${solution_path}"
    exit 1
  fi

  local args=("test" "${solution_path}" "--filter" "Category=Guardrail")
  if [[ $# -gt 0 ]]; then
    args+=("$@")
  fi

  "${dotnet_cmd}" "${args[@]}"
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
  plugin)
    ensure_dotnet
    local project_rel="${NEXUS_PLUGIN_TOOL_PROJECT:-$plugin_tool_default}"
    local project_path="${repo_root}/${project_rel}"
    if [[ ! -f "${project_path}" ]]; then
      >&2 printf 'error: expected plugin tool at %s\n' "${project_path}"
      >&2 printf '       override via NEXUS_PLUGIN_TOOL_PROJECT.\n'
      exit 1
    fi
    local dotnet_cmd="${DOTNET:-dotnet}"
    if [[ $# -gt 0 ]]; then
      "${dotnet_cmd}" run --project "${project_path}" -- "$@"
    else
      "${dotnet_cmd}" run --project "${project_path}" -- help
    fi
    ;;
  guardrails)
    run_guardrails "$@"
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

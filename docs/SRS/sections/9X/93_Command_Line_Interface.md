# 93 — Command Line Interface (Status: collecting proposals)

## Problem statement
Operators, integrators, and automation pipelines need a consistent command line
experience to manage Nexus Core, inspect plugin health, and automate
installations across Windows and Linux targets. The current toolchain relies on
bespoke scripts or plugin-specific utilities, creating fragmentation and
hindering offline workflows. A unified CLI must bridge local configuration,
remote Core instances, and plugin-provided verbs without duplicating business
logic or violating manifest governance established elsewhere in the SRS.

## Requirements (from contributors)
- R-CLI-000 (MUST, devex consolidation): Ship a single `nx` host binary that
  discovers plugin verbs at startup so users obtain one installer, one help
  surface, and consistent telemetry hooks.
- R-CLI-001 (MUST, offline readiness): Support filesystem-driven commands when
  Core is not running, covering plugin install/remove, config edits, and profile
  export/import scenarios.
- R-CLI-002 (MUST, live orchestration): Negotiate connections to a running Core
  process via named pipes on Windows, Unix domain sockets on Linux, and a TLS
  TCP fallback for remote access, with `--endpoint` overrides for advanced
  routing.
- R-CLI-003 (MUST, plugin integration): Load optional
  `Nexus.Plugin.Cli.Abstractions` adapters shipped beside plugins and/or query
  plugin CLI reflection gRPC services so plugins can contribute verbs without
  duplicating host logic.
- R-CLI-004 (SHOULD, parsing & UX): Use `System.CommandLine` for argument
  parsing, help, and shell completions, plus `Spectre.Console` for TTY rendering
  (tables, spinners) while falling back to plain text in non-TTY contexts.
- R-CLI-005 (MUST, scripting): Provide structured output modes (`human`,
  `--json`, `--ndjson`) with stable DTO schemas so automation and observability
  tooling can rely on the CLI without brittle parsing.
- R-CLI-006 (SHOULD, configuration discovery): Resolve configuration inputs in
  priority order (`~/.nexus/config.yml`, repository `.nexus/`, environment
  variables) to stay aligned with 12-factor expectations and existing manifests.
- R-CLI-007 (SHOULD, auth): Read remote authentication tokens from
  `~/.nexus/credentials` and surface permission errors with remediation hints.
- R-CLI-008 (MUST, version negotiation): Enforce semantic version compatibility
  between CLI ↔ Core APIs and plugin-declared verb contracts, surfacing clear
  upgrade paths when mismatches occur.
- R-CLI-009 (SHOULD, diagnostics): Offer diagnostics verbs (`nx core status`,
  `nx diag dump`, `nx events tail`) that gather logs, manifests, and bus events
  for support workflows, including streaming output where appropriate.
- R-CLI-010 (SHOULD, packaging): Build and distribute the CLI as a .NET 8 global
  tool and as self-contained single-file binaries for Windows (x64/arm64) and
  Linux (x64/arm64, including CM5/Pi) with optional macOS arm64 support when
  available.

## Options
- O-CLI-0: Unified `nx` host with plugin-discovered verbs, offline filesystem
  workflows, and Core-connected live mode (recommended design in this brief).
- O-CLI-1: Per-plugin CLIs maintained independently, each re-implementing
  transport discovery and manifest wiring.
- O-CLI-2: Script-only tooling with no supported CLI host.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
| --- | --- | --- | --- | --- |
| O-CLI-0 | Unified UX, shared transport logic, consistent telemetry | Requires plugin adapters or reflection contracts | Host must guard against plugin incompatibilities | Existing manifests, ADR-031 governance, System.CommandLine patterns |
| O-CLI-1 | Plugin teams ship at their own cadence | Fragmented UX, duplicated plumbing, harder compatibility enforcement | Divergent JSON schemas and auth models | Legacy single-purpose scripts |
| O-CLI-2 | Zero new tooling investment | Operators lack supported automation path | Unsupported scripts block rollout of new governance | None |

## Evaluation criteria
Cross-platform reach, plugin onboarding effort, observability coverage,
compatibility governance, and automation friendliness.

## Current sentiment
- Packaging and manifest governance from Section 16 position us to load plugin
  adapters safely without bypassing capability checks.
- Transport negotiation should reuse the endpoint locator logic already required
  for Core ↔ UI processes (Sections 03 & 07) while surfacing CLI-specific
  telemetry.
- A single host strengthens docs, support playbooks, and training materials
  compared to proliferating per-plugin executables.

## Upcoming ADR coverage
- **ADR-054 Nexus CLI host** will formalize the unified host, transport
  negotiation, plugin adapter expectations, and packaging plan to satisfy
  R-CLI-000 through R-CLI-010. (TBD)

## Open questions
- Which initial verbs and DTOs constitute the minimal viable CLI surface for the
  first pilot release?
- How will plugin reflection services authenticate/authorize remote CLI
  invocations when Core hosts additional security policies?

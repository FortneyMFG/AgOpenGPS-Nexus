# ADR-054: Nexus CLI Host and Plugin Verb Architecture

## Status
Proposed

**Relevant Plugin(s):** Full Stack

## Context
Plugin authors and operators rely on automation to install packages, inspect
Core health, and run diagnostics across Windows and Linux rigs. Existing tooling
is fragmented across bespoke scripts and plugin-specific binaries, making it
hard to enforce manifest governance from ADR-031, align with the transport
contracts in Sections 03 and 07, or deliver consistent packaging experiences
from Section 16. Section 18 of the SRS now captures explicit requirements for a
unified CLI host (R-CLI-000…R-CLI-010), including offline workflows, transport
negotiation, plugin-supplied verbs, structured output, and multi-RID
distribution.

## Decision
Ship a single `.NET 8` CLI host named `nx` that boots plugin verbs dynamically
and operates in offline or live modes:

- Discover installed plugins via the manifest registry and load optional
  `Nexus.Plugin.Cli.Abstractions` adapters (`ICommandHandler`) located beside each
  plugin. When a plugin lacks a local adapter, query its Core-hosted CLI
  reflection gRPC service to surface verbs and argument metadata.
- Resolve Core endpoints automatically by checking for a running Core process on
  Windows named pipes, Linux Unix domain sockets, then falling back to a TLS TCP
  endpoint (`127.0.0.1:<port>` by default) unless `--endpoint` overrides are
  supplied.
- Support offline commands that operate on manifests, config files, schema
  caches, and plugin archives so users can manage deployments without Core
  running.
- Standardize UX with `System.CommandLine` (parsing, help, completions) and
  `Spectre.Console` for rich TTY output while retaining plain text rendering for
  non-TTY contexts.
- Emit structured responses via a shared serialization layer: human-readable by
  default, `--json` for deterministic objects, and `--ndjson` for streaming
  feeds like event tails.
- Package the CLI as both a dotnet global tool and self-contained single-file
  binaries for Windows (x64/arm64) and Linux (x64/arm64, including CM5/Pi), with
  optional macOS arm64 builds when CI capacity permits.
- Enforce semantic version compatibility between CLI ↔ Core APIs and plugin
  verbs, surfacing actionable remediation (`nx self update`, `nx core update`).

## Consequences
- Positive impacts
  - Operators gain a single automation surface for installs, diagnostics, and
    scripting across supported platforms.
  - Plugin teams reuse Core manifests and dependency checks instead of
    duplicating transport and auth plumbing in standalone CLIs.
  - Support workflows benefit from consistent diagnostics (`nx core status`,
    `nx diag dump`, `nx events tail`) with structured outputs suitable for logs
    and bug reports.
- Negative/mitigated impacts
  - The host must sandbox plugin-provided verbs to prevent version skew or
    missing dependencies; manifest capability checks and semver ranges mitigate
    this risk.
  - Maintaining multi-RID single-file builds increases CI time, but reuse of the
    existing packaging infrastructure from Section 16 minimizes incremental
    effort.
- Follow-up actions
  - Publish `Nexus.Plugin.Cli.Abstractions` with dependency injection helpers and
    guidance for adapter authors.
  - Implement the endpoint resolver, health probes, and initial verb set (`nx
    core status`, `nx plugin list`, `nx config get/set`).
  - Document CLI installation, completions, and JSON output schemas under
    `docs/cli/` and update plugin author guides with adapter expectations.

## Legacy Implementation Notes
### AgOpenGPS v6
No unified CLI existed. Operators relied on PowerShell scripts, batch files, or
manual configuration to manage plugins and settings, limiting automation.

### Legacy Dev Branch
The dev branch contains ad-hoc tooling for manifest validation and telemetry but
lacks a cohesive CLI host. Each plugin team maintained bespoke utilities without
shared transport logic or version negotiation.

## References
- [SRS Section 18 – Command Line Interface](../SRS/sections/18_Command_Line_Interface.md)
- [SRS Section 16 – Plugin Packaging, Updates, and Catalog](../SRS/sections/16_Plugin_Packaging_Updates.md)
- [ADR-031 – Official plugin bundle](ADR-031-official-plugin-bundle.md)
- [ADR-028 – Stack boundaries](ADR-028-stack-boundaries.md)

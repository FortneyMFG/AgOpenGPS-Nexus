# CLI Extensions for Nexus Plugins (Draft)

Plugins can now publish CLI verbs that run under the shared `nx` host described
in ADR-054 and SRS Section 18. This guide highlights how plugin teams surface
commands without duplicating transport or manifest plumbing.

## Install the CLI adapter SDK

Reference the published abstractions so your project builds against the same
contracts as the `nx` host. The package is available from the local repository
feed (`./artifacts/nuget`) or GitHub Packages.

```bash
dotnet add package AgOpenGPS.Nexus.Plugin.Cli.Abstractions --version 0.1.*
```

The package ships XML documentation and nullable-enabled APIs for
`System.CommandLine` integration. See the README inside the package for local
packaging instructions.

## Adapter vs. reflection contributions
- **Adapter assemblies (`*.Cli.dll`)** implement `Nexus.Plugin.Cli.Abstractions`.
  Drop the assembly beside your plugin package and export one or more
  `ICommandModule` types. The host will instantiate each module via dependency
  injection and mount its commands under `nx <plugin-id> …` when the plugin is
  installed locally.【F:docs/ADR/ADR-054_NexusCliHost.md†L20-L56】
- **Reflection services** let remote-only plugins expose verbs without shipping a
  local adapter. Implement the CLI reflection gRPC contract published by the
  Core team so the host can enumerate verbs and parameters dynamically. Use this
  path when your plugin runs out-of-process or must delegate execution to Core
  services.【F:docs/SRS/sections/18_Command_Line_Interface.md†L24-L48】

## Command design expectations
- **Output modes:** Support human-readable, `--json`, and `--ndjson` output to
  match the host’s scripting guarantees. JSON payloads should use the same DTOs
  your plugin already publishes via gRPC or manifest schemas to keep automation
  stable.【F:docs/SRS/sections/18_Command_Line_Interface.md†L31-L38】
- **Config discovery:** Respect the standard lookup order (`~/.nexus/`, repo
  `.nexus/`, environment variables) so CLI and UI edits remain consistent.
- **Version gating:** Declare verb compatibility requirements in your plugin
  manifest. The host will block incompatible versions and surface remediation
  hints during `nx plugin list` and command execution.【F:docs/ADR/ADR-054_NexusCliHost.md†L36-L56】

## Packaging & testing
- Ship adapter binaries as part of your plugin archive so single-file CLI builds
  can load them without extra installers. Follow the same RID folder structure
  the plugin manifest already uses for runtime assets.【F:docs/SRS/sections/16_Plugin_Packaging_Updates.md†L1-L60】
- Cover CLI verbs with deterministic tests that run via `nx --json` inside the
  plugin’s CI workflow. Include fixtures for offline mode where the command reads
  manifests directly.

## Next steps
- Track ADR-054 progress in the ADR roadmap for exact transport and reflection
  schemas.
- Coordinate with the DevEx pod before introducing breaking changes to existing
  CLI verbs so ADR updates and migration guides can land simultaneously.

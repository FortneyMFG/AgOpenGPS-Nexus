# Plugin Manifest Compliance & Capability Reporting

The ADR-031 governance work introduces a dedicated tool for validating Nexus plugin
manifests and exporting the capability data consumed by the Device Manager
compatibility dashboard. Use the commands below to run the checks locally and to
publish capability snapshots for dashboards or diagnostic tooling.

## Lint manifests against recorded baselines

```
./tools/scripts/nexus.sh plugin lint
```

The lint command performs the same checks enforced by CI:

- Ensures every manifest under `docs/development/SRS/appendices/samples/plugins/` parses via the shared
  loader and matches its canonical baseline in
  `Nexus SourceCode/tests/Aog.Plugins.Tests/Compatibility/Baselines/`.
- Verifies that each `supportedCapabilities` entry has a corresponding lease
declaration.
- Flags baseline snapshots that no longer have a manifest so stale fixtures can
be removed.

Use `--manifests` or `--baselines` to override the default roots when testing
feature branches or external plugins:

```
./tools/scripts/nexus.sh plugin lint --manifests ../my-plugin/manifests --baselines ../my-plugin/baselines
```

The PowerShell equivalent (`./tools/scripts/nexus.ps1 plugin lint ...`) accepts
the same switches on Windows hosts.

## Export capability and lease data

```
./tools/scripts/nexus.sh plugin capabilities --format json > capabilities.json
```

This command emits a normalized list of plugin capabilities with their lease
modes, timeout budgets, and recovery strategies. The JSON output is designed to
feed Device Manager dashboards and governance checks. Supported options:

- `--plugin <id|name>` filters the report to a single plugin.
- `--format text` renders a human-readable summary instead of JSON.
- `--output <path>` writes the report to disk.

The defaults scan the repository manifests; override `--manifests` to analyse
external bundles.

## Environment overrides

All commands honour the `NEXUS_PLUGIN_TOOL_PROJECT` environment variable if you
need to point the wrapper scripts at an alternate project location. The wrapper
still accepts `DOTNET` overrides when multiple SDK versions are installed.

Keeping these checks in your local workflow helps ensure manifest changes pass
the CI gate introduced with NX-284 and surface capability updates for NX-283's
Device Manager work.

# Nexus Plugin Documentation Hub

Use this page as the launch pad for all plugin-related documentation. It links to the architectural references, SDK integration guides, and the catalogue of official plugins that ship with Nexus.

## Architecture & SDK
- [Zip Plugin Architecture](architecture.md) — Authoritative description of the zip packaging model, manifest schema, and runtime lifecycle.
- [Core Integration Guide](../../Nexus SourceCode/src/Aog.Core/PLUGINS.md) — Host services and capability leases exposed by the core runtime.
- [UI Integration Guide](../../Nexus SourceCode/src/Aog.UI.Avalonia/PLUGINS.md) — Blocks, windows, tools, and map host extension points.
- [Plugin Manifest Reference](../Core/reference/plugin-manifest.md)
- [Security Guidelines](security.md) and [Performance Budgets](performance.md)

## Official Plugin Cards
The official plugins maintained with Nexus each have a concise reference card:

- Browse the catalogue: [docs/Plugins/official/](official/README.md)
- Quick links: [AutoSteer](official/AutoSteer.md) · [Sections Control](official/Sections.md) · [Telemetry Logging](official/TelemetryLogging.md) · [Variable Mapping](official/VariableMapping.md)

## Legacy Briefs & Deep Dives
Historical briefs predating the zip architecture remain available for context. They are gradually being refreshed to align with the new packaging model.

- [Pumpkin Pi](pumpkin-pi.md) — CM5-local HAL + shared-memory fast path.
- [Isobus Bridge](IsobusBridge.md)
- [Device Manager](DeviceManager.md)
- [Automation Engine](AutomationEngine.md)
- [Mapping](Mapping.md), [Telemetry Logging](TelemetryLogging.md), [Replay](Replay.md)
- [CLI Extensions](CLIExtensions.md) and other tooling docs.

> When updating an official plugin, refresh both the new card under `official/` and any legacy brief that still contains relevant history.

Refer to `docs/INDEX.md` for the complete documentation map, ADRs, and rollout plans.

# Compatibility Evaluator Plugin

## Overview
The Compatibility plugin analyses installed plugin manifests, resolves dependency graphs, and highlights conflicts before activation. It powers the Plugin Manager dashboard and ensures that operators understand why a plugin cannot be enabled.

## Capabilities
- `diagnostics.compatibility` shared capability for surfacing bundle health.
- Provides metadata consumed by the status strip and Device Manager dashboards.

## Core Integration
- `PluginCompatibilityEvaluator.cs` computes dependency, capability, and lease conflicts using `PluginManifest` metadata.
- `CompatibilityEnvironment.cs` models the host environment (SDK versions, available transports, hardware flags) so evaluations match the current runtime.
- Reports are represented by `PluginCompatibilityReport.cs` and `PluginCompatibilityResult.cs`, which the UI converts into user-facing summaries.
- Unit tests in `tests/Aog.Tools.PluginCompliance.Tests` validate edge cases such as circular dependencies and missing capabilities.

## UI Integration
- `DeviceManagerCompatibilityViewModel` (`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/DeviceManagerCompatibilityViewModel.cs`) consumes evaluation results to render the dashboard card.
- When packaged as a zip, the UI entry point registers the dashboard provider and status strip indicators that show blocked/warning counts.

## Dependencies
- Depends on `PluginManifest` models shared in `Aog.Plugins`.
- Requires access to the plugin registry store maintained by the core host.

## Packaging Notes
- Manifest should request permissions for reading the plugin registry and, if exposed via CLI, list `cli.module` capability.
- Bundle sample manifest fixtures under `assets/snapshots/` for automated diagnostics or offline analysis.

## Related Resources
- `docs/plugins/architecture.md` for manifest structure.
- `docs/plugins/official/Sections.md` and other cards reference compatibility implications.
- ADR covering plugin dependency governance (once published) will document policy decisions enforced here.


# Appendix — Nexus Plugin Architecture

This appendix captures the authoritative "this is how Nexus does plugins" description for the zip-based plugin ecosystem. It
complements the normative requirements in [SRS Section 12 – Extensibility & Plugins](../sections/12_Extensibility_Plugins.md)
and consolidates the SDK, packaging, runtime, and governance expectations into a single reference for host maintainers,
plugin authors, and quality teams.

## Purpose & Scope

- **Audience:** Core Nexus maintainers, plugin developers (first-party and third-party), documentation and release engineers.
- **Scope:** SDK surface area, plugin package format, lifecycle management in both the UI host and Core process, dependency and
  version rules, observability, and compliance gates that ensure plugins remain installable, upgradeable, and supportable.

## Architectural Overview

Nexus operates as a host shell plus a collection of optional zip-packaged plugins. Each plugin is an independently deployable
unit that contributes UI surfaces, domain services, or AgIO sidecars. The host exposes a stable SDK (delivered as NuGet
packages), enforces manifest contracts, and manages plugin discovery, activation, and teardown using collectible
`AssemblyLoadContext` instances.

The plugin ecosystem rests on three pillars:

1. **SDK Contracts** — Shared assemblies containing interfaces only. They avoid heavy dependencies so plugin binaries remain
   lightweight and versionable.
2. **Manifest & Packaging Tooling** — A JSON manifest describes entrypoints, capabilities, assets, and dependency constraints.
   An MSBuild target produces a deterministic `plugin.zip` artifact with schema validation and signing hooks.
3. **Runtime Hosts** — The Avalonia UI shell and Aog.Core service layer load, activate, monitor, and unload plugins while
   keeping layout state, telemetry, and resource usage bounded.

## SDK Composition

Publish three NuGet packages under the `Nexus.Sdk.*` umbrella. They are built from the repository and versioned together to
prevent interface skew.

| Package | Purpose | Key Interfaces |
| --- | --- | --- |
| `Nexus.Sdk.Core` | Core service contracts and lifecycle wiring. | `IPluginEntrypoint`, `ICoreEntrypoint`, `IHostServices`, `IEventBus`, `ICommandBus`, `ISettingsStore`, `ITelemetry` |
| `Nexus.Sdk.UI.Avalonia` | UI extension points for the desktop shell. | `IWindowProvider`, `IBlockProvider`, `IToolProvider`, `ILayerProvider`, `IMapHost`, `IAssetLocator` |
| `Nexus.Sdk.AgIo` | DTOs and supervisors for AgIO sidecars. | `IAgIoProcessSupervisor`, gRPC contracts |

SDK guidelines:

- Interfaces must remain dependency-light and avoid pulling host-only implementations into plugin contexts.
- `IPluginEntrypoint.Initialize(IHostServices services)` boots the plugin; `ShutdownAsync` reverses registrations and frees
  resources. Collectible load contexts demand that plugins detach static handlers and dispose managed/unmanaged objects.
- `IAssetLocator` resolves `avares://<pluginId>/...` and `/assets/...` URIs inside the packaged zip.
- `SdkVersion.Current` exposes the host SDK version and is compared against plugin manifest `sdkVersion` ranges.

## Plugin Package & Manifest

Each plugin ships as a schema-validated zip produced by `PackPlugin.targets`.

```
plugin.zip
├── manifest.json
├── lib/*.dll
├── assets/**/*
└── native/**/*
```

Key manifest fields:

```json
{
  "id": "fe.example",
  "name": "Example Plugin",
  "version": "1.0.0",
  "sdkVersion": ">=1.0.0 <2.0.0",
  "requires": {},
  "entrypoints": { "core": null, "ui": "Fe.Example.UiPlugin", "agio": null },
  "capabilities": ["window", "blocks", "layers", "tools"],
  "assets": { "icon": "assets/icon.png" },
  "update": { "feed": null },
  "permissions": { "network": true, "serial": false }
}
```

Packaging requirements:

- `PackPlugin.targets` takes the build output, manifest path, and asset globs, producing a deterministic zip.
- The build fails on schema validation errors or missing manifest entries.
- Optional signing hooks run post-pack; unsigned zips are permitted for development environments.
- Generated JSON Schema lives under `docs/Plugins/schema/plugin.schema.json` and is refreshed during CI.

## Runtime Hosts & Lifecycle Management

### Discovery

- On startup and via a background file watcher, the UI host scans `%APPDATA%/Nexus/plugins` and the bundled `../../../Plugins`
  directory.
- Dropping a `.zip` into `%APPDATA%/Nexus/plugins/inbox` expands it to `plugins/<id>/<version>/` and triggers validation.
- Manifests with duplicate IDs, invalid SemVer, unmet `requires` clauses, or unsupported `sdkVersion` ranges are rejected with
  actionable error messages shown in the Plugin Manager panel.

### Activation

- Each plugin loads in its own collectible `AssemblyLoadContext`, sharing only the SDK assemblies from the host.
- Types implementing `IPluginEntrypoint` are discovered via reflection. Hosts call `Initialize` once dependencies are resolved.
- UI entrypoints register windows, blocks, layers, and tools with the shell registries. Core entrypoints register services and
  message handlers. AgIO entrypoints connect to the supervisor for out-of-process management.
- Layout stores (blocks, windows, tools) react to plugin enable/disable events and hide surfaces when a plugin is unavailable.

### Deactivation & Unload

- Disabling a plugin invokes `ShutdownAsync`, unregisters contributions, and unloads the `AssemblyLoadContext`.
- Memory diagnostics verify that no managed types remain pinned; leaky plugins fail QA and CI gating tests.
- Host state persists enabled/disabled flags and active versions in `%APPDATA%/Nexus/plugins/plugins.json`.

### Versioning & Dependencies

- `requires` expresses plugin-to-plugin dependencies using SemVer ranges. Hosts topologically sort activations and surface clear
  failure states when requirements are unmet.
- Side-by-side versions are supported. Users can switch the active version, which reloads the plugin in a fresh load context.
- SDK compatibility is enforced via the `sdkVersion` range. CI publishes prerelease SDK packages for the `sdk-*` branch/tag
  family.

## Host Responsibilities

| Area | Responsibilities |
| --- | --- |
| UI Host (`Aog.UI.Avalonia`) | Plugin discovery, management panel UI, registry exposure for windows/blocks/tools/layers, drag-and-drop installation, enable/disable toggles, asset resolution, layout persistence. |
| Core Host (`Aog.Core`) | Central plugin inventory, shared enable/disable state, install/uninstall orchestration, IPC bridges to UI, telemetry logging, dependency graph evaluation. |
| AgIO Supervisor | Reserved for future IO sidecars; allocates transport endpoints and supervises external processes per plugin. |

## Observability & Safety

- Telemetry records load/unload events, initialization failures, manifest validation errors, and `AssemblyLoadContext` unload
  outcomes.
- Automated tests exercise load → disable → unload → reload cycles to guarantee determinism and bounded resource usage.
- A canary plugin intentionally leaks resources to ensure the unload watchdog trips and fails CI if regressions appear.
- Permissions declared in the manifest gate access to network, serial, and hardware resources via host policy enforcement.

## Documentation & Automation

- ADRs documenting architecture, extension points, and packaging (e.g., ADR-0xx series) stay synchronized with this appendix.
- `docs/Plugins/development.md` provides author guidance, manifest field explanations, and DI best practices.
- CI builds the SDK projects, regenerates the manifest schema, validates every committed `manifest.json`, and publishes
  prerelease SDK packages on `sdk-*` tags. Release pipelines attach `plugin.zip` artifacts.

## Example Plugins & Quick Start

- `Nexus SourceCode/plugins/examples/fe.hello/` demonstrates a minimal UI plugin contributing a block.
- Optional mapping extensions showcase layer and tool providers, ensuring rendering paths obey ES3 constraints.
- Developers can pack the sample plugin via:

  ```bash
  dotnet msbuild "Nexus SourceCode/plugins/examples/fe.hello/Fe.Hello.csproj" /t:PackPlugin /p:Configuration=Release
  ```

## Compliance Checklist

Hosts and plugins must satisfy the following to remain compliant with the Nexus plugin platform:

- SDK interfaces implemented without introducing host-only dependencies or static state leaks.
- Manifest schema validation succeeds; capability, entrypoint, and permission metadata stay current.
- Plugin Manager surfaces friendly diagnostics for dependency or version conflicts.
- Disable/uninstall flows unload the associated `AssemblyLoadContext` and remove UI contributions.
- Telemetry confirms load/unload events, and CI fails on regressions in schema generation or manifest validation.
- Documentation updates accompany architectural changes so plugin authors always have an up-to-date reference.

## Related References

- [SRS Section 12 – Extensibility & Plugins](../sections/12_Extensibility_Plugins.md)
- [ADR-028 – Nexus stack responsibilities & handoff boundaries](../sections/2X_System_Architecture/21-ADR-028 - Nexus stack responsibilities & handoff boundaries.md)
- [ADR-031 – Official plugin bundle dependency governance](../sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md)
- [ADR-018 – Plugin API capability discovery and runtime model](../sections/9X_Frontends_Ops/94-ADR-018 - Plugin API Capability Discovery and Runtime Model.md)
- [docs/Plugins/development.md](../../../Plugins/development.md)
- [docs/Plugins/architecture.md](../../../Plugins/architecture.md)

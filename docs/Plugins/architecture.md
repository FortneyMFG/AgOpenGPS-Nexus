# Nexus Zip Plugin Architecture

This document is the authoritative reference for how Nexus discovers, loads, and manages zip-based plugins. It complements the platform overview in [SRS Appendix — Nexus Plugin Architecture](../development/SRS/appendices/Nexus_Plugin_Architecture.md) and the developer quick starts under `docs/Plugins/tutorials/`.

---

## Lifecycle Overview

1. **Packaging** – A plugin author builds their assemblies against the Nexus SDKs and runs the `PackPlugin` MSBuild target. The target validates `manifest.json`, copies declared assets, and produces a `<id>-<version>.zip`.
2. **Drop / Install** – The operator (or an automated updater) copies the zip into either the bundled `./` directory beside the Nexus binaries or into the user-scoped `%APPDATA%/Nexus/plugins/inbox/`.
3. **Validation** – The host expands the archive under `%APPDATA%/Nexus/plugins/<id>/<version>/`, reads `manifest.json`, checks the `sdkVersion` range, dependency graph, and required capabilities, then records the plugin in the registry store.
4. **Activation** – When the operator enables the plugin, the host spins up a collectible `AssemblyLoadContext`, loads all `lib/*.dll` files, instantiates every exported `IPluginEntrypoint`, and hands them an `IHostServices` bridge.
5. **Contribution** – Entry points opt into core, UI, and/or AgIO integration by implementing the typed interfaces described below. The host registers the resulting providers with the appropriate subsystem (block registry, tool registry, command bus, etc.).
6. **Unload / Update** – Disabling a plugin or switching versions calls `ShutdownAsync` on the entry point, disposes contributed resources, unloads the ALC, and refreshes UI layout state.

The process is deterministic: every load/unload is idempotent, the manifest is authoritatively validated, and the host never executes plugin code outside of the sandboxed ALC.

---

## Manifest Contract

Every plugin must ship a `manifest.json` in the archive root. The JSON instance is validated against `docs/Plugins/schema/plugin.schema.json` during packaging and again by the runtime loader.

```jsonc
{
  "id": "org.agopengps.plugins.sections",
  "name": "Sections Control",
  "version": "1.1.0",
  "sdkVersion": ">=1.0.0 <2.0.0",
  "entrypoints": {
    "core": "Aog.Plugins.Sections.SectionsCoreEntrypoint",
    "ui": "Aog.Plugins.Sections.SectionsUiEntrypoint",
    "agio": null
  },
  "capabilities": [
    "blocks",
    "windows",
    "telemetry"
  ],
  "requires": [
    { "id": "org.agopengps.plugins.mapping", "range": ">=1.0.0" }
  ],
  "assets": {
    "icon": "assets/sections.png"
  },
  "permissions": {
    "network": true,
    "serial": false
  },
  "update": {
    "feed": "https://plugins.agopengps.org/sections/index.json"
  }
}
```

### Key Properties

| Field | Description |
| --- | --- |
| `id` | Reverse-DNS identifier. Used as the load key, disk directory and ALC name. |
| `sdkVersion` | SemVer range of Nexus SDK packages that the plugin supports. Incompatible versions block activation. |
| `entrypoints` | Fully qualified type names implementing `ICoreEntrypoint`, `IUiEntrypoint`, and/or `IAgIoEntrypoint`. Types must live inside `lib/*.dll`. |
| `capabilities` | Declarative hints used for UI catalogs and governance reports. |
| `requires` | Optional dependency list. The host resolves and topologically sorts plugins before activation. |
| `assets` | Key → relative path map of non-code files. The loader exposes these through `IAssetLocator`. |
| `permissions` | Signals that the plugin will request potentially sensitive host services. Used for consent surfaces. |
| `update.feed` | Optional HTTP endpoint that publishes SHA256 + URL pairs for delta updates. |

See `docs/Plugins/schema/plugin.schema.json` for the full contract.

---

## SDKs and Extensions

Plugins depend on **exactly three** Nexus SDK NuGets shipped by the host:

| Package | Purpose |
| --- | --- |
| `Nexus.Sdk.Core` | Common abstractions (`IPluginEntrypoint`, `IHostServices`, event bus contracts, capability leasing). |
| `Nexus.Sdk.UI.Avalonia` | UI extension points (window descriptors, block/tool providers, map host registration). |
| `Nexus.Sdk.AgIo` | Optional AgIO bridge (sidecar process contracts, transport registration). |

All other dependencies must be private to the plugin. The loader will unify references to the SDK assemblies by supplying the host copies to each `AssemblyLoadContext` so type identity stays consistent.

---

## Drop Locations & State

| Path | Purpose |
| --- | --- |
| `./` | Bundled plugins that ship with Nexus. Installed under the application root. |
| `%APPDATA%/Nexus/plugins/inbox/` | Watch folder. Any zip placed here is unpacked and registered. |
| `%APPDATA%/Nexus/plugins/<id>/<version>/` | Expanded plugin payload (manifest, lib, assets, native). |
| `%APPDATA%/Nexus/plugins/plugins.json` | Registry store tracking installed versions, enablement, and dependency resolution state. |

The UI Plugin Manager panel and CLI share the same registry, ensuring consistent enable/disable state across host surfaces.

---

## Entrypoints and Contribution

Entrypoints may implement multiple optional interfaces:

```csharp
public sealed class SectionsUiEntrypoint
    : IUiEntrypoint, IBlockProvider, IWindowProvider
{
    public void Initialize(IHostServices services)
    {
        services.Ui.BlockRegistry.Register(this);
        services.Ui.WindowRegistry.Register(this);
    }

    public IEnumerable<BlockDescriptor> GetBlocks(IServiceProvider sp) { ... }
    public IReadOnlyList<WindowDescriptor> GetWindows(IServiceProvider sp) { ... }
    public ValueTask ShutdownAsync(CancellationToken ct) => ValueTask.CompletedTask;
}
```

Other provider interfaces include `IToolProvider`, `ILayerProvider`, `IShellCommandHandler`, and `ISimulationProvider`. See `src/Aog.UI.Avalonia/PLUGINS.md` and `src/Aog.Core/PLUGINS.md` for subsystem specifics.

When disabled, the host calls `ShutdownAsync`, unregisters providers, disposes transient resources, and unloads the ALC. Plugin code **must** detach any timers, threads or event handlers; leaked references will prevent unload and raise a warning in the Plugin Manager.

---

## Packaging Checklist

1. Reference the Nexus SDK NuGets (`Nexus.Sdk.Core`, etc.) in the plugin project.
2. Author `manifest.json`. Validate with `dotnet msbuild /t:ValidatePluginManifest`.
3. Include assets under `assets/` or embed them as resources.
4. Run `dotnet msbuild /t:PackPlugin /p:Configuration=Release`.
5. (Optional) Sign the output zip and publish to the plugin feed specified in `manifest.json`.

CI pipelines should:

- Run unit/integration tests with `sdkVersion` compatibility stubs.
- Validate manifests against the schema.
- Produce a release note snippet for the Plugin Manager to surface.

---

## Runtime Observability

- **Logging** – The loader emits structured events for install, upgrade, enable, disable, manifest errors, dependency resolution issues, and unload success/failure. Search for source context `PluginHost`.
- **Telemetry** – `IPluginTelemetrySink` is available via `IHostServices` so plugins can report health metrics that flow into `TelemetryLogging`.
- **Crash Safety** – A crashing plugin ALC will be torn down and quarantined; the host surfaces a banner with remediation suggestions. Re-enabling replays the installation flow.

---

## Further Reading

- `src/Aog.Core/PLUGINS.md` — Core service integration.
- `src/Aog.UI.Avalonia/PLUGINS.md` — UI contribution points.
- `docs/Plugins/official/` — Reference cards for the plugins bundled with Nexus.
- `docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-029 - Mapping as a plugin with a minimal geospatial kernel in Core.md` — Architectural decision record governing the zip plugin model.
- `docs/Plugins/tutorials/first-plugin.md` — Hands-on walkthrough of building and packaging a plugin.


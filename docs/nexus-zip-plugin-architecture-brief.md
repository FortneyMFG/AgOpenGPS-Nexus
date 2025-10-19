# Migration Brief — Move Nexus to Zip-Plugin Architecture

## Goal

Refactor Nexus into a true SDK + Host + Zip Plugins model so features can ship as independent, installable, enable/disable-able plugins that contribute core services, UI windows/blocks/tools, and optionally out-of-process AgIO sidecars. Keep documentation current automatically as part of the build.

## Phase 0 — Repo & Build Plumbing (Foundations)

### 0.1 Create SDK projects (NuGet-publishable)

Add the following projects:

- `Nexus SourceCode/src/Nexus.Sdk.Core/Nexus.Sdk.Core.csproj`
- `Nexus SourceCode/src/Nexus.Sdk.UI.Avalonia/Nexus.Sdk.UI.Avalonia.csproj`
- `Nexus SourceCode/src/Nexus.Sdk.AgIo/Nexus.Sdk.AgIo.csproj`

`Nexus.Sdk.Core` contains interfaces only and stays free of heavy dependencies.

- Core contracts: `IEventBus`, `ICommandBus`, `ISettingsStore`, `ITelemetry`
- Plugin lifecycle:
  - `public interface IPluginEntrypoint { void Initialize(IHostServices s); ValueTask ShutdownAsync(CancellationToken ct = default); }`
  - `public interface ICoreEntrypoint : IPluginEntrypoint { }`
- Host services façade: `IHostServices` (DI accessors, logging, storage, paths)
- Versioning helper: `SdkVersion.Current = new Version(1, 0, 0)`

`Nexus.Sdk.UI.Avalonia` defines UI extension points:

- `public interface IWindowProvider { WindowDescriptor Create(IServiceProvider sp); }`
- `public interface IBlockProvider { IEnumerable<BlockDescriptor> GetBlocks(IServiceProvider sp); }`
- `public interface IToolProvider { IEnumerable<ToolDescriptor> GetTools(IServiceProvider sp); }`
- `public interface ILayerProvider { IEnumerable<LayerFactory> GetLayerFactories(); }`
- `public interface IMapHost { IMapSurface ActiveSurface { get; } void RegisterLayerProvider(ILayerProvider p); void RegisterToolProvider(IToolProvider p); }`
- Resource resolution: `IAssetLocator` supports `avares://pluginId/...` and `/assets/...` paths inside plugin zip bundles.

`Nexus.Sdk.AgIo` supplies gRPC contracts for IO sidecars (DTOs only) plus an `IAgIoProcessSupervisor` interface that the host core implements.

### 0.2 Introduce plugin manifest schema & MSBuild packer

Add the manifest infrastructure:

- `Nexus SourceCode/src/Nexus.Plugin.Manifest/PluginManifest.cs` — C# model and JSON Schema generation.
- `Nexus SourceCode/build/PackPlugin.targets` — MSBuild target to assemble plugin packages.

Use `manifest.json` as the authoritative source:

```json
{
  "id": "fe.example",
  "name": "Example Plugin",
  "version": "1.0.0",
  "sdkVersion": ">=1.0.0 <2.0.0",
  "requires": { },
  "entrypoints": { "core": null, "ui": "Fe.Example.UiPlugin", "agio": null },
  "capabilities": ["window", "blocks", "layers", "tools"],
  "assets": { "icon": "assets/icon.png" },
  "update": { "feed": null },
  "permissions": { "network": true, "serial": false }
}
```

`PackPlugin.targets` overview:

- Inputs: project `OutputPath`, manifest path, asset globs.
- Output: `plugin.zip` containing:
  - `/manifest.json`
  - `/lib/*.dll`
  - `/assets/**/*`
  - `/native/**/*`
- Validate schema and fail the build on mismatch.
- Provide hooks for optional signing.

**Acceptance:** `dotnet msbuild /t:PackPlugin /p:Manifest=...` emits a valid zip. JSON Schema is generated to `docs/plugins/schema/plugin.schema.json`.

## Phase 1 — Host Loader & Management (UI + Core)

### 1.1 Plugin host (UI process)

Add host infrastructure under `Nexus SourceCode/src/Aog.UI.Avalonia/Plugins/`:

- `PluginHost.cs`
- `PluginLoadContext.cs` (collectible `AssemblyLoadContext`)
- `PluginRegistry.cs` (tracks loaded contexts, entrypoints, providers)
- `PluginManagerPanel.axaml` (UI to list, install, enable/disable, update)

Behavioral requirements:

- Scan `%APPDATA%/Nexus/plugins` and bundled `./plugins` on startup plus via file watcher.
- Accept `.zip` drops and expand to `%APPDATA%/Nexus/plugins/<id>/<version>/`.
- Validate `sdkVersion`, `requires`, duplicate IDs, and SemVer rules.
- Load each plugin with its own collectible ALC.
- Discover types implementing `IPluginEntrypoint`, call `Initialize`, and register UI providers when entrypoints implement UI interfaces.
- Enable/disable by invoking `ShutdownAsync()`, removing registrations, unloading ALCs, and updating layout stores to hide UI.
- Support side-by-side versions with a pointer selecting the active version and allow switching with reload.

**Acceptance:** Dropping a minimal sample plugin zip causes its block/window to appear without restarting the app. Disabling it hides UI elements and frees the ALC (no pinned types; memory drops).

### 1.2 Core-side plugin service

Add `Nexus SourceCode/src/Aog.Core/Plugins/PluginService.cs` to manage shared inventory, install/uninstall state, and persisted enable/disable flags. Expose the service to the UI (via IPC or DI).

**Acceptance:** UI and CLI show the same plugin list and status.

## Phase 2 — UI Composition (Remove hard-codes)

### 2.1 Replace hard-wired registrations

- Update `Aog.UI.Avalonia/Hosting/ServiceCollectionExtensions.cs` to remove baked provider lists and instead register `BlockRegistry`, `WindowRegistry`, `ToolRegistry`, and `LayerRegistry`.
- Update `BlockLayoutStore` to persist and display only blocks/windows that exist, dropping missing entries when a plugin is disabled or unloaded.

**Acceptance:** With no plugins, only the shell UI loads. Installing a plugin contributing blocks/windows exposes them immediately.

### 2.2 Map host service (Mapping/Variable Mapping readiness)

- Add `Aog.UI.Avalonia/Map/MapHost.cs` implementing `IMapHost` and a basic `IMapSurface` using Avalonia/Skia.
- Expose `IMapHost` via DI to allow plugins to register tools and layers without owning windows.

**Acceptance:** Plugins can resolve `IMapHost` to register tools/layers, assuming a Map window plugin provides the surface view.

## Phase 3 — Safety, Versioning, Dependencies

### 3.1 SemVer gates & dependency resolution

- Implement dependency graph resolution for `requires` (plugin ID → version range).
- Topologically order activation and fail fast with user-friendly errors shown in the Plugin Manager when dependencies are missing or incompatible.

### 3.2 Lifecycle & unload hygiene

- Enforce that `ShutdownAsync` detaches static events, timers, threads, and subscriptions.
- Ship a test plugin that intentionally leaks and assert that ALC unload fails; flag the regression in tests.

**Acceptance:** Automated tests cover load/disable/unload/reload cycles while maintaining bounded memory usage.

## Phase 4 — AgIO Sidecars (Optional in this PR)

- Add `Aog.Core/AgIo/AgIoSupervisor.cs` scaffolding to launch sidecar processes per IO plugin in the future.
- Reserve IPC addresses (UDS/NamedPipe) per plugin ID.

**Acceptance:** Hooks exist even if sidecar implementation ships later.

## Phase 5 — Docs & Automation

### 5.1 Architecture decision records (ADRs)

Create:

- `docs/adr/ADR-0xx-plugin-architecture.md` — Zip plugin architecture rationale (status: Accepted).
- `docs/adr/ADR-0xy-ui-extension-points.md` — Windows, blocks, tools, layers, and map host extension points.
- `docs/adr/ADR-0xz-plugin-packaging.md` — Plugin packaging and signing (outline for future signing work).

Document motivations, SDK composition, manifest governance, lifecycle, and migration plan from in-tree features.

### 5.2 Developer guide

- `docs/plugins/development.md` — Author plugins using the SDK, configure local debug folders, describe manifest fields, and list DI do/don’t guidance.
- `docs/plugins/schema/plugin.schema.json` — Generated during build.

### 5.3 Continuous documentation enforcement

Add a GitHub Actions job that:

- Builds SDK projects.
- Generates JSON Schema from `PluginManifest` (source generator) to `docs/plugins/schema`.
- Validates every `plugins/**/manifest.json` against the schema.
- Publishes prerelease SDK packages on `sdk-*` tags.
- Attaches any produced `plugin.zip` artifacts to releases.

**Acceptance:** Manifest-breaking changes fail CI with clear diagnostics.

## Phase 6 — Example Plugins (Smoke Test)

### 6.1 Minimal example UI plugin (in-tree)

Add `Nexus SourceCode/plugins/examples/fe.hello/Fe.Hello.csproj` with `manifest.json` and `assets/icon.png`. Contribute a single block (`HelloBlockProvider`) containing a simple label.

**Acceptance:** Packing and dropping the plugin zip installs the block, disabling hides it, and re-enabling restores it.

### 6.2 Minimal map extension plugin (optional)

Add another example plugin contributing a dummy `LayerProvider` and `ToolProvider` that draws a polyline when toggled.

## Overall Acceptance Criteria

- App starts with zero baked blocks; UI content is entirely plugin-driven.
- Dropping a valid `plugin.zip` into `%APPDATA%/Nexus/plugins/inbox` installs and activates it live via file watcher.
- Disabling a plugin unloads its ALC, reduces memory usage, and updates layout state to hide contributed UI.
- Manifest `requires` rules prevent activation when dependencies are missing or versions incompatible; Plugin Manager surfaces the error clearly.
- `PackPlugin` emits schema-validated zips; CI validates manifests and regenerates `docs/plugins/schema/plugin.schema.json`.
- ADRs and developer guide stay current automatically.

## Implementation Notes

- Use one collectible `AssemblyLoadContext` per plugin. Load managed dependencies into that context while unifying SDK assemblies from the host to avoid type identity issues.
- Activate lazily: discover plugins at startup but instantiate windows/blocks/tools when requested by the shell to minimize resource usage.
- Prefer embedded resources for assets and support `/assets/` fallbacks mapped to `avares://<pluginId>/...`.
- Persist plugin state (enabled/disabled, active version) in `%APPDATA%/Nexus/plugins/plugins.json`.
- Allow unsigned zips during development but retain signing hooks for official distribution.
- Telemetry must log plugin load/unload events, exceptions, and ALC unload outcomes.

## Deletions / Deprecations (Immediate)

- Remove or mark obsolete hard-coded `CoreBlockProvider` registrations.
- Remove documentation-only plugin manifests under `docs/plugins/manifests/**`; replace them with the generated schema and example plugin directories.
- Keep the monolithic `Aog.Plugins` assembly compiling temporarily, but shift new work into example plugins to validate the loader.

## Deliverables Checklist

- `Nexus.Sdk.Core`, `Nexus.Sdk.UI.Avalonia`, and `Nexus.Sdk.AgIo` projects with prerelease NuGet packaging.
- `PluginManifest` model, JSON Schema, and validator.
- `PackPlugin.targets` plus sample project wiring.
- Plugin host, load context, registries, and Plugin Manager panel.
- Refactored UI shell using registries (no hard-coded blocks).
- Minimal example plugin(s) with end-to-end load/disable/unload tests.
- ADRs and developer guide updates, plus CI validation for manifests and schema regeneration.

## Quick Reference Commands

```bash
# Build SDKs
dotnet build "Nexus SourceCode/src/Nexus.Sdk.Core"
dotnet build "Nexus SourceCode/src/Nexus.Sdk.UI.Avalonia"

# Pack example plugin
dotnet msbuild "Nexus SourceCode/plugins/examples/fe.hello/Fe.Hello.csproj" /t:PackPlugin /p:Configuration=Release

# Install: copy zip to user plugin directory
mkdir -p "%APPDATA%/Nexus/plugins/inbox"
copy .\bin\Release\fe.hello-1.0.0.zip "%APPDATA%\Nexus\plugins\inbox\"
```

## Need Starter Files?

If a jump-start is required, request the prepared starter code for interfaces, minimal `PluginHost`, `PackPlugin` target, and the hello-block plugin.

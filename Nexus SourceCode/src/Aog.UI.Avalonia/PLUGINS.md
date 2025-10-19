# Plugin Integration Guide — Avalonia UI Shell

This guide documents the entry points plugins use to extend the Nexus Avalonia shell after they have been activated by the core plugin host. It focuses on UI composition, asset loading, and lifecycle expectations.

---

## UI Registries at a Glance

The UI host exposes several registries through `IHostServices.Ui` (from `Nexus.Sdk.UI.Avalonia`):

| Registry | Purpose | Key Interfaces |
| --- | --- | --- |
| `BlockRegistry` | Sidebar/footer command blocks rendered in the shell chrome. | `IBlockProvider`, `BlockDescriptor` |
| `WindowRegistry` | Top-level windows or dialogs that plugins can open on demand. | `IWindowProvider`, `WindowDescriptor` |
| `ToolRegistry` | Canvas tools (draw, measure, waypoint) surfaced on the toolbar/map. | `IToolProvider`, `ToolDescriptor` |
| `LayerRegistry` | Map layer factories (coverage, heatmaps, vector overlays). | `ILayerProvider`, `LayerDescriptor` |
| `ShellCommandRegistry` | Commands bound to menus/toolbar buttons. | `IShellCommandHandler` |
| `StatusIndicatorRegistry` | Status strip indicators contributed by plugins. | `IStatusIndicatorProvider` |

Plugins register providers inside their `IUiEntrypoint.Initialize` implementation:

```csharp
public sealed class SectionsUiEntrypoint : IUiEntrypoint, IBlockProvider, IWindowProvider
{
    private IUiHostServices? _uiServices;

    public void Initialize(IHostServices services)
    {
        _uiServices = services.Ui;
        _uiServices.BlockRegistry.Register(this);
        _uiServices.WindowRegistry.Register(this);
    }

    public IEnumerable<BlockDescriptor> GetBlocks(IServiceProvider scope) { ... }
    public IReadOnlyList<WindowDescriptor> GetWindows(IServiceProvider scope) { ... }

    public ValueTask ShutdownAsync(CancellationToken ct)
    {
        _uiServices?.BlockRegistry.Unregister(this);
        _uiServices?.WindowRegistry.Unregister(this);
        _uiServices = null;
        return ValueTask.CompletedTask;
    }
}
```

### Descriptor Basics

Each descriptor declares:

- **Id** — Stable identifier used for persistence.
- **Display metadata** — Label, tooltip, icon.
- **Factory** — Lambda that resolves the actual view/view-model using the plugin scoped `IServiceProvider`.
- **Contracts** — Optional surface metadata (e.g., `ShellPluginSurfaces.TopToolbar`).

The block and window registries coordinate with the layout store so blocks disappear automatically when a plugin is disabled.

---

## Asset Loading

The loader exposes plugin assets through `IAssetLocator`. Plugins can:

- Reference embedded Avalonia resources via `avares://{PluginId}/Views/MyView.axaml`.
- Load loose files declared in `manifest.json` under `assets/`.
- Provide icons by returning `AssetDescriptor`s from descriptors (the shell turns these into `IBitmap`s on demand).

All assets are scoped to the plugin directory; core never looks outside of the registered paths.

---

## Menus, Toolbars, and Commands

- **Menus** – Implement `IShellCommandHandler` and register items with the `ShellCommandRegistry`. Associate menu entries by referencing injection points from `artifacts/ui-to-plugin.yaml`.
- **Toolbar Buttons** – Provide a block descriptor or contribute to `PluginSurfaceDescriptor` injection points (`toolbar.top`, `toolbar.section`, etc.).
- **Keyboard Shortcuts** – Use `ShortcutRegistry.Register` with unique IDs; shortcuts are automatically namespaced per plugin.

Ensure that command handlers are idempotent and check enablement before acting. The dispatcher stops routing once a handler returns `true`.

---

## Map Integration

Mapping-oriented plugins can integrate with the shared map host:

```csharp
public void Initialize(IHostServices services)
{
    var mapHost = services.Ui.MapHost;
    mapHost.RegisterLayerProvider(new CoverageLayerProvider());
    mapHost.RegisterToolProvider(new HeadlandToolProvider());
}
```

- Layer providers return `LayerDescriptor`s that build view-models for overlays.
- Tool providers expose `Activate` / `Deactivate` to wire input gestures.
- Plugins must unregister providers in `ShutdownAsync`.

The map host ensures determinism in simulation mode by replaying tool interactions through the same APIs during regression runs.

---

## Dialogs and Windows

`IWindowProvider` descriptors support:

- **Modal dialogs** – Provide a `WindowFactory` delegate that resolves an `Avalonia.Controls.Window`. The shell automatically sets the owner and handles async completion.
- **Non-modal windows** – Mark the descriptor as `IsSingleton`. The shell reuses the same instance until the plugin unloads.
- **Contextual launchers** – Combine with `IShellCommandHandler` to open dialogs in response to toolbar or menu commands.

When a plugin is disabled, the shell closes any open windows originating from that plugin.

---

## Status Strip & Telemetry

Use `IStatusIndicatorProvider` to surface telemetry snapshots on the status strip. Each indicator exposes:

- Title
- Value text
- Severity (`Normal`, `Warning`, `Critical`)
- Tooltip

The status strip view-model refreshes indicators on the UI thread when the provider signals a change.

---

## Layout Persistence

`BlockLayoutStore` serializes active block placements. When a plugin contributes new blocks:

1. The registry emits `LayoutChanged`.
2. The layout store persists the new block instances.
3. Disabling the plugin removes its block records to prevent stale references.

Plugins should provide deterministic block IDs and avoid mutating descriptors after registration.

---

## UX Guidelines

- Keep blocks concise; long-running operations should open a dedicated dialog or flyout.
- Use the theme resources defined in `App/NexusLegacyShellTheme.axaml`.
- Respect DPI scaling; prefer vector-based icons.
- Provide localization keys for strings if the plugin will ship to non-English markets.

---

## Debugging Tips

- Launch Nexus with `--plugins:trace` to enable verbose loader logging (search for `"PluginUi"` category).
- Use the Plugin Manager panel (`View > Plugin Manager`) to inspect resolved descriptors and dependency trees.
- The diagnostics sidebar exposes loaded ALC counts; unloading a plugin should decrement the count by one.

For examples, review the official plugin guides in `docs/plugins/official/` and the `fe.hello` example plugin under `plugins/examples/`.


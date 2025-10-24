# Floating Block Layout Overview

The Nexus shell now renders quick actions and plugin shortcuts as floating blocks rather than sidebar
buttons. The layout engine combines persisted `ShellGridLayout` tiles with `FloatingBlockSpec`
overlays so the workspace can mix docked panels and free-form controls without maintaining separate
side strip templates.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/Shell/BlockLayoutViewModel.cs†L57-L125】

## Layout persistence

`ShellLayoutPreferences` stores the grid definition, floating panel metadata, and a collection of
`FloatingBlockSpec` records keyed by `BlockInstanceId`. Each spec captures the overlay's position and
size; when a spec is missing or has zero dimensions, `BlockLayoutViewModel` derives a default width,
height, and origin so overlays remain visible after migrations.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Settings/ShellLayoutPreferences.cs†L9-L152】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/Shell/BlockLayoutViewModel.cs†L500-L567】

`BlockLayoutStore` seeds default shortcut clones through `EnsureFloatingShortcut`. The helper
normalises any legacy sidebar clones to `BlockRegion.Overlay`, assigns stable `Order` values, and
ensures a matching `FloatingBlockSpec` exists with curated coordinates. Telemetry tiles continue to be
seeded on the tiled grid via `EnsureClone`, which now updates existing clones when the preferred region
changes.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Blocks/BlockLayoutStore.cs†L129-L206】

## Runtime composition

`AppShellView.axaml` binds floating overlays through an `ItemsControl` that targets
`BlockLayoutViewModel.FloatingBlocks`. Each entry renders a `FloatingBlockOverlay`, while tiles and
panes continue to use the `TiledPanel` and `PaneCanvas` presenters. Sidebar button templates and
width converters were removed; the workspace margin still honours `SidebarLayoutSettings` spacing for
panel tiles.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Views/Shell/AppShellView.axaml†L1-L520】

`FloatingBlockOverlay.axaml` now consumes themed gradient brushes (`FloatingBlockBackgroundBrush`) and
corner-radius tokens from `NexusLegacyShellTheme.axaml`, aligning overlay chrome with the refreshed
palette. The overlay exposes drag and resize handles that call back into
`BlockLayoutViewModel.UpdateFloatingBlock` after coordinates are normalised by `ClampFloatingBounds`.
`FloatingBlockViewModel` raises a settings command when unlocked, emitting
`BlockLayoutViewModel.FloatingBlockSettingsRequested` so modal dialogs can open per overlay.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Views/Shell/FloatingBlockOverlay.axaml†L1-L74】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/Shell/FloatingBlockViewModel.cs†L15-L117】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/Shell/BlockLayoutViewModel.cs†L987-L1115】

## Theming updates

`NexusLegacyShellTheme.axaml` introduces new surface/accent colours, gradient brushes, and typography
sizes that match the floating block demo palette. Both light and dark dictionaries expose
`ShellChromeGradientBrush` and `FloatingBlockBackgroundBrush`, while font and spacing tokens were
retuned (11/13/15/20/28pt scale, wider spacing, and 6/12/18px corner radii). Floating overlays consume
those resources via `DynamicResource`, giving drag handles and settings chrome consistent borders with
the rest of the shell.【F:Nexus SourceCode/src/Aog.UI.Avalonia/App/NexusLegacyShellTheme.axaml†L1-L86】

## Interaction flow

`BlockLayoutViewModel` manages overlay state alongside grid tiles. Unlocking the layout toggles
`AreFloatingOverlaysVisible` and pushes the lock state into each `FloatingBlockViewModel`. Updates to
positions or sizes immediately persist via `_layoutStore.Save` and
`IUiPreferencesService.UpdateShellLayout`, ensuring quick actions rehydrate on the next launch. Tests
in `BlockLayoutViewModelTests` assert the new lock toggles, settings command routing, and floating
block persistence behaviours.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/Shell/BlockLayoutViewModel.cs†L202-L362】【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/Shell/BlockLayoutViewModel.cs†L1008-L1115】【F:Nexus SourceCode/tests/Aog.UI.Avalonia.Tests/Blocks/BlockLayoutViewModelTests.cs†L19-L196】


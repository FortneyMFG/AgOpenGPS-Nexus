# Sidebar Layout System Overview

The Nexus shell uses a shared set of layout primitives to drive every block surface in the UI—left/right sidebars, the top and bottom strips, and the central workspace. This document summarizes how the sizing model, runtime converters, overlay rendering, and view-model orchestration fit together.

## Core layout settings

`SidebarLayoutSettings` defines the sizing contract that each region uses:

- Independent `WidthMode` and `HeightMode` values decide whether a dimension stretches to fill (`Dynamic`) or clamps to a fixed block count (`Fixed`).
- When a dimension is fixed, the `BlockColumns` or `BlockRows` values indicate how many whole or fractional tiles the surface should expose.
- `BlockSize` (default `112px`) and `Spacing` (default `8px`) supply the base tile dimensions shared across regions, while `Clone()` provides deep-copy support so view models can hold editable instances. Factory helpers seed defaults tuned for vertical sidebars, the top telemetry strip, the bottom toolbar, and the workspace grid.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Settings/SidebarLayoutSettings.cs†L7-L88】

`ShellLayoutPreferences` persists one `SidebarLayoutSettings` per region together with block instances, visibility toggles, and the workspace grid copy. When layout editing is unlocked, the UI works against clones so the operator can tweak widths, heights, and spacing without mutating the stored defaults until they commit changes.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Settings/ShellLayoutPreferences.cs†L9-L68】

## Runtime sizing converters

Two value converters translate the persisted layout settings into concrete measurements at render time:

- `SidebarDimensionConverter` returns an explicit pixel width or height when the associated dimension is fixed, otherwise it yields `double.NaN` so Avalonia can stretch the container dynamically.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Rendering/SidebarDimensionConverter.cs†L9-L38】
- `SpacingToThicknessConverter` turns the spacing value into symmetric margins, keeping content presenters aligned with the overlay grid regardless of orientation.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Rendering/SpacingToThicknessConverter.cs†L1-L22】

## Grid overlay rendering

`SidebarGridOverlay` draws a semi-transparent overlay whenever layout editing is active. It samples the bound `SidebarLayoutSettings`, fills the region with a light background, and computes how many cells to show along each dimension. Fixed dimensions respect the configured block counts (including fractional values); dynamic dimensions calculate how many slots fit within the current bounds, clamping to the base block size and spacing so the overlay mirrors Avalonia's tile layout.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Controls/SidebarGridOverlay.cs†L10-L170】

## Shell view integration

`AppShellView.axaml` wraps every block region inside a `SidebarGridOverlay`. Left and right sidebars use vertical `WrapPanel` presenters, the top and bottom strips use horizontal wrapping, and the workspace border displays the same overlay over the main content with extra padding. When the layout is unlocked (`Layout.IsLocked == false`), the overlays become visible and drag/drop is enabled; locking hides them and returns the surfaces to a read-only state.【F:Nexus SourceCode/src/Aog.UI.Avalonia/Views/Shell/AppShellView.axaml†L394-L508】

## Interaction flow

`BlockLayoutViewModel` loads the persisted `ShellLayoutPreferences`, clones each region's `SidebarLayoutSettings`, and exposes observable block collections for bindings. Toggling `IsLocked` flips drag/drop availability, refreshes command states, and controls whether overlays are shown. The view model also routes block invocations through the shell command dispatcher, emitting default status messages when appropriate.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/Shell/BlockLayoutViewModel.cs†L17-L118】

Together these pieces deliver a consistent, tile-based layout system that stretches smoothly with the window, preserves user customizations, and provides visual feedback while editing.

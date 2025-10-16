# NX-414 Settings, Hotkeys, and Appearance Consolidation

## Objectives
- Consolidate legacy settings dialogs into Avalonia experiences that align with [`artifacts/ui-theme-tokens.json`](../../artifacts/ui-theme-tokens.json) and [`artifacts/ui-backlog.json`](../../artifacts/ui-backlog.json).
- Reuse metadata-driven configuration infrastructure for persistence, import/export, and validation parity with V6/Dev shells.
- Ensure accessibility (contrast, focus order, keyboard navigation) matches guidance in [`docs/reference/metadata-driven-ui-style-guide.md`](../reference/metadata-driven-ui-style-guide.md).

## Settings Hub Composition

### Appearance & Display (`color_settings_dialog`, `appearance_palette_panel`)
- Port the colour picker, brightness/contrast sliders, and theme preset selectors from legacy WinForms (`FormColor.cs`) and AgValonia prototypes (`App.axaml`).
- Bind palette values directly to theme token providers so updates refresh active UI surfaces in real time.
- Introduce preview tiles demonstrating typography, spacing, and component states for operator confirmation.
- Persist custom palettes within `SettingsPersistenceService` and expose import/export to JSON for sharing across machines.

### Hotkey Manager (`hotkey_dialog`)
- Recreate keyboard shortcut assignment grid with search, conflict detection, and reset-to-default flows.
- Integrate with the unified input binding service so updates propagate to shell commands and plugin injection points (`settings.input`).
- Provide tests covering duplicate detection, macro assignments, and serialization.
- Offer quick-filter tabs (Navigation, Mapping, Sections, Diagnostics) matching backlog expectations.

### Help & About (`help_dialog`, `about_dialog`)
- Merge scattered dialogs into a centralized `AboutPanelViewModel` that surfaces version/build metadata, licensing acknowledgements, and support links.
- Surface documentation shortcuts (how-to guides, knowledge base) and plugin compatibility snapshots.
- Add copy-to-clipboard actions for support diagnostics.

### Configuration Profiles (`settings_profiles_panel`)
- Implement profile save/load/export flows allowing operators to capture full configuration snapshots.
- Support diff/merge previews when importing external profiles; reuse metadata diff presenter from NX-298.
- Validate profile schema using `tools/schemas/settings-profile.schema.json` when available.

## Persistence & Extensibility
- Expand the existing configuration registry to cover new settings scopes (appearance, input, help/about) with schema-backed validation.
- Provide `ISettingsUpgradeHandler` implementations for migrating legacy configuration files.
- Document extension points for plugins to contribute settings sections with consistent theming and telemetry.

## Testing & QA
- Unit-test view-model validation logic and conflict resolution in `Aog.UI.Avalonia.Tests`.
- Extend `nexus sim smoke` scenarios to toggle themes, rebind hotkeys, and reload profiles ensuring no runtime errors.
- Capture accessibility audit results (contrast ratios, focus order) and record them alongside screenshots in `artifacts/ui-screenshots` during rollout.

## Deliverables
- Avalonia dialogs/panels and supporting view-models for all settings-related backlog entries.
- Updated metadata-driven sample data enabling Storybook previews for appearance and hotkey flows.
- Documentation updates summarizing configuration migration steps and user-facing changes.

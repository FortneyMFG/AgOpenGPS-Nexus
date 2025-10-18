# NX-411 UI Migration Workbook

This workbook consolidates legacy UI assets, plugin surface contracts, and backlog metadata to guide the migration from the V6/Dev/AgValonia shells to the Nexus Avalonia host.

## Source Licensing Snapshot

| Legacy source | Detected license | Compliance notes |
| --- | --- | --- |
| Legacy SourceCode -V6 | GPL-3.0 | See [artifacts/ui-license-checklist.md](ui-license-checklist.md) for required obligations. |
| Legacy SourceCode -Dev | GPL-3.0 | See [artifacts/ui-license-checklist.md](ui-license-checklist.md) for required obligations. |
| Legacy SourceCode -AgValoniaGPS | Missing | See [artifacts/ui-license-checklist.md](ui-license-checklist.md) for required obligations. |

## Menu Surfaces

| Surface ID | Legacy intent | Plugin contract | Injection point | Backlog references | Provenance status |
| --- | --- | --- | --- | --- | --- |
| field_menu | Field Menu | core.field_menu.v1 | menu.field | [shell-and-navigation] Implement Field Menu UI, [shell-and-navigation] Implement Field Menu ViewModel, [shell-and-navigation] Implement Field Menu Tests | Recreated in `Nexus SourceCode/src/Aog.UI.Avalonia/Views/Shell/AppShellView.axaml`; layout validated against V6 capture and styled with theme tokens. |
| file_menu | File Menu | core.file_menu.v1 | menu.file | [shell-and-navigation] Implement File Menu UI, [shell-and-navigation] Implement File Menu ViewModel, [shell-and-navigation] Implement File Menu Tests | Recreated in `Nexus SourceCode/src/Aog.UI.Avalonia/Views/Shell/AppShellView.axaml`; bindings match plugin descriptor registry and theme token palette. |
| tools_menu | Tools Menu | core.tools_menu.v1 | menu.tools | [shell-and-navigation] Implement Tools Menu UI, [shell-and-navigation] Implement Tools Menu ViewModel, [shell-and-navigation] Implement Tools Menu Tests | Recreated in `Nexus SourceCode/src/Aog.UI.Avalonia/Views/Shell/AppShellView.axaml`; toolbar affordances align with V6 baseline using shared resources. |

## Toolbar Surfaces

| Surface ID | Legacy intent | Plugin contract | Injection point | Backlog references | Provenance status |
| --- | --- | --- | --- | --- | --- |
| top_toolbar | Top Command Toolbar | core.top_toolbar.v1 | toolbar.top | [shell-and-navigation] Implement Top Command Toolbar UI, [shell-and-navigation] Implement Top Command Toolbar ViewModel, [shell-and-navigation] Implement Top Command Toolbar Tests | Implemented in `Nexus SourceCode/src/Aog.UI.Avalonia/Views/Shell/AppShellView.axaml` + `ViewModels/Shell/TopToolbarViewModel.cs`; button states themed with `App/NexusLegacyShellTheme.axaml`. |

## Panels & Map Canvas

| Surface ID | Legacy intent | Plugin contract | Injection point | Coverage assets | Provenance status |
| --- | --- | --- | --- | --- | --- |
| app_shell | Main Field View | core.app_shell.v1 | shell.main | Hosts shell composites (menus, toolbar, status strip). | Ported into Avalonia (`Views/Shell/AppShellView.axaml`); theme parity achieved with `App/NexusLegacyShellTheme.axaml`, awaiting live map host wiring. |
| map_canvas | OpenGL Map Canvas | core.map_canvas.v1 | shell.map | Refer to docs/aog-v6-mapping-brief.md for overlay breakdown. | Placeholder host remains; map canvas wiring tracked under NX-412 follow-up. |

## Dialog Surfaces

| Surface ID | Legacy intent | Plugin contract | Injection point | Backlog references | Provenance status |
| --- | --- | --- | --- | --- | --- |
| about_dialog | About Dialog | core.about_dialog.v1 | help.modal | [dialogs-and-wizards] Implement About Dialog UI, [dialogs-and-wizards] Implement About Dialog ViewModel, [dialogs-and-wizards] Implement About Dialog Tests | Layout references pending; capture from Dev branch once archives restored. |
| ag_io_advanced_settings_dialog | AgIO Advanced Settings | core.ag_io_advanced_settings_dialog.v1 | plugin.agio | [hardware-and-sim] Implement AgIO Advanced Settings UI, [hardware-and-sim] Implement AgIO Advanced Settings ViewModel, [hardware-and-sim] Implement AgIO Advanced Settings Tests | Layout references pending; capture from Dev branch once archives restored. |
| ag_io_loop_dialog | AgIO Loop Monitor | core.ag_io_loop_dialog.v1 | plugin.agio | [hardware-and-sim] Implement AgIO Loop Monitor UI, [hardware-and-sim] Implement AgIO Loop Monitor ViewModel, [hardware-and-sim] Implement AgIO Loop Monitor Tests | Layout references pending; capture from Dev branch once archives restored. |
| ag_io_serial_dialog | AgIO Serial Configuration | core.ag_io_serial_dialog.v1 | plugin.agio | [hardware-and-sim] Implement AgIO Serial Configuration UI, [hardware-and-sim] Implement AgIO Serial Configuration ViewModel, [hardware-and-sim] Implement AgIO Serial Configuration Tests | Layout references pending; capture from Dev branch once archives restored. |
| boundary_tool_dialog | Boundary Tool | core.boundary_tool_dialog.v1 | dialog.boundary | [mapping-and-canvas] Implement Boundary Tool UI, [mapping-and-canvas] Implement Boundary Tool ViewModel, [mapping-and-canvas] Implement Boundary Tool Tests | Layout references pending; capture from Dev branch once archives restored. |
| color_settings_dialog | Display Color Settings | core.color_settings_dialog.v1 | settings.display | [settings-and-plugins] Implement Display Color Settings UI, [settings-and-plugins] Implement Display Color Settings ViewModel, [settings-and-plugins] Implement Display Color Settings Tests | Layout references pending; capture from Dev branch once archives restored. |
| event_viewer_dialog | Event Viewer | core.event_viewer_dialog.v1 | diagnostics.logs | [dialogs-and-wizards] Implement Event Viewer UI, [dialogs-and-wizards] Implement Event Viewer ViewModel, [dialogs-and-wizards] Implement Event Viewer Tests | Layout references pending; capture from Dev branch once archives restored. |
| field_directory_dialog | Create Field Dialog | core.field_directory_dialog.v1 | dialog.jobs | [dialogs-and-wizards] Implement Create Field Dialog UI, [dialogs-and-wizards] Implement Create Field Dialog ViewModel, [dialogs-and-wizards] Implement Create Field Dialog Tests | Layout references pending; capture from Dev branch once archives restored. |
| field_existing_dialog | Open Existing Field Dialog | core.field_existing_dialog.v1 | dialog.jobs | [dialogs-and-wizards] Implement Open Existing Field Dialog UI, [dialogs-and-wizards] Implement Open Existing Field Dialog ViewModel, [dialogs-and-wizards] Implement Open Existing Field Dialog Tests | Layout references pending; capture from Dev branch once archives restored. |
| field_map_dialog | Field Map Preview | core.field_map_dialog.v1 | dialog.jobs | [mapping-and-canvas] Implement Field Map Preview UI, [mapping-and-canvas] Implement Field Map Preview ViewModel, [mapping-and-canvas] Implement Field Map Preview Tests | Layout references pending; capture from Dev branch once archives restored. |
| field_save_dialog | Save Field Confirmation | core.field_save_dialog.v1 | dialog.general | — | Layout references pending; capture from Dev branch once archives restored. |
| flag_manager_dialog | Flag Manager | core.flag_manager_dialog.v1 | dialog.flags | [mapping-and-canvas] Implement Flag Manager UI, [mapping-and-canvas] Implement Flag Manager ViewModel, [mapping-and-canvas] Implement Flag Manager Tests | Layout references pending; capture from Dev branch once archives restored. |
| gps_data_dialog | GPS Data Monitor | core.gps_data_dialog.v1 | diagnostics.telemetry | [dialogs-and-wizards] Implement GPS Data Monitor UI, [dialogs-and-wizards] Implement GPS Data Monitor ViewModel, [dialogs-and-wizards] Implement GPS Data Monitor Tests | Layout references pending; capture from Dev branch once archives restored. |
| help_dialog | Help Dialog | core.help_dialog.v1 | help.modal | [dialogs-and-wizards] Implement Help Dialog UI, [dialogs-and-wizards] Implement Help Dialog ViewModel, [dialogs-and-wizards] Implement Help Dialog Tests | Layout references pending; capture from Dev branch once archives restored. |
| hotkey_dialog | Hotkey Manager | core.hotkey_dialog.v1 | settings.input | [settings-and-plugins] Implement Hotkey Manager UI, [settings-and-plugins] Implement Hotkey Manager ViewModel, [settings-and-plugins] Implement Hotkey Manager Tests | Layout references pending; capture from Dev branch once archives restored. |
| job_manager_dialog | Job Manager Dialog | core.job_manager_dialog.v1 | dialog.jobs | [dialogs-and-wizards] Implement Job Manager Dialog UI, [dialogs-and-wizards] Implement Job Manager Dialog ViewModel, [dialogs-and-wizards] Implement Job Manager Dialog Tests | Layout references pending; capture from Dev branch once archives restored. |
| section_config_dialog | Section Configuration | section_control.v1 | settings.plugins | [settings-and-plugins] Implement Section Configuration UI, [settings-and-plugins] Implement Section Configuration ViewModel, [settings-and-plugins] Implement Section Configuration Tests, [section-control] Implement Section Configuration UI, [section-control] Implement Section Configuration ViewModel, [section-control] Implement Section Configuration Tests | Layout references pending; capture from Dev branch once archives restored. |
| shift_position_dialog | Shift Position | core.shift_position_dialog.v1 | tools.offset | [mapping-and-canvas] Implement Shift Position UI, [mapping-and-canvas] Implement Shift Position ViewModel, [mapping-and-canvas] Implement Shift Position Tests | Layout references pending; capture from Dev branch once archives restored. |
| simulator_main_dialog | ModSim Main Window | core.simulator_main_dialog.v1 | simulation.shell | [hardware-and-sim] Implement ModSim Main Window UI, [hardware-and-sim] Implement ModSim Main Window ViewModel, [hardware-and-sim] Implement ModSim Main Window Tests | Layout references pending; capture from Dev branch once archives restored. |
| steer_chart_dialog | Steer Performance Chart | autosteer.v1 | diagnostics.autosteer | [autosteer] Implement Steer Performance Chart UI, [autosteer] Implement Steer Performance Chart ViewModel, [autosteer] Implement Steer Performance Chart Tests | Layout references pending; capture from Dev branch once archives restored. |
| steer_settings_dialog | Steer Settings | autosteer.v1 | settings.plugins | [settings-and-plugins] Implement Steer Settings UI, [settings-and-plugins] Implement Steer Settings ViewModel, [settings-and-plugins] Implement Steer Settings Tests, [autosteer] Implement Steer Settings UI, [autosteer] Implement Steer Settings ViewModel, [autosteer] Implement Steer Settings Tests | Layout references pending; capture from Dev branch once archives restored. |
| steer_wizard_dialog | Steer Wizard | autosteer.v1 | wizard.autosteer | [autosteer] Implement Steer Wizard UI, [autosteer] Implement Steer Wizard ViewModel, [autosteer] Implement Steer Wizard Tests | Layout references pending; capture from Dev branch once archives restored. |
| udp_status_dialog | UDP Status | core.udp_status_dialog.v1 | diagnostics.telemetry | [hardware-and-sim] Implement UDP Status UI, [hardware-and-sim] Implement UDP Status ViewModel, [hardware-and-sim] Implement UDP Status Tests | Layout references pending; capture from Dev branch once archives restored. |
| webcam_dialog | Webcam Viewer | video_monitor.v1 | plugin.video | [dialogs-and-wizards] Implement Webcam Viewer UI, [dialogs-and-wizards] Implement Webcam Viewer ViewModel, [dialogs-and-wizards] Implement Webcam Viewer Tests | Layout references pending; capture from Dev branch once archives restored. |

## Theme Token Snapshot

Theme tokens extracted from `artifacts/ui-theme-tokens.json` to enforce parity between legacy and Nexus styling. `App/NexusLegacyShellTheme.axaml` now materializes these values into Avalonia resources consumed by the shell UI.

### Typography

Family: `Segoe UI`

| Token | Size |
| --- | --- |
| xs | 10 |
| sm | 12 |
| md | 14 |
| lg | 18 |
| xl | 24 |

### Spacing

| Token | Value |
| --- | --- |
| xs | 4 |
| sm | 8 |
| md | 12 |
| lg | 16 |
| xl | 24 |

### Radius

| Token | Value |
| --- | --- |
| sm | 4 |
| md | 8 |
| lg | 12 |

### Color (Light)

| Token | Hex |
| --- | --- |
| surface | #FFFFFF |
| surface-alt | #F3F6FB |
| text | #1F2933 |
| accent | #1D9BF0 |
| accent-muted | #7CC4FF |
| success | #2DCB74 |
| warning | #F7B529 |
| danger | #E5484D |

### Color (Dark)

| Token | Hex |
| --- | --- |
| surface | #131722 |
| surface-alt | #1F2534 |
| text | #F8FAFC |
| accent | #5AB9FF |
| accent-muted | #94D2FF |
| success | #4ADE80 |
| warning | #FACC15 |
| danger | #F87171 |


# NX-410 Legacy UI Asset Migration Workbook

## Overview
- Task NX-410 captures the groundwork for porting AgOpenGPS legacy UI workflows into the Nexus Avalonia shell.
- This workbook inventories WinForms, Dev-branch, and AgValonia prototypes, flags licensing constraints, and links backlog acceptance criteria for each surface.
- Source metadata originates from [`../artifacts/ui-inventory.json`](../artifacts/ui-inventory.json) and [`../artifacts/ui-backlog.json`](../artifacts/ui-backlog.json).

## Licensing Snapshot
- **Legacy SourceCode -V6** — GPL-3.0. Preserve license text with binaries, offer source on distribution, avoid copying GPL files into permissively licensed modules, and record attribution in `THIRD_PARTY_NOTICES.md` per [`../artifacts/ui-license-checklist.md`](../artifacts/ui-license-checklist.md).
- **Legacy SourceCode -Dev** — GPL-3.0 with the same obligations as V6.
- **Legacy SourceCode -AgValoniaGPS** — License unknown; ownership confirmation is required before reusing Avalonia XAML or assets.

## Component Decisions
The following sections list every UI surface referenced in the modernization plan. For each component we capture provenance, recommended reuse strategy, licensing follow-ups, and distilled acceptance checkpoints from the backlog issues to align implementation intent.

### Core Shell & Navigation
#### Display Color Settings (`color_settings_dialog`)
- **Element type:** dialog; **Domain focus:** visual.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Settings/FormColor.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Settings/FormColor.cs`
  - Legacy SourceCode -AgValoniaGPS: `Views/App.axaml`
- **Reuse decision:** Adopt the Avalonia layout from AgValonia prototypes after license clearance, while porting behavioural logic from the V6/Dev WinForms sources into the Nexus plugin contracts for visual.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -AgValoniaGPS: License missing: contact repository owner to confirm terms before reuse; record attribution once clarified.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `settings.display`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### File Menu (`file_menu`)
- **Element type:** menu; **Domain focus:** core.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
- **Reuse decision:** Rebuild the menu in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for core behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `menu.file`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Hotkey Manager (`hotkey_dialog`)
- **Element type:** dialog; **Domain focus:** core.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Form_Keys.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Form_Keys.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for core behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `settings.input`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Main Field View (`app_shell`)
- **Element type:** panel; **Domain focus:** mapping, section-control, autosteer.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormGPS.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormGPS.cs`
  - Legacy SourceCode -AgValoniaGPS: `Views/MainWindow.axaml`
- **Reuse decision:** Adopt the Avalonia layout from AgValonia prototypes after license clearance, while porting behavioural logic from the V6/Dev WinForms sources into the Nexus plugin contracts for mapping, section-control, autosteer.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -AgValoniaGPS: License missing: contact repository owner to confirm terms before reuse; record attribution once clarified.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `shell.main`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Tools Menu (`tools_menu`)
- **Element type:** menu; **Domain focus:** autosteer, diagnostics.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
- **Reuse decision:** Rebuild the menu in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for autosteer, diagnostics behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `menu.tools`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Top Command Toolbar (`top_toolbar`)
- **Element type:** toolbar; **Domain focus:** autosteer, section-control.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
  - Legacy SourceCode -AgValoniaGPS: `Views/MainWindow.axaml`
- **Reuse decision:** Adopt the Avalonia layout from AgValonia prototypes after license clearance, while porting behavioural logic from the V6/Dev WinForms sources into the Nexus plugin contracts for autosteer, section-control.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -AgValoniaGPS: License missing: contact repository owner to confirm terms before reuse; record attribution once clarified.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `toolbar.top`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Core Mapping Surfaces
#### Boundary Tool (`boundary_tool_dialog`)
- **Element type:** dialog; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Field/FormBndTool.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Field/FormBndTool.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `dialog.boundary`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Field Map Preview (`field_map_dialog`)
- **Element type:** dialog; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Field/FormMap.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Field/FormMap.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `dialog.jobs`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Field Menu (`field_menu`)
- **Element type:** menu; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormGPS.Designer.cs`
- **Reuse decision:** Rebuild the menu in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `menu.field`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Flag Manager (`flag_manager_dialog`)
- **Element type:** dialog; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Field/FormFlags.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Field/FormFlags.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `dialog.flags`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### OpenGL Map Canvas (`map_canvas`)
- **Element type:** panel; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/OpenGL.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/OpenGL.Designer.cs`
  - Legacy SourceCode -AgValoniaGPS: `Views/MainWindow.axaml`
- **Reuse decision:** Adopt the Avalonia layout from AgValonia prototypes after license clearance, while porting behavioural logic from the V6/Dev WinForms sources into the Nexus plugin contracts for mapping.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -AgValoniaGPS: License missing: contact repository owner to confirm terms before reuse; record attribution once clarified.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `shell.map`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Shift Position (`shift_position_dialog`)
- **Element type:** dialog; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormShiftPos.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormShiftPos.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `tools.offset`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Job & Field Lifecycle
#### Create Field Dialog (`field_directory_dialog`)
- **Element type:** dialog; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Field/FormFieldDir.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Field/FormFieldDir.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `dialog.jobs`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Job Manager Dialog (`job_manager_dialog`)
- **Element type:** dialog; **Domain focus:** mapping, data-management.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Field/FormJob.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Field/FormJob.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping, data-management behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `dialog.jobs`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Open Existing Field Dialog (`field_existing_dialog`)
- **Element type:** dialog; **Domain focus:** mapping.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Field/FormFieldExisting.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Field/FormFieldExisting.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for mapping behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `dialog.jobs`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Save Field Confirmation (`field_save_dialog`)
- **Element type:** dialog; **Domain focus:** data-management.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Field/FormSaveOrNot.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Field/FormSaveOrNot.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for data-management behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:** Pending backlog entry extraction.


### Plugin: Section & Rate Control
#### Section Configuration (`section_config_dialog`)
- **Element type:** dialog; **Domain focus:** section-control.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Settings/FormButtonsRightPanel.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Settings/FormButtonsRightPanel.cs`
  - Legacy SourceCode -AgValoniaGPS: `Views/UserControls/VehicleImplementConfigUserControl.axaml`
- **Reuse decision:** Adopt the Avalonia layout from AgValonia prototypes after license clearance, while porting behavioural logic from the V6/Dev WinForms sources into the Nexus plugin contracts for section-control.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -AgValoniaGPS: License missing: contact repository owner to confirm terms before reuse; record attribution once clarified.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `settings.plugins`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Section Control Widget (`section_control_widget`)
- **Element type:** widget; **Domain focus:** section-control.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Sections.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Sections.Designer.cs`
- **Reuse decision:** Rebuild the widget in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for section-control behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `toolbar.section`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Plugin: Autosteer & Guidance
#### Steer Performance Chart (`steer_chart_dialog`)
- **Element type:** dialog; **Domain focus:** autosteer, diagnostics.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Settings/FormGraphSteer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Settings/FormGraphSteer.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for autosteer, diagnostics behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `diagnostics.autosteer`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Steer Settings (`steer_settings_dialog`)
- **Element type:** dialog; **Domain focus:** autosteer.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Settings/FormSteer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Settings/FormSteer.cs`
  - Legacy SourceCode -AgValoniaGPS: `Views/UserControls/VehicleImplementConfigUserControl.axaml`
- **Reuse decision:** Adopt the Avalonia layout from AgValonia prototypes after license clearance, while porting behavioural logic from the V6/Dev WinForms sources into the Nexus plugin contracts for autosteer.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -AgValoniaGPS: License missing: contact repository owner to confirm terms before reuse; record attribution once clarified.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `settings.plugins`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Steer Wizard (`steer_wizard_dialog`)
- **Element type:** dialog; **Domain focus:** autosteer.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Settings/FormSteerWiz.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Settings/FormSteerWiz.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for autosteer behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `wizard.autosteer`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Core Agio
#### AgIO Advanced Settings (`ag_io_advanced_settings_dialog`)
- **Element type:** dialog; **Domain focus:** hardware.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/AgIO/Source/Forms/FormAdvancedSettings.cs`
  - Legacy SourceCode -Dev: `SourceCode/AgIO/Source/Forms/FormAdvancedSettings.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for hardware behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `plugin.agio`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### AgIO Loop Monitor (`ag_io_loop_dialog`)
- **Element type:** dialog; **Domain focus:** hardware.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/AgIO/Source/Forms/FormLoop.cs`
  - Legacy SourceCode -Dev: `SourceCode/AgIO/Source/Forms/FormLoop.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for hardware behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `plugin.agio`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### AgIO Serial Configuration (`ag_io_serial_dialog`)
- **Element type:** dialog; **Domain focus:** hardware.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/AgIO/Source/Forms/SerialComm.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/AgIO/Source/Forms/SerialComm.Designer.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for hardware behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `plugin.agio`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Core Docs
#### About Dialog (`about_dialog`)
- **Element type:** dialog; **Domain focus:** support.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Form_About.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Form_About.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for support behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `help.modal`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### Help Dialog (`help_dialog`)
- **Element type:** dialog; **Domain focus:** support.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/Form_Help.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/Form_Help.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for support behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `help.modal`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Core Simulation
#### ModSim Main Window (`simulator_main_dialog`)
- **Element type:** dialog; **Domain focus:** simulation.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/ModSim/Source/Forms/FormSim.cs`
  - Legacy SourceCode -Dev: `SourceCode/ModSim/Source/Forms/FormSim.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for simulation behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `simulation.shell`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Core Telemetry
#### Event Viewer (`event_viewer_dialog`)
- **Element type:** dialog; **Domain focus:** diagnostics.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormEventViewer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormEventViewer.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for diagnostics behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `diagnostics.logs`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### GPS Data Monitor (`gps_data_dialog`)
- **Element type:** dialog; **Domain focus:** telemetry.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormGPSData.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormGPSData.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for telemetry behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `diagnostics.telemetry`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

#### UDP Status (`udp_status_dialog`)
- **Element type:** dialog; **Domain focus:** telemetry.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/UDPComm.Designer.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/UDPComm.Designer.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for telemetry behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `diagnostics.telemetry`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.


### Plugin Video-Monitor
#### Webcam Viewer (`webcam_dialog`)
- **Element type:** dialog; **Domain focus:** monitoring.
- **Legacy references:**
  - Legacy SourceCode -V6: `SourceCode/GPS/Forms/FormWebCam.cs`
  - Legacy SourceCode -Dev: `SourceCode/GPS/Forms/FormWebCam.cs`
- **Reuse decision:** Rebuild the dialog in Avalonia using Nexus view-model contracts, guided by the referenced WinForms sources for monitoring behaviour; regenerate icons and resources rather than copying GPL assets verbatim.
- **License follow-ups:**
  - Legacy SourceCode -V6: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
  - Legacy SourceCode -Dev: GPL-3.0: preserve license text with binaries, ensure source availability, avoid copying GPL code into permissive modules, record attribution in THIRD_PARTY_NOTICES.
- **Backlog acceptance checkpoints:**
  - Matches legacy behavior documented in ui-inventory.json.
  - Hooks into plugin host via `plugin.video`.
  - Includes unit tests for primary interactions.
  - Provides Storybook-style example in @nexus/ui-core samples.

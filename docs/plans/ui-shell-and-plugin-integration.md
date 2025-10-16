# Nexus UI Shell & Plugin Integration Plan (NX-410 → NX-418)

## Objectives
- Rebuild the Nexus desktop shell using the component inventory exported to [`artifacts/ui-inventory.json`](../../artifacts/ui-inventory.json) so every legacy workflow has a mapped host in Avalonia.
- Reuse layouts, bindings, and assets from the legacy V6 WinForms shell, Dev branch experiments, and AgValonia prototypes wherever licensing allows, following the compliance checklist in [`artifacts/ui-license-checklist.md`](../../artifacts/ui-license-checklist.md).
- Wire UI surfaces to plugin contracts enumerated in [`artifacts/ui-to-plugin.yaml`](../../artifacts/ui-to-plugin.yaml) and the component guidance in [`artifacts/ui-core-spec.md`](../../artifacts/ui-core-spec.md) while keeping metadata-driven styling aligned with [`docs/reference/metadata-driven-ui-style-guide.md`](../reference/metadata-driven-ui-style-guide.md).
- Preserve the theme tokens defined in [`artifacts/ui-theme-tokens.json`](../../artifacts/ui-theme-tokens.json) and extend the sample view-models described in [`Nexus SourceCode/src/Aog.UI.Avalonia/README.md`](../../Nexus%20SourceCode/src/Aog.UI.Avalonia/README.md) to power Storybook-style previews.

## Source Harvest & Compliance (Phase 0 — NX-410)
1. Snapshot UI assets, dialogs, and resource dictionaries from:
   - `Legacy SourceCode -V6` WinForms shell (menus, dialogs, toolbar glyphs).
   - `Legacy SourceCode -Dev` experimental UI tweaks (job wizard, diagnostics panes).
   - `Legacy SourceCode -AgValoniaGPS` Avalonia prototypes (shell layout, map canvas host).
2. Record provenance for each reused asset in a new migration log and verify GPL obligations per the license checklist.
3. Extract view-model intent from `artifacts/ui-backlog.json` issue bodies to cross-check acceptance criteria for each surface.
4. Deliverable: migration workbook containing file paths, reuse decisions, and follow-up relicensing actions.

## Shell, Navigation & Theming (Phase 1 — NX-411)
1. Port `app_shell`, `top_toolbar`, `file_menu`, `tools_menu`, and `field_menu` from V6/AgValonia into Avalonia `UserControl`s that match the bindings described in the UI core spec.
2. Implement shell-level dependency injection so menu commands dispatch through plugin contracts defined in `ui-to-plugin.yaml`.
3. Rehydrate persisted layout/theming using the theme token palette and update the layout persistence infrastructure to cover new panels.
4. Deliverable: fully navigable shell with menus, toolbars, status strip, and theme toggle parity with V6.

## Map Canvas & Field Operations Surfaces (Phase 2 — NX-412)
1. Embed the OpenGL/Skia `map_canvas` host with overlays for coverage, AB lines, and vehicle glyphs by reusing the V6 drawing routines.
2. Bind metadata-driven legend and inspector panels (from existing Avalonia view-model samples) to the map canvas to maintain ADR-034 parity.
3. Implement flag manager, boundary tool, and shift position dialogs using layout definitions from legacy shells, ensuring injection points `dialog.boundary`, `dialog.flags`, and `tools.offset` are satisfied.
4. Deliverable: interactive map canvas with supporting dialogs and metadata-driven overlays.

## Job & Field Lifecycle Dialogs (Phase 3 — NX-413)
1. Port job manager, field directory, existing field picker, map preview, and field save confirmation dialogs from V6.
2. Wire dialogs to JobsService APIs and the season/session view-models already present in `Aog.UI.Avalonia` to maintain lifecycle parity.
3. Implement validation, sorting, and status tagging per backlog acceptance criteria and add golden screenshot narratives to docs.
4. Deliverable: end-to-end job/field lifecycle flow operating inside the new shell.
5. Reference plan: [`docs/plans/ui-phase3-job-field-lifecycle.md`](ui-phase3-job-field-lifecycle.md).

## Settings, Hotkeys & Appearance (Phase 4 — NX-414)
1. Rebuild display colour, hotkey manager, and help/about dialogs using AgValonia prototypes as layouts.
2. Extend theme token binding coverage (typography, spacing, corner radius) to all dialogs and ensure accessibility metrics match the metadata-driven style guide.
3. Add configuration persistence and import/export glue that mirrors V6 configuration file behaviour.
4. Deliverable: complete settings hub with accessible theming and hotkey management.
5. Reference plan: [`docs/plans/ui-phase4-settings-hotkeys.md`](ui-phase4-settings-hotkeys.md).

## Plugin-owned Surfaces (Phase 5 — NX-415)
1. Host infrastructure: Stand up the plugin surface registry (`toolbar.*`, `workspace.*`, `dialog.*`) so Avalonia controls discovered from manifests can self-register, mirroring the plugin manifest metadata surfaced in [`artifacts/ui-to-plugin.yaml`](../../artifacts/ui-to-plugin.yaml).
2. Guidance & steering family:
   - Sections/Rate: Port `section_control_widget`, `section_config_dialog`, and the variable-rate overlays/wizards from V6 so sections, rate control, and variable mapping plugins share the same configurators.
   - Autosteer/Guidance: Recreate steer settings, calibration wizard, path tuning, and performance charts, mapping telemetry into the `SteerPanelViewModel` and `GuidanceDiagnosticsViewModel` families.
   - Mapping & AB tools: Rehost mapping/AB overlays, contour planners, and preset selectors so guidance plugins can inject custom layers into the map canvas and inspector panels.
3. Device, IO & connectivity plugins:
   - Device Manager, GNSS/IMU fusion, and Telemetry Logging: Restore dashboard tiles, device health popouts, and log export flows found in the Dev branch dashboards, making sure health indicators reuse diagnostics theme tokens.
   - AgIO/Radio/NTRIP/ISOBUS: Bring over link configuration dialogs, connection testers, and live status panes so transport plugins expose parity UI for provisioning and troubleshooting.
   - File IO & Replay: Refresh import/export, replay selection, and timeline scrubbing dialogs so automation plugins surface consistent workflow affordances.
4. Agronomic analytics plugins:
   - Planter Monitor, Combine Yield, Crop, Genetics, Cost/Profit: Rebuild charts, tabular summaries, and alert stacks leveraging the metadata-driven dashboard components introduced in Phases 2–3.
   - Field Health & Weather: Port map overlays, time-series panels, and alert drawers, ensuring colour tokens respect ADR-034 accessibility constraints.
   - Report Builder & Job Tasks: Restore report templating previews, task checklists, and automation toggles from the existing Avalonia prototypes.
5. Companion/Video surfaces: Rebuild the video monitor dialog with device enumeration, feed selection, and layout docking while wiring Companion metadata snapshots so plugins can light up on the companion experience.
6. Deliverable: plugin UI bundle covering guidance, sections/rate, device & transport, agronomic analytics, video/companion, and workflow automation plugins operating inside Nexus with manifest-driven registration.
7. Reference plan: [`docs/plans/ui-phase5-plugin-surfaces.md`](ui-phase5-plugin-surfaces.md).

## Hardware & Diagnostics (Phase 6 — NX-416)
1. Port GPS data, UDP status, AgIO loop/serial/advanced settings dialogs from legacy sources and align them with diagnostics injection points.
2. Integrate telemetry streams from `TelemetryPrivacyViewModel`, `DeviceManagerCompatibilityViewModel`, and related diagnostics view-models to surface health states consistent with NX-309/NX-311 cards.
3. Add log/event viewer overlays and ensure they respect the metadata style tokens and focus order requirements.
4. Deliverable: diagnostics workspace supporting telematics troubleshooting and AgIO configuration.

## Simulation & Companion Parity (Phase 7 — NX-417)
1. Hook simulator main dialog and simulation control surfaces into the Sim Bar, reusing scenario editor assets and backlog acceptance criteria.
2. Generate `CompanionMetadataSnapshot` exports for every new panel and validate them against the metadata-driven parity tests (NX-301 harness).
3. Build Storybook/preview harnesses for each control using the sample view-models in `Aog.UI.Avalonia` README so designers can iterate without running the full stack.
4. Deliverable: simulation flows and companion parity automation covering all new UI components.

## Documentation, QA & Release Readiness (Phase 8 — NX-418)
1. Update docs with screenshots and operator guides referencing the new UI surfaces, using the placeholders in `artifacts/ui-screenshots/README.md` as the outline.
2. Extend automated UI tests to cover dialog open/close flows, menu accelerators, and plugin widget commands.
3. Refresh release notes and the support knowledge base to point to new workflows and bridging steps from legacy shells.
4. Deliverable: documentation pack, automated regression suite updates, and release readiness checklist for the UI overhaul.

## Risk & Dependency Tracking
- **Licensing:** Validate GPL obligations before copying code; document relicensing requirements per the license checklist.
- **Plugin contracts:** Coordinate with plugin owners if UI changes require manifest or capability updates.
- **Performance:** Benchmark map canvas and diagnostics dialogs against V6 to ensure rendering parity.
- **Accessibility:** Validate colour contrast and focus order to maintain compliance with ADR-034 guidance.

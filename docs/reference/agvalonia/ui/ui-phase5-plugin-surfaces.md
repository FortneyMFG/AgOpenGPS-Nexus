# NX-415 Plugin UI Surfaces (Guidance, Device, Analytics, Video)

## Objectives
- Provide manifest-driven hosts for plugin-owned UI surfaces enumerated in [`../artifacts/ui-to-plugin.yaml`](../artifacts/ui-to-plugin.yaml).
- Recreate legacy workflows for guidance, rate control, device connectivity, agronomic analytics, and companion/video experiences using Avalonia components.
- Ensure plugin surfaces share metadata-driven styling, localization, and telemetry patterns established in earlier phases.

## Host Infrastructure
- Implement a plugin surface registry that discovers controls via manifest metadata (`toolbar.*`, `workspace.*`, `dialog.*`).
- Expose lifecycle hooks so plugins can register/unregister panes at runtime while respecting shell layout persistence.
- Provide capability negotiation: surfaces declare required contracts; the host validates availability before rendering.
- Add diagnostics instrumentation for plugin load failures, rendering exceptions, and telemetry integration.

## Guidance & Steering Family

### Section Control & Variable Rate
- Port `section_control_widget` and `section_config_dialog` with layout cues from V6 WinForms (`FormSection.cs`) and Dev experiments.
- Integrate with `SectionControlService` for realtime enable/disable commands, coverage previews, and manual overrides.
- Surface per-section health indicators and variable-rate map overlays using metadata-driven dashboards.
- Provide calibration/status history tables referencing `section_control.v1` contract expectations.

### Autosteer & Guidance Diagnostics
- Rebuild `steer_settings_dialog`, calibration wizard, and path tuning panels; align with `autosteer.v1` contract.
- Hook into `GuidanceDiagnosticsViewModel` to expose lookahead, error metrics, and controller gain charts.
- Support offline simulation mode so testers can evaluate behaviours without vehicle hardware.

### Mapping & AB Tools
- Host mapping overlays (contour planners, preset selectors) allowing guidance plugins to inject custom layers.
- Provide map toolbar hooks for plugin commands (e.g., AB line creation, boundary smoothing) with undo/redo integration.

## Device, IO, and Connectivity Plugins
- Restore Device Manager dashboards, GNSS/IMU fusion panels, and telemetry logging dialogs.
- Port AgIO, Radio, NTRIP, and ISOBUS configuration dialogs with live connection testers and status banners.
- Align diagnostics theming with tokens from `ui-theme-tokens.json` and leverage the telemetry privacy view-models built in NX-309/NX-311.
- Ensure configuration changes surface review prompts and persist through the shared settings infrastructure from NX-414.

## Agronomic Analytics & Reporting
- Rebuild planter monitor, combine yield, crop, genetics, cost/profit, field health, weather, and report builder surfaces.
- Use metadata-driven dashboard components (charts, tables, alerts) introduced in NX-297–NX-307 for consistent presentation.
- Provide data sample generators for Storybook previews and documentation captures.
- Validate plugin manifests expose required metrics/capabilities before rendering analytics panels.

## Companion & Video Experiences
- Port the video monitor dialog with device enumeration, feed selection, and layout docking.
- Integrate with Companion metadata snapshots to ensure plugin surfaces light up in the mobile companion apps.
- Provide bandwidth/latency indicators for remote streaming scenarios.

## Testing & Verification
- Expand automated UI tests to mount representative plugins and exercise toolbar/dialog/workspace injection.
- Simulate plugin lifecycle events (load, unload, capability change) ensuring host gracefully updates the UI.
- Capture golden screenshots for each plugin family and document QA sign-off in `tasks.md` once surfaces are validated on hardware or realistic sim.

## Deliverables
- Manifest-driven registry services, Avalonia hosts, and plugin sample controls covering all families listed above.
- Updated documentation outlining plugin integration points and extension guidance.
- Backlog housekeeping to mark corresponding entries complete in `../artifacts/ui-backlog.json` and plugin manifests.

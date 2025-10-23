# AutoSteer Plugin

## Overview
The AutoSteer plugin implements Nexus’ closed-loop steering controller. It translates guidance tracks and pose input into steering commands, applies adaptive tuning derived from machine profiles, and publishes telemetry so operators can validate behaviour in replay.

## Capabilities
- `guidance.control` (exclusive lease) for publishing `SteerCmd` messages.
- `guidance.telemetry` for reporting controller state and tuning events.
- Simulation providers for replaying steering sessions in deterministic tests.

## Core Integration
- Primary entry points live under `Nexus SourceCode/src/Aog.Plugins/AutoSteer/`. `AutoSteerLiteController.cs` houses the controller state machine, while `AutoSteerLiteTuning.cs` and `AutoSteerLiteTuningCalculator.cs` derive machine-specific gains.
- The plugin consumes pose and track data via the core event bus (`SteerCmd`, `SteerState`, and `GuidanceTrack` contracts).
- Tests in `tests/Aog.Plugins.Tests/AutoSteer/` enforce parity with the legacy controller and validate tuning calculations across machine profiles.

## UI Integration
- Exposes dashboard metrics and controls through the Steer panel (`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SteerPanelViewModel.cs`) and the steering dashboard (`SteerDashboardViewModel`). Zip packaging will register these surfaces via `IBlockProvider` and `IWindowProvider`.

## Dependencies
- Requires the Guidance plugin to supply tracks and the Mapping plugin for coverage-derived constraints.
- Declares telemetry/logging capabilities so Telemetry Logging can capture steering sessions.

## Packaging Notes
- Manifest should expose both `core` and `ui` entry points so the plugin can register blocks and toolbars.
- Include calibration icons in `assets/` for toolbar buttons if packaging separately.
- Ensure the manifest requests the `sections.control` capability only when headland avoidance integration is enabled.

## Related Resources
- `docs/Plugins/briefs/Guidance.md` for upstream data flow.
- `docs/development/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md` for how steering interacts with variable rate overlays.
- `docs/Plugins/tutorials/testing.md` for guidance on replay-based validation.


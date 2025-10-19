# Sections Control Plugin

## Overview
The Sections plugin automates boom/section enablement based on coverage, speed, and operator overrides. It enforces headland boundaries, integrates with rate control, and publishes mask telemetry for downstream consumers.

## Capabilities
- `sections.control` exclusive capability controlling the section mask.
- `sections.telemetry` shared capability for publishing mask history and diagnostics.
- Optional `rate.control` capability when bundled with rate automation.

## Core Integration
- `SectionIoOrchestrator.cs` computes masks and publishes `SectionMask` messages to the event bus. It honours constraint gates and fails safe when leases are revoked.
- `SectionMaskCalculator.cs` encapsulates mask calculation logic (speed gating, look-ahead, manual overrides).
- `VariableRateController.cs` and `VariableRateTransportGuard.cs` coordinate with rate control transports when variable rate is active.
- Tests in `tests/Aog.Plugins.Tests/Sections/` cover mask transitions, constraint handling, and regression fixtures.

## UI Integration
- Provides the Sections panel (`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/SectionsPanelViewModel.cs`) and toolbar commands for master toggles and manual overrides.
- Registers status indicators showing active sections and safe masks.
- Supplies configuration dialogs (planned) for section mapping and latency tuning.

## Dependencies
- Depends on Mapping for coverage inputs and on Guidance for headland planning.
- Coordinates with Variable Mapping when applying rate-based mask constraints.

## Packaging Notes
- Manifest must declare the exclusive `sections.control` lease with an appropriate timeout and recovery strategy.
- Include default section layouts under `assets/sections/` for quick configuration.

## Related Resources
- `docs/plugins/official/AutoSteer.md` and `Guidance.md` for upstream signals.
- `docs/plugins/official/VariableMapping.md` for rate-aware masking.
- `docs/plugins/tutorials/testing.md` describes how to replay mask telemetry in tests.


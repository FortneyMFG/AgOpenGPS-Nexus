# Variable Mapping Plugin

## Overview
The Variable Mapping plugin manages prescription maps, variable rate strategies, and zone analytics. It coordinates imports, exports, and live rate adjustments based on agronomic models.

## Capabilities
- `variable.mapping` shared capability for maintaining rate zones and prescriptions.
- `variable.export` shared capability for exporting updated prescriptions.
- `rate.hints` shared capability for advising Sections and Rate Control on optimal rates.

## Core Integration
- `VariableMappingService.cs` handles ingestion, reconciliation, and publication of rate zones. It exposes APIs for other plugins to query current recommendations.
- `PrescriptionExportPipeline.cs` converts internal representations back into industry-standard formats for controllers and advisors.
- The service publishes rate hints to the event bus, allowing rate controllers to adjust application rates in real time.
- Unit tests should cover zone interpolation, conflict resolution, and export round-tripping.

## UI Integration
- Provides map overlays for rate zones and dashboards summarising application performance.
- Registers configuration dialogs where operators can adjust prescription parameters and simulation tools for scenario planning.
- Integrates with the toolbar to toggle variable rate mode and with the status strip to report active prescriptions.

## Dependencies
- Depends on Mapping for spatial context, Crop for agronomic targets, and Sections/Rate Control for executing recommendations.
- Uses File IO for import/export workflows.

## Packaging Notes
- Manifest should declare optional dependencies on Crop and Field Health to reflect enhanced analytics when those plugins are enabled.
- Bundle sample prescriptions under `assets/prescriptions/` for regression testing and onboarding.

## Related Resources
- `docs/plugins/official/Sections.md` and `TelemetryLogging.md` for downstream integration.
- `docs/plugins/official/Crop.md` for agronomic context.
- ADRs on layer registry and variable rate architecture.


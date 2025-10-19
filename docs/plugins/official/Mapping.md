# Mapping Plugin

## Overview
The Mapping plugin is the cornerstone of spatial data management in Nexus. It records coverage, manages field state, and serves layers to the map host and downstream analytics plugins.

## Capabilities
- `mapping.coverage` shared capability for publishing coverage updates.
- `mapping.layers` shared capability for registering map layers.
- `mapping.state` shared capability providing canonical field state snapshots.

## Core Integration
- `FieldStateStore.cs` tracks boundaries, guidance lines, and layer metadata per field.
- `CoverageFeedPublisher.cs` streams coverage updates through the event bus to keep dashboards and logging in sync.
- `PlanarPointExtensions.cs` provides geometry helpers used by other mapping-aware plugins.
- Simulation providers (planned) will allow deterministic replays of coverage streams.

## UI Integration
- Works with the map host (`IMapHost`) to register layers and overlays.
- Supplies status indicators summarising coverage percentage and pass counts.
- Provides the layer legend and inspector view-models with data (see `LayerLegendViewModel` and `LayerInspectorViewModel`).

## Dependencies
- Forms the foundation for Sections, Variable Mapping, and Combine Yield plugins.
- Interacts with Job Tasks to reset field state when a new job starts.

## Packaging Notes
- Manifest must declare compatibility expectations for spatial coordinate systems and simulation providers.
- Consider bundling sample fields under `assets/fixtures/` to facilitate integration testing.

## Related Resources
- `docs/plugins/Mapping.md` (legacy deep-dive) and ADRs referencing layer registry design.
- `docs/plugins/official/Sections.md`, `VariableMapping.md`, and `CombineYield.md` rely heavily on mapping outputs.
- Replay documentation (`docs/plugins/Replay.md`) explains how mapping data participates in simulations.


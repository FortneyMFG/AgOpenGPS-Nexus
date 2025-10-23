# Guidance Plugin

## Overview
The Guidance plugin produces guidance tracks, lane plans, and turn strategies used by AutoSteer and Section Control. It fuses boundary data, machine geometry, and operator preferences to keep vehicles on-track across headlands and interior passes.

## Capabilities
- `guidance.plan` shared capability for generating guidance lanes.
- `guidance.turn` shared capability for headland and turn planning.
- Optional `simulation.guidance` when contributing deterministic simulation providers.

## Core Integration
- `GuidanceLanePlanner.cs` generates parallel tracks aligned with field boundaries.
- `GuidanceTurnPlanner.cs` computes headland turn arcs and U-turn strategies.
- The plugin publishes guidance plans onto the event bus, where AutoSteer consumes them and Section Control uses them to pre-empt headland transitions.
- Tests should verify planner outputs against representative boundary fixtures.

## UI Integration
- Contributes guidance controls to the top toolbar and block layout (e.g., toggling AB lines, headland modes).
- Registers overlay layers that visualise the active guidance plan in the map host.
- Provides dialogs for managing AB lines and guidance presets when packaged as a zip.

## Dependencies
- Depends on Mapping for boundary data and AutoSteer/Sections for downstream consumption.
- Works with Field Operations (boundary editor) to update lane plans when boundaries change.

## Packaging Notes
- Manifest must declare exclusive leases if the plugin owns guidance planning; collaborative scenarios can use shared leases alongside advisory plugins.
- Bundle sample field boundary fixtures under `assets/fixtures/` for testing.

## Related Resources
- `docs/Plugins/AutoSteer.md` for downstream consumers.
- `docs/Plugins/official/Sections.md` for how headland planning interacts with sections.
- ADRs on guidance algorithms provide theoretical background and tuning parameters.


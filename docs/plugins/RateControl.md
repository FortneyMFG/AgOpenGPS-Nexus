# Rate & Section Control Plugin Requirements (Draft)

## Overview

Rate and section control plugins manage product application across implements. Multi-field jobs and session metadata require additional context for deterministic control and audit trails.

## Runtime Contracts

<<<<<<< HEAD
- Subscribe to `onFarmLoaded`, `onSeasonLoaded`, `onJobLoaded`, and `onSessionStart` so controller state machines preload farm assets, job field geometry, and session metadata before enabling sections. Core context includes `seasonId?`, authoring metadata, and plugin `extensions` for agronomic overlays.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L18-L40】
- Receive `jobId`, `sessionId`, and `fieldIds[]` on initialization and when sessions change. Use this context to align coverage logging, implement leases, and automated shutoff policies.
- Respect multi-field envelopes provided by the mapping plugin to avoid unintended application when crossing field boundaries.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L21-L63】
- Consume planned layers (`vr.planned.*`) and as-applied outputs registered in the Layer Registry. Planned vs. actual comparisons must reference catalog metadata to ensure analytics and ISOXML exports remain consistent.【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L10-L106】
=======
- Subscribe to `onFarmLoaded`, `onSeasonLoaded`, `onJobLoaded`, and `onSessionStart` so controller state machines preload farm assets, job field geometry, and session metadata before enabling sections. Core context includes `seasonId?`, authoring metadata, and plugin `extensions` for agronomic overlays.【F:docs/SRS/sections/6X/62_Job_Lifecycle.md†L18-L40】
- Receive `jobId`, `sessionId`, and `fieldIds[]` on initialization and when sessions change. Use this context to align coverage logging, implement leases, and automated shutoff policies.
- Respect multi-field envelopes provided by the mapping plugin to avoid unintended application when crossing field boundaries.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L21-L63】
- Consume planned layers (`vr.planned.*`) and as-applied outputs registered in the Layer Registry. Planned vs. actual comparisons must reference catalog metadata to ensure analytics and ISOXML exports remain consistent.【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L33-L58】【F:docs/SRS/sections/7X/72_Mapping_Layers_Plugin.md†L10-L106】
>>>>>>> origin/develop
- Controllers can reuse the new `VariableRateController` helper to translate registry-backed layer cells into section setpoints while respecting configured minimum/maximum rates and defaults.【F:Nexus SourceCode/src/Aog.Plugins/Sections/VariableRateController.cs†L1-L103】
- Emit coverage/rate layers tagged with `jobId` and `sessionId`; append provenance records describing rate tables, variable maps, section events, and operator overrides.【F:schemas/Layer.v1.json†L1-L117】
- Integrate with ISOXML import/export flows so TaskData generation references session hashes, recipe IDs, and layer IDs per ADR-014. Exported jobs must remain round-trip safe for NX-165.【F:docs/ADR/ADR-014-interop-prescription-formats.md†L12-L56】
- Read/write `session.extensions` for plugin-specific telemetry (e.g., product lot tracking, profitability) while preserving core session fields.【F:schemas/Session.v1.json†L1-L115】

## Session-Aware Outputs

- When journaling section states, include session metadata (start/end timestamps, operator notes) to support compliance reports.
<<<<<<< HEAD
- Autosave changes whenever rate presets, material inputs, or environment data shift beyond configured thresholds; align with session autosave cadence for consistent recovery.【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L37-L74】
=======
- Autosave changes whenever rate presets, material inputs, or environment data shift beyond configured thresholds; align with session autosave cadence for consistent recovery.【F:docs/SRS/sections/6X/62_Job_Lifecycle.md†L37-L74】
>>>>>>> origin/develop
- Expose hooks for `onSessionEnd` to flush ISOXML tasks, coverage summaries, and rate diagnostics so report builders can assemble crop and profit summaries.

## UX Integration

- Display active session name, environment snapshot, and per-field coverage percentages in control panels to help operators validate product usage across fields.
- Provide controls to start a new session directly from the rate panel; Core handles lifecycle events but plugins should surface state transitions.
<<<<<<< HEAD
- Surface advisory `noWorkMask` warnings without blocking automation, following the control arbiter guidance in SRS §09.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L33-L60】
=======
- Surface advisory `noWorkMask` warnings without blocking automation, following the control arbiter guidance in SRS §09.【F:docs/SRS/sections/6X/61_Kinematics_Pose_Fusion.md†L33-L60】
>>>>>>> origin/develop

## Compatibility Notes

- Plugins relying on a single field must update; Core will pass all mounted fields and expect per-field rate totals.
- Replace any “Run” terminology with “Session” in logs, tooltips, and exported files for operator clarity.
- ISOXML bridges must reference the same recipe hashes and layer IDs recorded in the Layer Registry; mismatches block export until corrected.

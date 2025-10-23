# Field Health Risk Layer Registry Entries (Draft)

## Status
Draft — aligns with NX-613 and ADR-052 field health governance.

## Purpose
Field health overlays share a common schema but represent multiple registry
entries. This reference enumerates the sanctioned identifiers so mapping,
analytics, and QA tooling can negotiate consistent metadata during the registry
handshake.

## Layer catalog

| Layer ID | Schema ID | Planned | Requires session | Summary |
| --- | --- | --- | --- | --- |
| `risk.flood` | `https://agopengps.org/schemas/FieldHealthRiskLayer.v1.json` | ❌ | ✅ | Flood-prone zones identified during scouting or import workflows. |
| `risk.compaction` | `https://agopengps.org/schemas/FieldHealthRiskLayer.v1.json` | ❌ | ✅ | Soil compaction risk from telemetry, scouting notes, or lab reports. |
| `risk.weeds` | `https://agopengps.org/schemas/FieldHealthRiskLayer.v1.json` | ❌ | ✅ | Weed pressure overlays captured during agronomy passes. |

## Registry notes

- **Shared schema.** All risk overlays use `FieldHealthRiskLayer.v1` and rely on
  `x-nexus-layerIds` metadata so registry automation can map each identifier to
  the same payload structure.【F:schemas/FieldHealthRiskLayer.v1.json†L1-L36】
- **Session context.** Risk overlays are tied to specific scouting or survey
  sessions; registry consumers must ensure the session hash matches the overlay
  provenance before streaming telemetry.【F:schemas/examples/FieldHealthRiskLayer.sample.json†L1-L56】

## References
- [ADR-052 — Field Health Plugin](../../development/SRS/sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md)
- [Layer registry overview](../../development/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Registry_Contracts.md)

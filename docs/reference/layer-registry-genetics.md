# Genetics Layer Registry Entries (Draft)

## Status
Draft — aligns with NX-201 and ADR-046 genetics plugin contracts.

## Purpose
The genetics plugin contributes two canonical layer definitions to the Nexus Layer Registry:
`genetics.plan` for pre-job hybrid planning and `genetics.variety` for as-applied tracking.
This reference captures their schema bindings, provenance expectations, and session
requirements so controllers, UI overlays, and export tooling can negotiate consistent
metadata via the registry handshake.

## Layer catalog

| Layer ID | Schema ID | Planned | Requires session | Summary |
| --- | --- | --- | --- | --- |
| `genetics.plan` | `https://agopengps.org/schemas/GeneticsPlan.v1.json` | ✅ | ❌ | Planned hybrid/variety strips authored before execution. Includes seed brand, product, trait stack, lot, treatment, and planner notes for each geometry. |
| `genetics.variety` | `https://agopengps.org/schemas/GeneticsVariety.v1.json` | ❌ | ✅ | As-applied hybrid/variety records captured during planting. Adds session binding, barcode payloads, and change-log provenance for lot/treatment swaps. |

## Registry notes

- **Planned vs. actual.** The genetics schemas now embed `x-nexus-layerId` and
  `x-nexus-planned` annotations so registry automation can classify plan vs. actual
  payloads without inferring from identifiers.【F:schemas/GeneticsPlan.v1.json†L1-L46】【F:schemas/GeneticsVariety.v1.json†L1-L51】
- **Session awareness.** `genetics.variety` entries must include `sessionId`, matching the
  ADR-041 session lifecycle, while `genetics.plan` remains job-scoped. Controllers should
  advertise `requiresSession=true` for the variety layer during the registry handshake.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md†L17-L52】【F:docs/reference/layer-registry-handshake.md†L8-L64】
- **Change logs.** Barcode or lot updates recorded in `changeLog[]` enable downstream
  analytics to reconcile mid-session swaps and support audit reports referencing
  `LayerEditEvent` journals.【F:schemas/GeneticsVariety.v1.json†L52-L108】【F:docs/plugins/Genetics.md†L9-L34】

## References
- [ADR-046 — Genetics Plugin & Layers](../SRS/sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md)
- [Layer Registry Hash Handshake (Draft)](layer-registry-handshake.md)
- [Genetics Plugin Requirements](../plugins/Genetics.md)

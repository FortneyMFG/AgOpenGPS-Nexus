# Economics Layer Registry Entries (Draft)

## Status
Draft — aligns with NX-613 and ADR-050 cost/profit analytics.

## Purpose
Profit analytics extends the Layer Registry with canonical identifiers so Core,
plugins, and QA harnesses can negotiate the same metadata when rendering profit
overlays or verifying provenance. This reference captures the authoritative
layer IDs backed by `ProfitLayer.v1` and links to the governing ADRs for
traceability.

## Layer catalog

| Layer ID | Schema ID | Planned | Requires session | Summary |
| --- | --- | --- | --- | --- |
| `profit.net` | `https://agopengps.org/schemas/ProfitLayer.v1.json` | ❌ | ✅ | Net profit per unit area derived from cost records and session-aligned yield inputs. |

## Registry notes

- **Session alignment.** Profit overlays include the session identifier when
  derived from live application data so Report Builder exports and QA harnesses
  can replay the exact inputs.【F:schemas/ProfitLayer.v1.json†L1-L36】【F:docs/development/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L11-L46】
- **Actual layer semantics.** `x-nexus-actual=true` signals that the layer
  represents observed profitability; registry automation and dashboards avoid
  classifying it as a planned prescription.【F:schemas/ProfitLayer.v1.json†L1-L36】

## References
- [ADR-050 — Cost & Profit Plugin](../../development/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md)
- [Layer registry overview](../../development/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Registry_Contracts.md)

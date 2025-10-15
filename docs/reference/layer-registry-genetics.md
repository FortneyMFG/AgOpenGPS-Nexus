# Genetics Layer Registry (Draft)

## Status
Draft — aligns with NX-201 and ADR-046 genetics plugin contracts.【F:docs/ADR/ADR-046_GeneticsPlugin.md†L9-L66】

## Layer definitions

| Layer ID | Planned? | Requires session? | Geometry | Schema | Key attributes | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| `genetics.plan` | Yes | No | Vector polygons | [`GeneticsPlan.v1`](../../schemas/GeneticsPlan.v1.json) | `brand`, `product`, `traitStack`, `lot`, `treatment`, `source`, `notes` | Pre-job plans linked to optional jobs; registry metadata marks the layer as planned so dashboards group it with planning overlays.【F:schemas/GeneticsPlan.v1.json†L1-L80】【F:docs/plugins/Genetics.md†L10-L24】 |
| `genetics.variety` | No | Yes | Vector polygons | [`GeneticsVariety.v1`](../../schemas/GeneticsVariety.v1.json) | `brand`, `product`, `traitStack`, `lot`, `treatment`, `source`, `barcode`, `changeLog`, `appliedAt`, `sessionId` | Captures applied varieties during live coverage with session provenance, barcode history, and change logs for audit trails.【F:schemas/GeneticsVariety.v1.json†L1-L120】【F:docs/ADR/ADR-046_GeneticsPlugin.md†L21-L66】 |

## Registry metadata & presentation

- **Units & display.** Genetics layers use categorical palettes; UI shells read the registry entry to render variety legends and trait stack labels without hard-coded lists.【F:docs/ADR/ADR-010-layer-registry-variable-rate.md†L29-L53】【F:docs/plugins/Genetics.md†L10-L32】
- **Planned vs. actual flags.** `x-nexus-planned` annotations drive planned/actual grouping in dashboards and exports so operators can toggle between intended plans and applied results.【F:schemas/GeneticsPlan.v1.json†L1-L32】【F:schemas/GeneticsVariety.v1.json†L1-L40】
- **Session awareness.** `genetics.variety` entries require active sessions to guarantee provenance includes `sessionId`, `appliedAt`, and barcode context per ADR-041 session lifecycle requirements.【F:schemas/GeneticsVariety.v1.json†L1-L64】【F:docs/SRS/sections/03_JobLifecycle.md†L18-L66】

## Provenance & interoperability expectations

1. **Hash handshake.** Genetics controllers publish registry hashes before emitting data so Core validates bindings against the authoritative catalog, preventing drift with crop or yield analytics.【F:docs/reference/layer-registry-handshake.md†L1-L74】
2. **Change logging.** Actual variety updates append change log entries with actor, timestamps, and previous/current values for compliance exports and replay tooling.【F:schemas/GeneticsVariety.v1.json†L70-L120】
3. **Context linkage.** Plan records reference jobs when available, while actual layers link jobs and sessions to align with crop context, profitability analytics, and export pipelines.【F:schemas/GeneticsPlan.v1.json†L8-L64】【F:schemas/GeneticsVariety.v1.json†L16-L64】【F:docs/ADR/ADR-050_CostProfitPlugin.md†L19-L56】

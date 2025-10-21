# Weather Overlay Layer Registry Entries (Draft)

## Status
Draft — aligns with NX-613, ADR-053 weather overlays, and ADR-011 visualization
caching guidance.

## Purpose
Weather overlays reuse a shared schema for multiple variables. Documenting the
registry identifiers keeps renderer caches, QA automation, and export tooling in
sync while the contracts mature.

## Layer catalog

| Layer ID | Schema ID | Planned | Requires session | Summary |
| --- | --- | --- | --- | --- |
| `weather.temp` | `https://agopengps.org/schemas/WeatherOverlay.v1.json` | ❌ | ❌ | Temperature raster snapshots sourced from weather stations or partner APIs. |
| `weather.wind` | `https://agopengps.org/schemas/WeatherOverlay.v1.json` | ❌ | ❌ | Wind speed/direction overlays consumed by application rate guards. |
| `weather.rain` | `https://agopengps.org/schemas/WeatherOverlay.v1.json` | ❌ | ❌ | Rainfall accumulation grids that inform scouting and profitability rollups. |
| `weather.humidity` | `https://agopengps.org/schemas/WeatherOverlay.v1.json` | ❌ | ❌ | Relative humidity overlays paired with risk analytics. |
| `weather.pressure` | `https://agopengps.org/schemas/WeatherOverlay.v1.json` | ❌ | ❌ | Barometric pressure overlays used by fusion and transport QA scenarios. |

## Registry notes

- **Renderer cache alignment.** `WeatherOverlay.v1` surfaces `x-nexus-layerIds`
  so renderer caches can index imagery by layer ID instead of ad-hoc naming
  conventions, matching ADR-011 guidance.【F:schemas/WeatherOverlay.v1.json†L1-L36】【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-011 - Mapping and visualization imagery pipeline.md†L1-L52】
- **Fusion provenance.** Weather overlays participate in prescription fusion
  workflows; provenance must include the overlay hash so QA harnesses confirm the
  same dataset fed ADR-013 derivations.【F:schemas/examples/WeatherOverlay.sample.json†L1-L31】【F:docs/SRS/sections/3X_Data_Storage/32-ADR-013 - Derived products and prescription analytics.md†L11-L42】

## References
- [ADR-011 — Mapping & Imagery Visualization](../SRS/sections/9X_Frontends_Ops/91-ADR-011 - Mapping and visualization imagery pipeline.md)
- [ADR-053 — Weather Plugin](../SRS/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md)
- [Layer registry overview](../SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md)

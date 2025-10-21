# 71 — Mapping Kernel Contracts (Status: drafting)

This section summarizes the planned gRPC surface area for Nexus Core along with the authoritative layer catalog under the Layer Registry. It consolidates decisions from ADR-009/010 and the session/season job stack (ADR-030/040/041/043) so plugin authors can target the same identifiers, schemas, and lifecycle hooks while the contracts remain under active drafting.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L1-L40】【F:docs/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L1-L34】【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L1-L61】【F:docs/SRS/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L1-L44】【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L1-L60】【F:docs/SRS/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L12-L68】

## Core gRPC Services

| Service | Purpose | Status | Notes |
| --- | --- | --- | --- |
| `aog.core.FarmService` | Create, list, and load farm records backed by `Farm.v1` documents. | Reserved (schema defined, service pending) | Farm schema governs identifiers, assets, and plugin extensions until service scaffolding lands.【F:schemas/Farm.v1.json†L1-L61】 |
| `aog.core.FieldService` | Manage field geometry, headlands, and shared assets anchored to `Field.v1`. | Reserved (schema defined, service pending) | Field documents capture geometry references consumed by job envelopes and session context.【F:schemas/Field.v1.json†L1-L77】【F:docs/SRS/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L12-L68】 |
| `aog.core.SeasonService` | Organize jobs into optional seasons and broadcast context to plugins. | Drafting (ADR-040) | Season lifecycle, context fan-out, and extension handling defined in ADR-040 while service contract is drafted.【F:docs/SRS/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L1-L60】 |
| `aog.job.JobService` | Create, mount, resume, and close jobs with lifecycle hooks. | Proposed (ADR-030) | gRPC verbs for New/Resume/Drive-In plus autosave/journaling semantics are specified in ADR-030.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L1-L66】 |
| `aog.job.SessionService` | Start/end sessions, append notes, inputs, and weather snapshots. | Drafting (ADR-041) | Session payload, lifecycle events, and metadata expectations documented in ADR-041 and backed by `Session.v1` schema.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L1-L60】【F:schemas/Session.v1.json†L1-L84】 |
| `aog.layer.LayerService` | Enumerate layer definitions, read/write layer metadata, and orchestrate provenance. | Drafting (ADR-010) | LayerDefinition governance, validator expectations, and provenance hashing live in ADR-010 and `Layer.v1`.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L11-L40】【F:schemas/Layer.v1.json†L1-L73】 |
| `aog.layer.TileStoreService` | Chunked binary tile upload/download aligned with ADR-009 persistence. | In Review (ADR-009) | TileStore chunking, transforms, and crash safety defined in ADR-009; service scaffolding aligns with those guarantees.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L11-L45】 |
| `aog.telemetry.TelemetryService` | Publish GNSS/IMU/rate feeds and subscribe to diagnostics. | Drafting (Telemetry mesh ADRs) | Telemetry QoS, diagnostics, and mesh replication tracked in telemetry SRS and ADR-047 while gRPC contract is drafted.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L1-L44】【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L1-L66】 |
| `aog.machine.DeviceService` | Enumerate connected implements, ECUs, and health reports. | Drafting (Capabilities/Device schemas) | Device schema anchors IDs and health payloads ahead of gRPC scaffolding.【F:schemas/Device.v1.json†L1-L80】 |
| `aog.machine.MultiMachineService` | Coordinate presence and telemetry mesh participation across rigs. | Drafting (ADR-047) | Live telemetry mesh topics, QoS, and ACLs defined in ADR-047 inform this service.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L21-L71】 |
| `aog.analytics.AnalyticsService` | Run summaries/derivations (yield, profitability, VR QA). | Drafting (ADR-049/050) | Analytics APIs and rollup expectations live in ADR-049 (yield) and ADR-050 (profit).【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L1-L56】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L1-L56】 |
| `aog.file.FileIOService` | Import/export job artifacts (ISOXML, GeoJSON, CSV). | Proposed (ADR-030 follow-up) | Job import/export verbs mandated by ADR-030 with schema normalization before activation.【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L14-L38】 |
| `aog.map.ZoneService` | Edit/draw geometry features using the Zone Drawing Framework. | Drafting (ADR-044) | Zone edit events, undo/redo, and telemetry replication defined in ADR-044.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L21-L74】 |
| `aog.report.ReportService` | Generate PDF/CSV reports aligned with session/job provenance. | Drafting (ADR-051) | Report builder data contracts, sections, and provenance hooks covered in ADR-051.【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L1-L68】 |

### Supporting Schemas

- `Farm.v1`, `Field.v1`, and `Season.v1` documents supply the authoritative metadata for Farm/Field/Season services and preserve plugin extension bags.【F:schemas/Farm.v1.json†L1-L61】【F:schemas/Field.v1.json†L1-L77】【F:schemas/Season.v1.json†L1-L68】
- `Job.v1` anchors job identity, field membership, session references, and plugin extensions referenced by the JobService lifecycle.【F:schemas/Job.v1.json†L1-L76】
- `Session.v1` enumerates environment snapshots, weather updates, inputs, notes, and layer references required by SessionService.【F:schemas/Session.v1.json†L1-L84】

## Layer Catalog Summary

Layer identifiers follow the Layer Registry governance described in ADR-010 with planned families owned by dedicated plugins (ADR-045–ADR-053). Status reflects the governing ADR state (Drafting, In Review, etc.) or reserved namespaces awaiting implementation.

### Variable Rate / Agronomic

| Layer ID | Description | Units | Type | Status |
| --- | --- | --- | --- | --- |
| `vr.planned.seed` | Commanded seeding rate tiles. | seeds/acre | Planned rate | Drafting (ADR-010/013) |
| `vr.actual.seed` | Actual seeding rate tiles. | seeds/acre | Actual rate | Drafting (ADR-010/013) |
| `vr.planned.n` | Nitrogen prescription. | lb/acre | Planned rate | Drafting (ADR-010/013) |
| `vr.actual.n` | Actual nitrogen applied. | lb/acre | Actual rate | Drafting (ADR-010/013) |
| `vr.planned.p` | Phosphorus prescription. | lb/acre | Planned rate | Drafting (ADR-010/013) |
| `vr.actual.p` | Actual phosphorus applied. | lb/acre | Actual rate | Drafting (ADR-010/013) |
| `vr.planned.k` | Potassium prescription. | lb/acre | Planned rate | Drafting (ADR-010/013) |
| `vr.actual.k` | Actual potassium applied. | lb/acre | Actual rate | Drafting (ADR-010/013) |
| `vr.planned.chem` | Chemical rate plan. | oz/acre | Planned rate | Drafting (ADR-010/013) |
| `vr.actual.chem` | As-applied chemical rate. | oz/acre | Actual rate | Drafting (ADR-010/013) |
| `vr.planned.lime` | Lime prescription. | ton/acre | Planned rate | Drafting (ADR-010/013) |
| `vr.actual.lime` | Actual lime applied. | ton/acre | Actual rate | Drafting (ADR-010/013) |
| `vr.planned.cover` | Cover crop prescription. | lb/acre | Planned rate | Drafting (ADR-010/013) |
| `vr.actual.cover` | Actual cover crop applied. | lb/acre | Actual rate | Drafting (ADR-010/013) |

Variable-rate families remain governed by ADR-010 and ADR-013 analytics pipelines, which enforce registry hashes, provenance, and normalization behaviors.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L11-L40】【F:docs/SRS/sections/3X_Data_Storage/32-ADR-013 - Derived products and prescription analytics.md†L1-L42】

### Coverage & Work

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `coverage.worked` | Boolean worked area coverage. | bool | In Review (ADR-009) |
| `coverage.sectionstate` | Section on/off map. | bool | In Review (ADR-009) |
| `coverage.time` | Time spent over area. | seconds | In Review (ADR-009) |
| `coverage.speed` | Pass speed heatmap. | km/h | In Review (ADR-009) |

Coverage tiles, section state, and derived metrics are part of the TileStore persistence scope under ADR-009.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-009 - PoseStream vector logs and layer TileStore persistence.md†L11-L45】

### Crop Type & Genetics

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `cropType.planned` | Intended crop zones from planning. | crop string | Drafting (ADR-045) |
| `cropType.actual` | Actual planted crop geometry. | crop string | Drafting (ADR-045) |
| `cropType.history` | Prior crop history overlays. | crop string | Drafting (ADR-045) |
| `genetics.plan` | Planned hybrid/variety strips. | product ID | Drafting (ADR-046) |
| `genetics.variety` | Actual hybrid/variety planted. | product ID | Drafting (ADR-046) |

`cropType.planned` payloads store the crop name, target season (`year`), and fixed
`status="planned"` flag alongside optional variety notes and the authoring source so
Field history can trace provenance.【F:schemas/CropTypePlanned.v1.json†L8-L58】
`cropType.actual` requires the planted crop, `year`, `status="actual"`, and an explicit
`source` (manual, sensor, import, barcode) plus optional variety metadata for rotation and
analytics workflows.【F:schemas/CropTypeActual.v1.json†L8-L74】
`cropType.history` aggregates append-only records with links back to the originating job
and session plus the evidencing layer ID, keeping seasonal history immutable for audits
and replay.【F:schemas/CropTypeHistory.v1.json†L8-L58】 Field documents mirror these entries
through `CropTypeHistoryRecord.v1`, which now includes optional `jobId`/`sessionId`
references so reports and analytics can navigate directly to the source job bundle.【F:schemas/CropTypeHistoryRecord.v1.json†L1-L53】

Crop and genetics layers are owned by dedicated plugins in ADR-045 and ADR-046, sharing context via sessions and job extensions.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L1-L52】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-046 - Genetics Plugin & Layers.md†L1-L52】

### Yield & Harvest

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `yield.actual` | Yield at harvest. | bu/acre (kg/ha) | Drafting (ADR-049) |
| `yield.moisture` | Grain moisture. | % | Drafting (ADR-049) |
| `yield.testWeight` | Test weight. | lb/bu | Drafting (ADR-049) |
| `yield.flow` | Instantaneous mass flow. | lb/s | Drafting (ADR-049) |

Yield plugin responsibilities and analytics surfaces defined in ADR-049 drive these layers.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L1-L56】

### Profit & Economics

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `profit.cost` | Total input cost map. | USD/acre | Drafting (ADR-050) |
| `profit.revenue` | Gross revenue layer. | USD/acre | Drafting (ADR-050) |
| `profit.net` | Net profit after costs. | USD/acre | Drafting (ADR-050) |
| `cost.seed` | Seed cost allocation. | USD/acre | Drafting (ADR-050) |
| `cost.chem` | Chemical cost allocation. | USD/acre | Drafting (ADR-050) |
| `cost.fert` | Fertilizer cost allocation. | USD/acre | Drafting (ADR-050) |

Economic analytics and ProfitLayer schema requirements live in ADR-050. The
`ProfitLayer.v1` schema now advertises `x-nexus-layerId="profit.net"` and marks the
layer as actual so registry automation can link analytics and provenance without
manual catalog entries.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L11-L46】【F:schemas/ProfitLayer.v1.json†L1-L36】

### Weather & Environment

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `weather.temp` | Air temperature overlay. | °C | Drafting (ADR-053) |
| `weather.wind` | Wind speed/direction overlay. | m/s + ° | Drafting (ADR-053) |
| `weather.rain` | Rainfall accumulation. | mm | Drafting (ADR-053) |
| `weather.humidity` | Relative humidity. | % | Drafting (ADR-053) |
| `weather.pressure` | Barometric pressure overlay. | hPa | Drafting (ADR-053) |

Weather logging and overlay requirements defined in ADR-053 also populate
session weather snapshots via SessionService. `WeatherOverlay.v1` publishes the
canonical layer identifiers via `x-nexus-layerIds` so renderer caches and QA
harnesses can reason about every weather overlay consistently.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L1-L49】【F:schemas/Session.v1.json†L29-L84】【F:schemas/WeatherOverlay.v1.json†L1-L36】

### Soil & Lab Data (Reserved)

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `soil.ph` | Soil pH grid. | pH | Reserved namespace |
| `soil.om` | Organic matter. | % | Reserved namespace |
| `soil.p` | Phosphorus grid. | ppm | Reserved namespace |
| `soil.k` | Potassium grid. | ppm | Reserved namespace |
| `soil.n` | Nitrogen grid. | ppm | Reserved namespace |
| `soil.ec` | Electrical conductivity. | mS/m | Reserved namespace |
| `soil.cec` | Cation exchange capacity. | meq/100g | Reserved namespace |

Soil namespaces are reserved under the Layer Registry charter; schemas will follow future soil analytics ADRs referenced in the registry roadmap.【F:docs/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md†L35-L53】【F:docs/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L51-L83】

### Imagery & Elevation

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `imagery.ndvi` | NDVI raster tiles. | unitless | Drafting (ADR-011) |
| `imagery.tci` | True color composite. | RGB | Drafting (ADR-011) |
| `imagery.thermal` | Thermal imagery. | °C | Drafting (ADR-011) |
| `imagery.elevation` | DSM/DTM elevation. | meters | Drafting (ADR-011) |

Mapping and visualization ADR-011 governs imagery ingestion, color ramps, and persistence.【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-011 - Mapping and visualization imagery pipeline.md†L1-L52】

### Risk & Field Health

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `risk.flood` | Flood-prone zone mapping. | severity index | Drafting (ADR-052) |
| `risk.compaction` | Compaction risk zones. | severity index | Drafting (ADR-052) |
| `risk.weeds` | Weed pressure zones. | severity index | Drafting (ADR-052) |

Risk overlays and severity handling defined in ADR-052 align with zone drawing
workflows and analytics hooks. The shared `FieldHealthRiskLayer.v1` schema
exposes all governed risk layer identifiers via `x-nexus-layerIds`, enabling the
registry handshake and renderer caches to keep multiple severity overlays in
sync.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-052 - Field Health & Risk Plugin.md†L1-L43】【F:schemas/FieldHealthRiskLayer.v1.json†L1-L36】

### Guidance & Geometry

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `geometry.boundary` | Field boundary polygon. | meters | Drafting (ADR-043) |
| `geometry.headland` | Headland buffer. | meters | Drafting (ADR-043) |
| `geometry.abline` | Straight-line guidance (AB). | degrees | Drafting (ADR-033) |
| `geometry.contour` | Curved guidance paths. | meters | Drafting (ADR-033) |
| `geometry.keepout` | Keep-out/no-go zones. | boolean | Drafting (ADR-044) |
| `geometry.tile` | Drainage tile plan. | meters | Reserved namespace |

Guidance overlays and multi-field envelopes are handled in ADR-033 and ADR-043, while keep-out editing ties back to the zone framework (ADR-044).【F:docs/SRS/sections/8X_Guidance/81-ADR-033 - Guidance planner and autosteer orchestration.md†L1-L58】【F:docs/SRS/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L12-L68】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L21-L74】

### Device & Telemetry

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `telemetry.tracks` | Machine trail polylines. | meters | Drafting (ADR-047) |
| `telemetry.signal` | GNSS quality heatmap. | cm or bars | Drafting (Telemetry SRS) |
| `telemetry.error` | Autosteer deviation layer. | cm | Drafting (Telemetry SRS) |
| `telemetry.hours` | Operating hours heatmap. | hours | Drafting (Telemetry SRS) |

Live telemetry mesh ADR-047 and telemetry SRS requirements capture the share/subscribe expectations for these overlays.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L21-L71】【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L24-L44】

### Administrative & Meta

| Layer ID | Description | Units | Status |
| --- | --- | --- | --- |
| `notes.annotation` | User-drawn annotations. | text | Drafting (ADR-044) |
| `report.crop` | Generated crop report polygons. | none | Drafting (ADR-051) |
| `audit.provenance` | Provenance visualization graph. | hash graph | Drafting (ADR-019) |

Notes and zone annotations tie into the zone framework, report polygons originate from Report Builder, and provenance overlays follow ADR-019 governance.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L21-L74】【F:docs/SRS/sections/9X_Frontends_Ops/91-ADR-051 - Report Builder & Export System.md†L1-L68】【F:docs/SRS/sections/6X_Core_Domain_Services/64-ADR-019 - Provenance audit and QA governance.md†L1-L56】

## Relationships & Context Bus

Farm → Field → Season → Job → Session context is emitted over the lifecycle bus defined in ADR-040/041/043 and SRS §03 so plugins can subscribe once and receive deterministic updates when jobs or sessions change.【F:docs/SRS/sections/3X_Data_Storage/31-ADR-040 - Season Organizers.md†L21-L44】【F:docs/SRS/sections/6X_Core_Domain_Services/62-ADR-041 - Job Sessions Lifecycle.md†L52-L72】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L1-L53】 Layer provenance references `jobId` and `sessionId` through `Layer.v1`, ensuring analytics, reporting, and collaborative editing share consistent identifiers.【F:schemas/Layer.v1.json†L1-L73】


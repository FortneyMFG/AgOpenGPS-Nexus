# ADR-053 — Weather & Environment Plugin

- **Status:** Drafting
- **Date:** -
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Weather conditions heavily influence job planning, spraying decisions, and post-run analysis. Operators currently rely on external
apps or manual logs. Nexus needs a plugin that ingests sensor data, API feeds, and manual entries, stores snapshots with sessions,
and renders environmental overlays.

## Decision

Introduce a Weather plugin that records periodic weather snapshots, supports imports from APIs or farm stations, and publishes a
`weather.overlay` layer for map visualization. The plugin extends session metadata with weather fields and integrates with report
builder and analytics.

### Features

- Sources: onboard sensors, third-party APIs, manual entries via UI.
- Configurable auto-logging interval (e.g., every 5 minutes) while a session is active.
- UI surfaces current conditions, historical timeline charts, and import status.
- Weather overlay visualizes rainfall, temperature gradients, and wind vectors atop the active envelope.

### Data Model

- Session documents include `weatherSnapshot` (temperature, humidity, wind, wind gusts, rainfall, pressure, dew point, wet bulb, delta T, solar irradiance, UV index, visibility, evapotranspiration, soil temperature/moisture, leaf wetness) captured at start and optionally
  updated via `onSessionWeatherUpdate` events.
- `weather.overlay` layer stores gridded environmental values with provenance referencing the source and timestamp.
- Weather data available to other plugins via context bus for analytics (e.g., spraying compliance, yield correlation).

## Consequences

- Provides consistent weather context across planning, execution, and reporting.
- Requires caching and rate limiting for external API calls.
- Introduces additional sensor integration points.

## Governance Updates

- **Snapshot retention.** Weather snapshots adopt the session archival cadence defined in JobsService; operators must retain raw
  imports and API provenance so audits can rehydrate the data underpinning regulatory reports.【F:docs/sections/6X_Core_Domain_Services/62-ADR-030 - Field job sessions and lifecycle services.md†L13-L96】【F:schemas/WeatherOverlay.v1.json†L1-L140】
- **Source verification.** External API connectors log request/response hashes and rate limit decisions, and regression packs
  replay them through the composite simulation harness to verify deterministic caching.【F:docs/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L9-L43】
- **Alert scope compliance.** Weather-derived notifications respect share/subscribe ACLs and RadioBridge throttles, keeping
  sensitive agronomic data scoped to authorized collaborators.【F:docs/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L21-L70】【F:docs/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L9-L60】

## Amendment — 2025 architecture refresh (NX-190)

- Session snapshots capture weather deltas alongside crop, genetics, and profit references so analytics can correlate outcomes
  without bespoke joins.【F:schemas/Session.v1.json†L1-L120】【F:docs/sections/7X_Mapping_Geospatial/72-ADR-050 - Cost & Profit Plugin.md†L9-L96】
- Multi-field envelope membership informs weather overlays, enabling per-field rainfall and wind reporting that aligns with
  regulatory compliance and profitability splits.【F:docs/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L9-L112】
- Weather overlays seed LayerEditEvent journals when operators draw manual impact zones, ensuring collaborative edits replay
  consistently across mesh-connected devices.【F:docs/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L9-L74】

## Alternatives Considered

1. **Leave weather to external apps.** Rejected because compliance and analytics rely on aligned timestamps and contexts.
2. **Embed weather solely in session notes.** Too unstructured for analytics.

## Dependencies

- Builds on ADR-040/041 for session metadata and context events.
- Shares overlays with ADR-044 Zone Drawing (for manual weather impact zones) and ADR-047 mesh (optional live sharing).
- Supplies data to ADR-051 Report Builder and ADR-050 Profit analytics.

## SRS Impact

- Fulfills session snapshot and overlay requirements R-DATA-042 and R-DATA-048 in §08 Data Model & Storage.【F:docs/sections/3X_Data_Storage/32_Persistence_Formats.md†L30-L36】
- Wires weather logging hooks into §03 Job Lifecycle events (`onSessionWeatherUpdate`).【F:docs/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L31-L35】
- Powers weather overlays and timelines required by R-FE-074 and R-FE-075 in §05 Frontends.【F:docs/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L29-L31】

---

## Change Log

| Date | Summary | Author | PR / Issue |
|------|---------|--------|------------|
| 2025-10-24 | Initial draft | Nexus Team (Codex) |  |


# ADR-053 — Weather & Environment Plugin

- **Status:** Drafting
- **Date:** 2025-03-19
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

- Session documents include `weatherSnapshot` (temperature, humidity, wind, rainfall, pressure) captured at start and optionally
  updated via `onSessionWeatherUpdate` events.
- `weather.overlay` layer stores gridded environmental values with provenance referencing the source and timestamp.
- Weather data available to other plugins via context bus for analytics (e.g., spraying compliance, yield correlation).

## Consequences

- Provides consistent weather context across planning, execution, and reporting.
- Requires caching and rate limiting for external API calls.
- Introduces additional sensor integration points.

## Alternatives Considered

1. **Leave weather to external apps.** Rejected because compliance and analytics rely on aligned timestamps and contexts.
2. **Embed weather solely in session notes.** Too unstructured for analytics.

## Dependencies

- Builds on ADR-040/041 for session metadata and context events.
- Shares overlays with ADR-044 Zone Drawing (for manual weather impact zones) and ADR-047 mesh (optional live sharing).
- Supplies data to ADR-051 Report Builder and ADR-050 Profit analytics.

## SRS Impact

- Extends §03 Job Lifecycle with weather logging events.
- Updates §02 Data Model (Session weather snapshot) and §08 Data & Storage (weather overlay schema).
- Adds UI requirements to §05 Frontends for weather summaries and timeline charts.

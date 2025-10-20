# ADR-052 — Field Health & Risk Plugin

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Operators must document agronomic risks (flooding, compaction, weed outbreaks, disease) for planning, regulatory compliance, and
profitability analysis. Today these observations are captured as photos or notes without spatial structure, limiting reuse.
Nexus requires a plugin that records risk zones using the shared zone drawing tool, supports severity tracking, and exposes data
to analytics and reporting.

## Decision

Introduce a Field Health plugin that manages `risk.flood`, `risk.compaction`, `risk.weeds`, and `risk.other` layers. Attributes
include `severity`, `observedAt`, `observer`, and `notes`. Layers leverage the Zone Drawing Framework for editing and integrate
with the context bus for analytics.

### Contracts

- Publish `FieldHealthRiskLayer.v1` under `/schemas` to describe risk layer metadata (severity scale, observation provenance,
  attachments, roll-up statistics). Consumers validate layer payloads against this schema before persisting or rendering
  overlays.【F:schemas/FieldHealthRiskLayer.v1.json†L1-L140】

### Workflow

- Operators draw or import risk zones during scouting; severity is color-coded in the UI.
- Historical view per season shows risk evolution and correlation with crop/yield/performance metrics.
- Plugins such as Profit and Yield subscribe to `onFeatureCommit` events to adjust analytics (e.g., yield penalties).

## Consequences

- Provides structured risk documentation with provenance to fields, jobs, sessions, and observers.
- Enables analytics that correlate risk zones with costs and yields.
- Requires UI to surface severity legends and filtering.

## Governance Updates

- **Risk schema stewardship.** `FieldHealthRiskLayer.v1` revisions must publish migration scripts and downgrade guidance so
  archived scouting logs remain readable alongside new analytics releases.【F:schemas/FieldHealthRiskLayer.v1.json†L1-L140】
- **Observer accountability.** Risk entries require observer identity and timestamp validation tied back to session journals,
  producing auditable records for compliance and report exports.【F:schemas/Session.v1.json†L1-L120】【F:docs/ADR/ADR-051_ReportBuilder.md†L9-L70】
- **Alert gating.** Mesh alerts derived from risk zones must honor share/subscribe ACLs and RadioBridge throttling limits to
  avoid leaking sensitive agronomic data during collaborative operations.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L70】【F:docs/ADR/ADR-048_RadioBridge.md†L9-L60】

## Amendment — 2025 architecture refresh (NX-190)

- Field health overlays inherit crop, genetics, and weather context from session snapshots, letting analytics rank risks by crop
  susceptibility or weather stress automatically.【F:docs/ADR/ADR-045_CropTypePlugin.md†L9-L96】【F:docs/ADR/ADR-046_GeneticsPlugin.md†L9-L87】【F:docs/ADR/ADR-053_WeatherPlugin.md†L9-L66】
- Multi-field jobs track risk severities per field, improving profitability rollups and targeted scouting follow-ups without
  manual filtering.【F:docs/ADR/ADR-043_MultiFieldJobEnvelopes.md†L9-L112】
- Zone Drawing undo seeds propagate across mesh replicas so collaborative scouts produce identical audit trails even with
  intermittent connectivity.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L9-L74】

## Alternatives Considered

1. **Use generic notes/photos.** Insufficient for spatial analytics and collaboration.
2. **Fold risks into crop layers.** Would conflate agronomic status with crop assignment and complicate plugin responsibilities.

## Dependencies

- Relies on ADR-044 Zone Drawing Framework and context events from ADR-040/041/043.
- Feeds analytics for ADR-049 Yield and ADR-050 Profit.
- Data used by ADR-051 Report Builder for scouting sections.

## SRS Impact

- Implements risk overlay storage requirement R-DATA-047 in §08 Data Model & Storage and catalog coverage in §04 Mapping Layers.【F:docs/SRS/sections/3X/32_Persistence_Formats.md†L35-L36】【F:docs/SRS/sections/7X/72_Mapping_Layers_Plugin.md†L62-L67】
- Powers severity toggles and overlay legends required by R-FE-074 in §05 Frontends.【F:docs/SRS/sections/9X/91_UI_Shell_Layout.md†L29-L30】

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

### Workflow

- Operators draw or import risk zones during scouting; severity is color-coded in the UI.
- Historical view per season shows risk evolution and correlation with crop/yield/performance metrics.
- Plugins such as Profit and Yield subscribe to `onFeatureCommit` events to adjust analytics (e.g., yield penalties).

## Consequences

- Provides structured risk documentation with provenance to fields, jobs, sessions, and observers.
- Enables analytics that correlate risk zones with costs and yields.
- Requires UI to surface severity legends and filtering.

## Alternatives Considered

1. **Use generic notes/photos.** Insufficient for spatial analytics and collaboration.
2. **Fold risks into crop layers.** Would conflate agronomic status with crop assignment and complicate plugin responsibilities.

## Dependencies

- Relies on ADR-044 Zone Drawing Framework and context events from ADR-040/041/043.
- Feeds analytics for ADR-049 Yield and ADR-050 Profit.
- Data used by ADR-051 Report Builder for scouting sections.

## SRS Impact

- Adds risk layer requirements to §04 Mapping Layers and §08 Data Model & Storage.
- Updates §05 Frontends with severity legend and historical toggle expectations.

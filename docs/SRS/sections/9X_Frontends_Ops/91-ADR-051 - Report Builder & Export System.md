# ADR-051 — Report Builder & Export System

- **Status:** Drafting
- **Date:** 2025-03-19
- **Author(s):** Nexus architecture guild
- **NX Task:** NX-190 Comprehensive ADR portfolio review

## Context

Operators and consultants need consolidated reports that draw from seasons, jobs, sessions, and plugin analytics. Current exports
are siloed per plugin, forcing manual compilation. Nexus requires a centralized report builder that consumes plugin-provided
sections and generates PDF/CSV/GeoJSON bundles.

## Decision

Implement a Report Builder service that loads templates, queries data sources via plugin hooks, and produces export packages.
Templates define header metadata, required data sources, and widget layout. Plugins register sections using
`registerReportSection()` and supply renderers invoked during report generation.

### Template & API

- Template schema includes `id`, `name`, `version`, `scope` (farm/season/job/session), `sections[]`, and `outputs`.
- Built-in templates: Crop Report (Crop Type + Genetics + Yield), Profit Summary (Cost & Profit + Yield), Season Summary
  (multi-job overview).
- API `generateReport(templateId, scope, options)` orchestrates data fetch, rendering, and export packaging.
- Exports include PDF (layout engine), CSV tables per section, and optional GeoJSON bundles for spatial layers referenced in the
  report.

### Plugin Hooks

- `registerReportSection(sectionId, capabilities, renderFn)` — Plugins declare their contribution and dependencies.
- `onReportGenerate(context)` — Notification for plugins to preload caches or compute expensive analytics ahead of rendering.
- Sections specify required context (crop, yield, profit, field health) and declare fallback messaging when data is missing.

## Consequences

- Provides a unified, extensible reporting workflow covering all official plugins.
- Requires contract governance to ensure templates stay compatible as plugins evolve.
- Introduces layout tooling and PDF rendering dependencies into the build pipeline.

## Governance Updates

- **Template signing.** Templates bundled with operator presets include signed manifests listing required plugin versions and
  schema hashes; CI rejects unsigned templates or those referencing deprecated sections.【F:schemas/Layer.v1.json†L1-L120】【F:docs/SRS/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L17-L70】
- **Section certification.** Plugins must ship regression renderings for each registered section demonstrating compatibility with
  multi-field envelopes, session splits, and collaborative edits before release.【F:docs/SRS/sections/3X_Data_Storage/31-ADR-043 - Multi-Field Job Envelopes.md†L9-L112】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L9-L74】
- **Audit trails.** Generated reports embed session hashes and provenance pointers so downstream audits and export archives can
  verify the exact state used during generation.【F:schemas/Session.v1.json†L1-L120】

## Amendment — 2025 architecture refresh (NX-190)

- Report scopes now default to session snapshots that already include crop, genetics, yield, profit, field health, and weather
  context, reducing custom data joins for template authors.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-045 - Crop Type Plugin & Layers.md†L9-L96】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-049 - Yield & Analytics Plugin.md†L9-L96】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-053 - Weather & Environment Plugin.md†L9-L66】
- Templates can render collaborative timelines by replaying LayerEditEvent journals, letting teams illustrate who edited which
  zones during a session without exporting raw logs.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-044 - Zone Drawing Framework.md†L9-L74】
- Profit summaries now include live mesh metrics when available, capturing collaborative machine contributions while respecting
  RadioBridge ACLs for shared data.【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-047 - Live Telemetry Mesh.md†L21-L70】【F:docs/SRS/sections/4X_Interprocess_Communications/42-ADR-048 - RadioBridge for ELRS LoRa Telemetry.md†L9-L60】

## Alternatives Considered

1. **Maintain per-plugin exports only.** Rejected because it increases operator burden and prevents cross-plugin analytics.
2. **Use third-party BI tools.** Would require complex data pipelines and lose offline capability.

## Dependencies

- Consumes data from ADR-045 through ADR-050 and ADR-052/053 as available.
- Relies on context events (ADR-040/041/043) for scope selection.
- Must respect ACLs and privacy policies defined in ADR-047/048 when including shared data.

## SRS Impact

- Addresses report template governance R-DATA-049 in §08 Data Model & Storage and related packaging guidance.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L37-L38】
- Supplies generate/preview flows defined by R-FE-076 in §05 Frontends.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L31】
- Requires plugin registration and export packaging policies covered in §12 Extensibility.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L18-L36】

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

## Alternatives Considered

1. **Maintain per-plugin exports only.** Rejected because it increases operator burden and prevents cross-plugin analytics.
2. **Use third-party BI tools.** Would require complex data pipelines and lose offline capability.

## Dependencies

- Consumes data from ADR-045 through ADR-050 and ADR-052/053 as available.
- Relies on context events (ADR-040/041/043) for scope selection.
- Must respect ACLs and privacy policies defined in ADR-047/048 when including shared data.

## SRS Impact

- Adds export system requirements to §08 Data & Storage and §12 Extensibility.
- Updates §05 Frontends with UI expectations (generate button, preview, share).
- Documents new schemas in §options or appendices for template packaging.

# Report Template Catalog (ADR-051)

*Updated: 2025-03-19 UTC*

## Overview

The report builder centralizes how Nexus assembles cross-plugin exports by loading
templates, invoking registered section renderers, and producing PDF/CSV/GeoJSON
bundles for a chosen farm, season, job, or session scope.【F:docs/ADR/ADR-051_ReportBuilder.md†L16-L33】
Versioned `ReportTemplate` manifests capture the metadata that keeps those exports
deterministic and auditable across releases.【F:docs/SRS/sections/08_Data_Model_Storage.md†L37-L38】

## Catalog inventory

The initial catalog ships three first-party templates. Each template records its
identifier, scope, required sections, and output formats. Sections link to plugin
contributors via `registerReportSection()` so the builder can validate readiness
before generation.【F:docs/ADR/ADR-051_ReportBuilder.md†L17-L33】

| Template | Template ID | Scope | Primary sections | Data dependencies | Outputs |
| --- | --- | --- | --- | --- | --- |
| Crop Report | `report.crop.v1` | Season / Job | Crop acreage summary, genetics attribution, yield overlays | Crop Type, Genetics, Yield, Weather | PDF, CSV, GeoJSON |
| Profit Summary | `report.profit.v1` | Farm / Season / Job | Cost ledger rollup, profit heatmap, yield comparison | Cost & Profit, Yield, Crop Type, Genetics, Weather | PDF, CSV |
| Season Summary | `report.season-summary.v1` | Season | Multi-job timeline, weather timeline, scouting highlights | Crop Type, Yield, Field Health, Weather | PDF, CSV, GeoJSON |

> **Readiness states** — A template is eligible for export only when each required
section declares `Ready`, and optional sections may be skipped when their
contributors aren't available. Section contributors are responsible for
communicating missing data explanations to the UI prior to export.【F:Nexus SourceCode/src/Aog.Core/Reporting/ReportTemplate.cs†L23-L107】

## Template manifest schema

Templates are packaged as `ReportTemplate.v1` JSON documents alongside compiled
assets. The schema mirrors the runtime `ReportTemplate` record, capturing:

| Field | Description |
| --- | --- |
| `id` | Stable identifier used by `generateReport(templateId, scope, options)` and repository diffs. |
| `name` | Localized display string surfaced in the UI template picker. |
| `version` | Semantic version incremented for schema or layout changes. Minor versions cover cosmetic edits; major versions signal breaking section/output changes. |
| `scope` | Enum: `Farm`, `Season`, `Job`, or `Session`. Determines context fetch and validation rules. |
| `sections[]` | Ordered list of section descriptors (`sectionId`, `isOptional`, optional `parameters`). |
| `outputs[]` | Declares export payloads (`format`, `description`, optional `parameters` passed to the renderer). |
| `metadata` | Optional free-form block for feature flags, preview thumbnails, or localization bundles. |

Templates are validated before registration to prevent duplicate IDs, missing
sections, or unsupported outputs.【F:Nexus SourceCode/src/Aog.Core/Reporting/ReportBuilderService.cs†L16-L96】【F:Nexus SourceCode/src/Aog.Core/Reporting/ReportTemplate.cs†L23-L177】

> **Note:** Runtime section and output descriptors only capture identifiers,
> optionality, and parameter bags. Presentation fields such as section titles,
> required data hints, or fallback copy must be supplied by the consuming UI or
> documentation until the runtime models grow dedicated properties.

### Example `ReportTemplate.v1`

```json
{
  "id": "report.crop.v1",
  "name": "Crop Report",
  "version": "1.0.0",
  "scope": "Season",
  "sections": [
    {
      "sectionId": "crop.analytics.summary",
      "isOptional": false,
      "parameters": {
        "layout": "wide"
      }
    },
    {
      "sectionId": "yield.analytics.summary",
      "isOptional": true
    }
  ],
  "outputs": [
    { "format": "pdf", "description": "Paginated PDF export" },
    { "format": "csv", "description": "Section tables (UTF-8)" },
    { "format": "geojson", "description": "Spatial overlays" }
  ],
  "metadata": {
    "featureFlag": "ReportBuilder.Enabled",
    "preview": "reports/crop-report-preview.png"
  }
}
```

## Section contributors

The catalog relies on plugin-owned section contributors registered during startup.
Templates should only reference section IDs that resolve to an installed
contributor; otherwise the builder rejects registration during boot or export.

| Section ID | Provided by | Required context | Notes |
| --- | --- | --- | --- |
| `crop.analytics.summary` | Crop Type plugin | Crop history, job/season context | Aggregates acreage per crop and season.【F:docs/ADR/ADR-045_CropTypePlugin.md†L44-L49】 |
| `genetics.analytics.summary` | Genetics plugin | Crop context, variety layers | Summarizes variety distribution and barcode traceability for the selected scope.【F:docs/ADR/ADR-046_GeneticsPlugin.md†L17-L43】 |
| `yield.analytics.summary` | Yield plugin | Yield layers, crop/genetics metadata | Provides yield statistics and overlays aligned with crop and genetics context.【F:docs/ADR/ADR-049_YieldPlugin.md†L15-L53】 |
| `profit.analytics.summary` | Cost & Profit plugin | Cost records, yield aggregations | Produces cost, revenue, and profit rollups plus ledger excerpts.【F:docs/ADR/ADR-050_CostProfitPlugin.md†L17-L40】 |
| `fieldHealth.summary` | Field Health plugin | Risk layers, observations | Highlights risk hotspots with severity notes and attachments.【F:docs/ADR/ADR-052_FieldHealthPlugin.md†L17-L49】 |
| `weather.timeline.summary` | Weather plugin | Session weather snapshots, overlays | Renders compliance timeline charts and weather overlays referenced by exports.【F:docs/ADR/ADR-053_WeatherPlugin.md†L16-L49】 |

When a template includes an optional section, ensure the contributor exposes a
`CapabilityState.Optional` flag so the builder can mark it as skippable without
failing the export. Optional sections should emit their own empty-state
messaging so operators understand the trade-off of exporting early.【F:Nexus SourceCode/src/Aog.Core/Reporting/ReportTemplate.cs†L127-L177】

## Governance & change control

1. **Author templates alongside ADR updates.** New templates require an ADR or
   ADR addendum describing scope, data dependencies, and expected outputs so they
   remain traceable to product decisions.【F:docs/ADR/ADR-051_ReportBuilder.md†L20-L33】
2. **Version diligently.** Bump the manifest version when section ordering,
   required data, or outputs change. Major versions accompany breaking changes;
   minor versions cover additive sections or cosmetic updates; patch versions
   capture copy fixes or metadata tweaks.【F:docs/SRS/sections/08_Data_Model_Storage.md†L37-L38】
3. **Diff review.** Store manifests under source control so CI can diff template
   changes and ensure reviewers validate scope/output deltas. Pair template
   updates with automated tests that exercise `ReportBuilderService.ListTemplates()`
   to catch duplicate IDs or missing sections.【F:Nexus SourceCode/src/Aog.Core/Reporting/ReportBuilderService.cs†L16-L96】【F:Nexus SourceCode/tests/Aog.Core.Tests/Reporting/ReportBuilderServiceTests.cs†L16-L140】
4. **UI preview contract.** Coordinate with the Avalonia UI to ensure previews and
   template selection flows stay synchronized with manifest metadata (`name`,
   outputs, readiness states) surfaced through `ListTemplates()` and
   `TryGetTemplate()` APIs.【F:Nexus SourceCode/src/Aog.Core/Reporting/IReportBuilderService.cs†L15-L65】【F:Nexus SourceCode/tests/Aog.UI.Avalonia.Tests/Reporting/ReportPreviewViewModelTests.cs†L70-L115】【F:docs/SRS/sections/05_Frontends.md†L31-L32】
5. **Export auditing.** Pair template changes with report export audit fixtures so
   QA can diff PDF/CSV/GeoJSON bundles when section logic evolves, paving the way
   for NX-331 automated export auditing.【F:Nexus SourceCode/tests/Aog.Core.Tests/Reporting/ReportExportAuditorTests.cs†L13-L120】【F:tasks.md†L313-L331】

## Offline and enterprise distribution

- **Bundled templates:** Ship default templates with Core so offline rigs always
  have a baseline catalog. Templates register during service start and are
discoverable via `ListTemplates()` for UI and CLI tooling.【F:Nexus SourceCode/src/Aog.Core/Reporting/ReportBuilderService.cs†L16-L96】
- **Enterprise overrides:** Allow additional templates to be side-loaded as signed
  bundles (ZIP containing manifest + assets). Enterprises should stage them in the
  plugin catalog with the same signature policy used for manifests to preserve
  provenance and traceability.【F:docs/SRS/sections/16_Plugin_Packaging_Updates.md†L16-L27】
- **Offline updates:** Document override templates in release notes and provide
  checksum manifests so operators can verify authenticity before installing in
  air-gapped environments. Use the same workflow as plugin catalog side-loads to
  minimize bespoke tooling.【F:docs/SRS/sections/16_Plugin_Packaging_Updates.md†L10-L31】

## Operational checklist

1. Confirm all referenced section contributors load without warnings in the
   startup log.
2. Run `nexus report list` (planned CLI) or equivalent API inspection to verify
   template metadata after deployment.
3. Trigger a dry-run export in staging to populate audit diffs before promoting to
   production.
4. Update operator training materials so they understand new templates, required
   data, and export formats.

Maintaining this catalog ensures Report Builder exports remain predictable,
verifiable, and aligned with the agronomic analytics roadmap established in
ADR-051.【F:docs/ADR/ADR-051_ReportBuilder.md†L16-L39】

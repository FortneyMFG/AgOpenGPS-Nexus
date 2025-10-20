# Weather Compliance Export Playbook (Draft)

## Purpose

Weather-driven regulations often mandate documenting conditions before and during crop input applications. Nexus consolidates
weather snapshots, overlays, and ingest provenance through the Weather plugin so compliance packets no longer depend on
handwritten logbooks. This playbook describes how to assemble the export bundle defined by ADR-053 using existing Report Builder
and Regulatory plugin hooks.【F:docs/ADR/ADR-053_WeatherPlugin.md†L9-L48】【F:docs/plugins/Regulatory.md†L1-L32】【F:docs/ADR/ADR-051_ReportBuilder.md†L9-L40】

## Prerequisites

1. **Session weather snapshot coverage** — Ensure `Session.v1` documents capture the required weather sample fields and emit
   updates when manual entries or ingest pipelines modify readings.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L29-L53】
2. **Overlay generation** — Confirm the Weather plugin is producing `weather.overlay` tiles aligned with the session timeline so
   spatial context is available in the export bundle.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L44】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L54-L74】
3. **Regulatory export registration** — The Regulatory plugin must subscribe to the weather compliance export type so it can sign
   the JSON manifest and surface completion status on the compliance dashboard.【F:docs/plugins/Regulatory.md†L1-L32】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L74-L90】
4. **Report Builder template deployment** — Publish the `weather-compliance.v1` template that renders the summary PDF referenced
   below. Templates follow the Report Builder governance defined in ADR-051.【F:docs/ADR/ADR-051_ReportBuilder.md†L9-L41】

## Export package layout

Generate the export as a deterministic ZIP bundle: `WeatherCompliance_<SessionId>_<Timestamp>.zip`. Inside the archive, create
the following artifacts to satisfy regulatory audits and internal replay workflows. Checksums are SHA-256 digests recorded in the
manifest to support tamper detection.

| Artifact | Description | Source | Notes |
| --- | --- | --- | --- |
| `manifest.json` | Top-level metadata: export ID, session reference, generation timestamp, checksum table, signing certificates, and
regulatory program codes. Stored in JSON for machine-to-machine ingestion by partners. | Regulatory plugin | Manifest fields align
with the `WeatherComplianceExport` sample in `schemas/examples` and are signed per Regulatory plugin policy.【F:docs/plugins/Regulatory.md†L12-L27】【F:schemas/examples/WeatherComplianceExport.sample.json†L1-L52】 |
| `weather-summary.pdf` | Human-readable PDF summarizing weather bands, threshold violations, and provenance badges. | Report Builder |
Generated from the `weather-compliance.v1` template. Each section references the data slices included in the manifest for audit
parity.【F:docs/ADR/ADR-051_ReportBuilder.md†L12-L40】 |
| `session-weather.json` | Snapshot history extracted from `Session.v1` with the canonical weather fields captured at start and
per subsequent update. | Weather plugin / Session store | Values follow `R-DATA-042` and include the source identifier for each
sample so auditors can confirm instrumentation.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L29-L53】 |
| `weather-overlay.geojson` | Spatial tiles (temperature, rainfall, wind vectors) covering the job envelope at the export time
range. | Weather plugin tile cache | Down-sampled to the regulatory resolution while retaining quantization metadata noted in the
manifest.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L44】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L54-L74】 |
| `ingest-log.csv` | Chronological feed of ingest pipeline events: sensor heartbeats, API polling status, manual entries, and
validation notices. | Weather ingest pipeline | Serves as traceability for downtime investigations and is optional when no errors or
manual overrides occurred during the session.【F:docs/plugins/Weather.md†L9-L28】 |

## Manifest schema highlights

The manifest extends the Regulatory plugin’s signed export format with weather-specific blocks. See
`schemas/examples/WeatherComplianceExport.sample.json` for a validating example. Key sections include:

- `weatherSnapshotHistory[]` — Ordered list of weather samples with timestamp, instrument source, and the fields mandated by
  R-DATA-042 for compliance-ready reporting.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L29-L53】
- `overlayArtifacts[]` — References to exported overlay files plus metadata describing grid resolution, coordinate reference
  system, interpolation confidence, and checksums for each payload.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L44】
- `thresholdEvaluations[]` — Summaries of regulatory thresholds (e.g., wind speed, delta T) evaluated across the session with
  violation windows and pointer references into the PDF report. These evaluations backstop UI warnings and provide textual
  justifications required by auditors.【F:docs/plugins/Weather.md†L9-L20】
- `signatures[]` — Chain of cryptographic signatures (operator, reviewer, regulatory agent) leveraging the Regulatory plugin’s
  signing flow so exports can be validated offline.【F:docs/plugins/Regulatory.md†L12-L27】

## Generation workflow

1. Trigger the Report Builder service with template `weather-compliance.v1`, scope=`session`, and options containing the desired
   export window (defaults to full session). Capture the generated PDF and structured data payloads returned by the template
   renderers.【F:docs/ADR/ADR-051_ReportBuilder.md†L12-L40】
2. Request the Weather plugin’s export endpoint for the targeted session, providing the same export window. The plugin streams the
   overlay bundle and ingest log required for compliance packaging.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L44】【F:docs/plugins/Weather.md†L9-L28】
3. Assemble `manifest.json` with references, checksums, and provenance metadata for each artifact. Include threshold evaluations
   derived from the structured payload returned in step 1 and the ingest events from step 2. Sign the manifest with the
   Regulatory plugin service identity and append any operator signatures collected in-cab.【F:docs/plugins/Regulatory.md†L12-L27】
4. Zip the artifacts, attach the signed manifest, and store the bundle under the compliance archive path governed by the
   Regulatory plugin. Update the compliance dashboard status and push audit events so downstream systems can detect the new
   export.【F:docs/plugins/Regulatory.md†L12-L27】

## Operational considerations

- **Retention and storage** — Follow the archival policies defined in the data lifecycle ADR to keep weather compliance packets
  accessible for the mandated window (e.g., 3–5 years) before transitioning to cold storage.【F:docs/ADR/ADR-025-data-lifecycle-retention.md†L1-L34】
- **Replays and QA** — Use the manifest’s checksums and timestamps when loading weather context into replay scenarios to guarantee
  deterministic analytics runs or audit reviews.【F:docs/ADR/ADR-004-composite-simulation.md†L31-L52】
- **Cross-plugin coordination** — Link export IDs to pesticide or application logs generated by the Regulatory plugin so auditors
  can cross-reference product usage, weather, and operator actions without manual stitching.【F:docs/plugins/Regulatory.md†L1-L32】


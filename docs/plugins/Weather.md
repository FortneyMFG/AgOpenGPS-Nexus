# Weather Plugin UX & Visualization Requirements (Draft)

## Overview

The Weather plugin surfaces environmental context alongside the job timeline and map overlays. It consumes weather snapshots, overlay grids, and ingest status events exposed by ADR-053 to keep operators aware of spraying windows, compliance thresholds, and forecast deviations. The UX must highlight both the latest conditions and historical trends while remaining consistent across desktop and companion clients.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L55】

## Session Timeline Experience

- Render a multi-series timeline showing temperature, humidity, wind speed/direction, rainfall rate, and delta T across the active session. Operators can toggle individual series; temperature/delta T use dual-axis scaling so agronomic thresholds remain legible.【F:docs/SRS/sections/9X/91_UI_Shell_Layout.md†L27-L34】
- Overlay regulatory guard rails (e.g., maximum wind for spraying, minimum temperature) derived from crop presets or compliance policies. Exceeding bands highlight the chart region and raise a warning badge in the session drawer.【F:docs/ADR/ADR-027-spatial-constraints.md†L49-L62】
- Surface ingest provenance with inline badges: station ID, API source, manual entry indicator, and clock drift status when the ingest pipeline falls behind the configured cadence.【F:docs/ADR/ADR-053_WeatherPlugin.md†L33-L49】
- Provide manual entry affordances that stamp snapshots onto the timeline with operator initials and notes. Manual points inherit the same validation rules enforced by `WeatherSample.Validate` so out-of-range values prompt correction dialogs.【F:Nexus SourceCode/src/Aog.Plugins/Weather/WeatherSample.cs†L7-L101】
- Expose import status (API polling, sensor connectivity) adjacent to the chart legend. Errors include retry ETA and recommended operator actions sourced from the ingest pipeline diagnostics.【F:Nexus SourceCode/src/Aog.Plugins/Weather/WeatherIngestPipeline.cs†L19-L120】

## Map Overlay UX

- Register `weather.overlay` as a quick toggle within the layer toolbar alongside crop, genetics, yield, profit, and field health overlays. Metadata defines available variables so the UI can present sub-layer chips (temperature, rainfall, wind vectors) without code changes.【F:docs/SRS/sections/7X/72_Mapping_Layers_Plugin.md†L72-L109】【F:schemas/examples/WeatherOverlay.sample.json†L1-L32】
- Draw scalar grids (temperature, rainfall) using perceptually uniform color ramps with accessible legend labels. Wind overlays render vector arrows scaled by speed with gust indicators, sharing legend entries that describe direction conventions.【F:docs/ADR/ADR-053_WeatherPlugin.md†L21-L49】
- Link overlay timestamps to the session timeline cursor. Scrubbing the timeline updates the map to the closest overlay snapshot and updates legends with the effective timestamp/provenance badge.【F:docs/SRS/sections/3X/31_Domain_Data_Model.md†L70-L127】
- When overlay coverage is sparse, display fallback annotations showing nearest station values plus interpolation confidence so operators know when the grid is extrapolated versus observed.【F:docs/ADR/ADR-053_WeatherPlugin.md†L33-L49】
- Respect zone and constraint visibility by stacking weather overlays beneath keep-out/automation gating layers, ensuring constraint hatching remains legible while still conveying weather intensity.【F:docs/SRS/sections/7X/72_Mapping_Layers_Plugin.md†L6-L79】

## Accessibility & Responsiveness

- Charts and map legends must honor theme contrast requirements and support keyboard navigation for toggling series, switching overlay variables, and annotating snapshots. Focus indicators follow the shared Avalonia theming guidelines used by other analytics panels.【F:docs/SRS/sections/9X/91_UI_Shell_Layout.md†L14-L33】
- Companion clients reuse the same data model but collapse the timeline into summarized cards (current, 15 min, 1 hr trends) with a “View Chart” modal to avoid overwhelming smaller screens.【F:docs/ADR/ADR-003-avalonia-ui.md†L24-L44】
- Offline workflows cache the last 24 hours of snapshots and overlays locally. When reconciling after reconnect, the UI surfaces a “Replayed data” banner and merges missing intervals while preserving manual entries with conflict resolution prompts.【F:docs/ADR/ADR-030-field-job-sessions.md†L33-L86】

## Telemetry & Reporting Hooks

- Emit user interactions (series toggles, timeline scrubbing, manual entry saves) via telemetry events so analytics can correlate weather awareness with job decisions. Events include session/job IDs and overlay variable context.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L1-L38】
- Provide export hooks for Report Builder so the same timeline view can render into PDF reports with accessibility-compliant color palettes and provenance callouts. Export payloads share the overlay metadata bundle and chosen time range.【F:docs/ADR/ADR-051_ReportBuilder.md†L21-L52】

## Open Questions

- Should the UI support predictive overlays (e.g., short-term forecasts) or restrict visuals to observed data until ADR-053 defines forecast ingestion contracts?
- How aggressively should the timeline down-sample when sessions exceed eight hours to preserve performance without losing compliance-relevant spikes?

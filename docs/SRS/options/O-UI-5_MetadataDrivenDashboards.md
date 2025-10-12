# O-UI-5: Metadata-driven dashboards and visualization

## Summary
Updates the desktop UI so widgets, charts, and inspectors consume layer metadata rather than hard-coded IDs, enabling declarative dashboards for planters, sprayers, and combines while supporting per-layer visualization rules and drill-downs.

## Details
- Extend viewport overlays to show hover tooltips with decoded value, unit, quality, weight, and `rateNA` flags for the closest section, plus a pin-able inspector card that exposes raw PGN bytes and transport info.
- Layer visibility toggles, opacity controls, legends, per-row strip views, and summary widgets all read display ranges, aggregation hints, and quality rules from the layer definition metadata.
- Provide dashboard presets (per-row bar chart, stacked-pass volume chart, time-series trendline) configured purely through metadata with scale toggles for relative vs. absolute views.
- Allow users to bookmark chart layouts per layer and dock drill-down panels to the right strip or float overlays, keeping multi-monitor support intact.
- Add a Layer Definition Manager accessible from the existing configuration button with tabs for Layer Catalog, Derived Layers, and Transport that supports import/export of JSON, validation of smoothing/EMA/deadband settings, and warnings for incompatible hardware precision.
- Augment section configuration to include per-layer defaults, coverage factors, and previews showing how overlaps aggregate (area-weighted averages vs. stacked sums) with quick duplication from presets such as a 48-row planter.
- Keep shipped dashboards declarative so new layers only require metadata updates; avoid branching on specific IDs inside UI components.

## Pros
- Operators can tailor dashboards without waiting for bespoke code changes.
- Metadata-driven rendering keeps new layer types in sync across WPF/OpenGL views and remote displays.
- Inspector upgrades improve in-field diagnostics and speed troubleshooting.

## Cons
- Requires significant UI refactoring to read metadata instead of hard-coded layer IDs.
- New configuration flows add complexity that must be explained in documentation and onboarding.
- Rendering pipelines must be validated across Windows multi-monitor setups to avoid regressions.

## Risks & mitigations
- **Risk:** Performance degradation from richer overlays. **Mitigation:** Benchmark 48–64 row rigs at 10 Hz ingest, target ≤3% CPU and ≤150 MB RAM after one hour, and reuse throttled snapshots from backend controllers.
- **Risk:** User confusion with numerous options. **Mitigation:** Ship presets, tooltips, and validation that highlights missing required layers or conflicting settings.
- **Risk:** Divergent styling between components. **Mitigation:** Adopt shared color ramp utilities with test coverage for bin boundaries and legend labels to keep WPF/OpenGL aligned.

## Borrowables
- Existing WPF configuration dialogs and OpenGL renderer supply baseline components that can be extended with metadata binding.
- AgDiag legend parity tests and packet-rate monitors can be reused for overlay validation and telemetry visualization.
- Multi-monitor window placement helpers already in AgOpenGPS ensure new dashboards respect current layout persistence.

## Rough effort
L — Touches core rendering, inspector UX, dashboard frameworks, and configuration workflows to make them metadata-driven.


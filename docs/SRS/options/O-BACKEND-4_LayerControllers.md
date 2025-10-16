# O-BACKEND-4: Layer controllers with aggregation pipelines

## Summary
Refactors the backend navigation/mapping services to manage per-section layer controllers that normalize raw sensor feeds, buffer samples between GNSS fixes, and emit aggregated geometry snapshots for rendering, dashboards, and storage.

## Details
- Represent each section/row as a container of layer controllers that maintain rolling accumulators (sum, numerator/denominator, min/max) plus timestamps and position history so overlap math stays precise even after multiple passes.
- Provide normalized value (0–1), engineering value, and quality (0–1) for each controller together with `rateNA` flags and derived layer bindings to support views like “Under Applied” and “Over Applied”.
- Allow multiple hardware inputs to feed the same logical layer via `sourceMappings`, smoothing parameters (`deadbandPct`, `emaAlpha`), and packed bit indices for digital feeds.
- Introduce per-layer write queues and emission cadence (`emitCadenceHz`) to coalesce high-frequency samples, reuse buffers, and produce immutable snapshots for the mapping thread to consume.
- When mapping is active, compute `areaSlice = groundSpeed * dt * sectionWidth * coverageFactor`, update accumulators, and store zero weights for missing data so later aggregation can ignore gaps cleanly.
- Support compositing rules per layer: sums for rates, maxima for alarm layers, logical OR for digital feeds, percentage calculations via numerator/denominator counters, and optional min/max tracking for analytics.
- Suspend mapping below configurable `v_min` while keeping the latest sensor sample available for UI/diagnostics and log quality metrics for each controller.
- Break runtime into IO ingestion, aggregation/mapping, and UI rendering threads with immutable snapshots passed between them to avoid tearing.
- Register layer controllers and derived layer factories through dependency injection so plugins can supply additional telemetry processors without modifying core code.

## Pros
- Clean separation between ingestion, aggregation, and rendering reduces coupling and eases testing of overlap math.
- Derived layers and quality metrics enable richer analytics without requiring firmware changes for every new visualization.
- Per-layer emission cadence provides deterministic performance even with dozens of high-rate sensors.

## Cons
- Requires significant refactoring of the current mapping pipeline and section data structures.
- Increases state management complexity, especially when multiple inputs feed one logical layer.
- Needs thorough validation to ensure no regressions in binary section coverage or steering responsiveness.

## Risks & mitigations
- **Risk:** Aggregation bugs could misreport application volumes. **Mitigation:** Add deterministic replay suites with golden outputs per layer and compare accumulators against legacy totals.
- **Risk:** Threading issues between IO, aggregation, and UI loops. **Mitigation:** Pass immutable snapshots, enforce single-writer rules, and cover with stress tests that simulate bursty firmware traffic.
- **Risk:** Complexity overwhelms smaller contributors. **Mitigation:** Document controller lifecycle, provide scaffolding tests, and keep defaults aligned with current binary behavior for simple rigs.

## Borrowables
- Existing `TurnMappingOn` flow and triangle-strip generation from AgOpenGPS can seed the snapshot emission logic.
- AgDiag replay tooling can drive deterministic ingestion streams for controller tests.
- Current section/row configuration dialogs provide geometry metadata needed for coverage factor calculations.

## Rough effort
L — Touches ingestion, mapping, rendering, and configuration code paths with new controller abstractions and replay harnesses.

## References
- [Section 04 — Mapping Layers](../sections/04_MappingLayers.md)
- [Section 08 — Data Model Storage](../sections/08_Data_Model_Storage.md)
- [Section 09 — Control Automation](../sections/09_Control_Automation.md)

## Related ADRs
- [ADR-010 — Layer Registry & Variable Rate](../../ADR/ADR-010-layer-registry-variable-rate.md)
- [ADR-020 — Determinism & Replay CI](../../ADR/ADR-020-determinism-replay-ci.md)
- [ADR-068 — Layer controllers & aggregation runtime](../../ADR/ADR-068-layer-controllers-runtime.md)


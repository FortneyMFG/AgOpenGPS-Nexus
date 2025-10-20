# O-DATA-5: Metadata-driven variable-rate layers

## Summary
Establishes a layer catalogue and storage model that preserves today’s binary section maps while adding numeric and categorical feedback layers with shared metadata, units, and aggregation rules so historical passes, dashboards, and exports remain consistent.

## Details
- Maintain the existing triangle-strip geometry for on/off coverage while introducing per-layer controllers that store normalized values, engineering units, quality weights, and running accumulators so multiple feedback signals per section can coexist without rewriting the renderer.
- Ship a predefined layer catalogue (e.g., Section On/Off, Commanded Rate, Actual Rate, Skips, Doubles, Downforce, Hopper Pressure, Yield, Moisture, Test Weight, generic digital/analog inputs) while allowing projects to register new layers via configuration files without code changes.
- Publish a units registry (`state.onOff`, `state.flowState`, `ratio.percent`, `pressure.psi`, `pressure.kpa`, `rate.gpa`, `mass.kg_ha`, `volume.bu_ac`, `moisture.percent`) so dashboards, exports, and firmware share canonical IDs and symbols when they serialize layer definitions.
- Model each layer definition with schema version, identifier, names, units, normalization range, aggregation strategy, display ranges, composite rules, smoothing parameters, alarm bands, quality rules, resolution overrides, storage precision hints, and optional derived layer bindings for “Under Applied”/“Over Applied” views.
- Store geometry per layer with value, weight, min/max, and optional numerator/denominator counters to keep overlap math exact. Zero weight marks missing data, and per-layer write queues throttle emission cadence to avoid heap churn during high-rate sampling.
- Extend project/profile persistence to bundle layer definitions, controller bindings, and chunked map tiles (header + compressed layer chunks keyed by `(layerId, zoomTile)` using LZ4/Zstd) so replay/export tools can reconstruct engineering values exactly.
- Include quantization metadata, schema hashes, and optional downsampling per layer (`tileResolutionMultiplier`) while providing CSV/GeoTIFF export utilities fed by the same accumulators that drive dashboards.

## Pros
- Keeps backwards compatibility with current binary coverage files while opening the door to richer telemetry.
- Metadata-driven catalog avoids hard-coded UI logic and enables third parties to add layers by configuration.
- Shared units registry and aggregation hints improve interoperability with firmware, analytics, and export tooling.

## Cons
- Requires new configuration flows to manage layer definitions and unit registries safely.
- Increases storage footprint unless chunking, compression, and resolution hints are tuned carefully.
- Adds schema complexity that legacy tools must ignore or migrate toward.

## Risks & mitigations
- **Risk:** Data corruption during migration from legacy map files. **Mitigation:** Keep existing section streamer untouched and gate new layer persistence behind explicit schema versions and export tooling.
- **Risk:** Conflicting layer definitions between machines. **Mitigation:** Hash schema-critical fields and include the digest in persistence/handshake to detect drift before controllers emit data.
- **Risk:** Runaway memory usage from high-frequency layers. **Mitigation:** Use per-layer emission cadence, queue pooling, and optional coarse tile resolution to cap allocations.

## Borrowables
- Reuse AgOpenGPS field streamer interfaces for header/chunk handling.
- Adapt existing OpenGL strip renderer as the binary compatibility baseline while layering new scalar buffers alongside it.
- Leverage AgDiag export scripts as a starting point for CSV/GeoTIFF converters fed by the new accumulators.

## Rough effort
L — Introduces new schema definitions, persistence changes, configuration tooling, and export/replay utilities in addition to the runtime controllers.

## References
- [Section 31 — Domain Data Model](../sections/3X/31_Domain_Data_Model.md)
- [Section 72 — Mapping Layers Plugin](../sections/7X/72_Mapping_Layers_Plugin.md)
- [Section 32 — Persistence & Formats](../sections/3X/32_Persistence_Formats.md)

## Related ADRs
- [ADR-010 — Layer Registry & Variable Rate](../../ADR/ADR-010-layer-registry-variable-rate.md)
- [ADR-020 — Determinism & Replay CI](../../ADR/ADR-020-determinism-replay-ci.md)
- [ADR-034 — Metadata-Driven Dashboards](../../ADR/ADR-034-metadata-driven-dashboards.md)


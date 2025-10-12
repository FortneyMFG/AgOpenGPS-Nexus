# O-API-5: Versioned layer schemas and quality metadata

## Summary
Extends interprocess contracts so JSON/protobuf definitions describe layer metadata, schema hashes, quality rules, and alarm bands, enabling remote clients and plugins to stay in sync with firmware-emitted layers.

## Details
- Layer definition records include schema version, IDs, names, units, normalization ranges, aggregation modes, composite rules, display ranges, smoothing parameters, alarm bands, quality rules, derived layer bindings, storage precision, and emission cadence hints.
- Include `sourceMappings` so multiple hardware inputs can publish into a single logical layer and so remote clients understand how firmware channels relate to high-level metrics.
- Track `tileResolutionMultiplier`, `targetBand`, `alarmBands`, and `qualityRules` alongside `rateNAFlag` semantics so analytics and visualization clients interpret missing/invalid data the same way as the core app.
- Provide a shared units registry and layer ID registry to avoid typos and collisions, reserving IDs 240–255 for third-party namespaces.
- Exchange schema hashes during handshake and persist only hash-critical fields so cosmetic changes (e.g., legend labels) do not break compatibility mid-field.
- Surface monotonic timestamps, GNSS supplements, and validity bitmaps per block so consumers can reason about ordering and confidence without bespoke parsing.

## Pros
- Keeps remote displays, plugins, and backend services aligned with the layer catalog used by firmware and the desktop app.
- Schema hashing detects mismatches early, reducing debugging time in the field.
- Quality and alarm metadata enable richer alerting without duplicated logic in every client.

## Cons
- Requires additional serialization work (JSON/protobuf schemas) and documentation for integrators.
- Schema evolution must be carefully managed to avoid breaking older clients.
- Increased payload size when exchanging definitions and metadata.

## Risks & mitigations
- **Risk:** Conflicting schema versions across components. **Mitigation:** Centralize registry publishing in docs and handshake logs, and add CI linting for unique IDs.
- **Risk:** Third-party plugins misuse reserved IDs. **Mitigation:** Reserve ranges explicitly and validate at runtime, providing guidance for namespace extensions.
- **Risk:** Metadata drift between firmware and UI. **Mitigation:** Persist schema hashes and fail fast when mismatches occur, guiding operators to resync definitions.

## Borrowables
- Existing AgOpenGPS configuration serialization can be extended with the new metadata fields.
- AgIO settings exchange flows provide a blueprint for schema negotiation and logging.
- Templates in `docs/templates/SECTION.md` and `OPTION.md` can host living documentation of each schema revision.

## Rough effort
M — Requires schema design, serialization tooling, validation, and contributor education but largely builds on existing configuration exchange mechanisms.

## References
- Derived from the historical `docs/variable-layer-plan.md` proposal authored for the variable-rate initiative.

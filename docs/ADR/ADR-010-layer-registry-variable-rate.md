# ADR-010: Layer registry and variable-rate framework

## Status
Drafting (target review window: 2025-11-05 week)

**Relevant Plugin(s):** Mapping, Variable Mapping, Rate Control, Section Control, Planter Monitor, Telemetry Logging



## Context
Variable-rate workflows, analytics, and dashboards require a canonical catalog of layers with defined units, precision, and discovery metadata. Nexus currently lacks a central registry, forcing plugins and UI code to hard-code identifiers and color ramps. ADR-010 introduces the LayerDefinition schema and supporting tooling so mapping, presets, and capability manifests align on consistent metadata while remaining compatible with ADR-009 persistence and ADR-031 manifest governance.

## Decision
- Define a versioned LayerDefinition schema covering identifiers, schema hashes, units, normalization, aggregation modes, and presentation metadata.
- Provide a registry service and validator tooling that enforce numeric precision rules, unit compatibility, and hash handshake semantics with plugins.
- Integrate the registry with manifest tooling (ADR-031) and the variable-rate framework so layer discovery and capability gating operate consistently.
- Normalize sample agronomic layers and color ramps through the registry to validate interoperability with existing tooling.

## Consequences
- Plugins and UI surfaces can query the registry instead of hard-coding layer metadata, reducing drift and easing future extensions.
- Registry publication and hash enforcement introduce operational overhead but provide deterministic compatibility checks across deployments.
- Migration guides must translate prior variable-rate catalogs into the new schema to keep historical data actionable.

## Governance Updates
- **Registry beta.** The Layer Registry launched a constrained beta that reserves identifier namespaces and enforces submission templates. Early adopters sign contribution agreements covering SLA response within five business days.
- **Governance charter.** Published workflow defines proposal intake, review quorum, deprecation policy, and appeals. Automation syncs accepted definitions into CI so downstream ADRs consume the same catalog.
- **Deprecation handling.** Deprecated layers require dual publishing windows with compatibility adapters, and removal dates are broadcast 90 days in advance via release notes and operator mailers.

## Amendment — 2025 architecture refresh (NX-190)

- Catalog expanded to include crop (`cropType.planned`, `cropType.actual`, `cropType.history`), genetics (`genetics.plan`, `genetics.variety`), harvest (`yield.actual`, `yield.moisture`, `yield.testWeight`), and profitability (`profit.net`) layers aligned with ADR-045 through ADR-050. Schemas reside under `/schemas` with provenance flags marking Core vs. plugin ownership.
- Registry metadata now encodes planned vs. actual semantics using `x-nexus-planned` and `x-nexus-actual` annotations. UI and export tooling consume these flags to group layers without inferring from IDs.
- Session awareness is mandatory for mutable layers. Definitions declare `requiresSession: true` when edits must occur under an active session per ADR-041, ensuring provenance includes `sessionId`.
- External ingest workflows (NX-113) must validate incoming rasters/vectors against the registry before persistence; incompatible layers are rejected with actionable diagnostics referencing the expected definition hash.

## Validation
- LayerDefinition validator must reject inconsistent units/precision combinations with explicit error codes and ≥ 95% branch coverage in unit tests.
- Registry publish automation must emit signed manifests and propagate updates to plugin registries within three minutes, demonstrated in CI.
- Variable-rate normalization must process yield, as-applied, and prescription samples with ≤ 0.5% numeric drift and matching color ramps against design references.

## References
- [Data model & storage requirements](../SRS/sections/08_Data_Model_Storage.md)
- [Extensibility & plugin requirements](../SRS/sections/12_Extensibility_Plugins.md)
- [ADR-009: PoseStream vector logs and layer TileStore persistence](ADR-009-posestream-vector-tilestore-persistence.md)
- [ADR-031: Official plugin bundle governance](ADR-031-official-plugin-bundle.md)

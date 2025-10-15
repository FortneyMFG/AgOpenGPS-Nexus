# Nexus Capability Registry (Draft)

## Status
Draft — tracks NX-194 and aligns with ADR-029 (mapping plugin architecture) and ADR-031
(official plugin bundle governance).

## Purpose
The capability registry defines the canonical identifiers that Core, AgIO, and plugins use
when negotiating functionality during the capabilities handshake. Each entry captures the
default semantic version, owning functional area, and the attributes that descriptors should
carry so downstream diagnostics and governance tooling can reason about feature support.

The registry is intentionally conservative: identifiers are stable, additive, and guarded by
ADR review. Consumers must treat unknown capabilities as optional and surface actionable
messages when required capabilities are absent.

## Capability catalog

| Name | Category | Default version | Summary | Attributes |
| --- | --- | --- | --- | --- |
| `guidance.control` | Guidance | 1.0.0 | Provides closed-loop autosteer control and arbitration services. | `bundle=guidance`, `mode=exclusive` |
| `guidance.telemetry` | Guidance | 1.0.0 | Publishes guidance status, engage state, and lease health for operator dashboards. | `bundle=guidance`, `mode=shared` |
| `mapping:raster` | Mapping | 1.0.0 | Publishes raster coverage tiles, rate surfaces, and diagnostics. | `bundle=mapping`, `surface=raster` |
| `mapping:vector` | Mapping | 1.0.0 | Provides vector layer ingestion, editing, and export pipelines. | `bundle=mapping`, `surface=vector` |
| `mapping:offline` | Mapping | 1.0.0 | Indicates the NullMapping provider is active and mapping features are offline. | `bundle=mapping`, `status=degraded` |
| `mapping:unavailable` | Mapping | 1.0.0 | Signals that no mapping provider is available on the host. | `bundle=mapping`, `status=absent` |
| `zones:evaluate` | Zones | 1.0.0 | Evaluates pose samples against zone constraints and publishes PoseZoneMask state. | `bundle=zones`, `role=evaluation` |
| `zones:registry` | Zones | 1.0.0 | Publishes zone registry snapshots and validation hashes for consumers. | `bundle=zones`, `role=authority` |
| `zones:edit` | Zones | 1.0.0 | Supports collaborative zone editing, journaling, and reconciliation workflows. | `bundle=zones`, `role=editor` |
| `sections.control` | Sections | 1.0.0 | Commands boom and row actuators using Core arbitration policies. | `bundle=sections`, `mode=exclusive` |
| `sections.telemetry` | Sections | 1.0.0 | Streams duty cycle, switch feedback, and diagnostics from section controllers. | `bundle=sections`, `mode=shared` |
| `fileio.import` | Data Ops | 1.0.0 | Handles import workflows for agronomic layers, jobs, and provenance manifests. | `bundle=file-io`, `mode=shared` |
| `fileio.export` | Data Ops | 1.0.0 | Exports agronomic layers, jobs, and provenance manifests in supported formats. | `bundle=file-io`, `mode=shared` |
| `replay.guidance` | Replay | 1.0.0 | Provides deterministic guidance command replay streams for analysis. | `bundle=replay`, `mode=shared` |
| `replay.pose` | Replay | 1.0.0 | Provides deterministic pose replay streams for overlay and validation. | `bundle=replay`, `mode=shared` |

## Usage notes
- **Deterministic metadata.** Core uses the registry to seed capability descriptors so that
  manifests, diagnostics, and handshake logs emit consistent versions and summaries.
- **NullMapping semantics.** When no mapping provider is available, Core advertises
  `mapping:unavailable` during the handshake. When the NullMapping shim is active, Core
  instead publishes `mapping:offline` so consumers can degrade gracefully while retaining
  deterministic behaviour.【F:docs/ADR/ADR-029-mapping-plugin-architecture.md†L61-L99】
- **Zone governance.** Zone capabilities align with the layer/zone handshake described in
  ADR-027 and the registry draft, ensuring pose gating and editing surfaces share a uniform
  contract.【F:docs/reference/layer-registry-handshake.md†L1-L58】【F:docs/ADR/ADR-027-spatial-constraints.md†L13-L33】
- **Manifest validation.** Plugin manifests must only advertise capabilities listed in the
  registry or an approved extension once ADR-031 governance tooling is live. Registry
  attributes help the loader enforce bundle policies and surface actionable diagnostics.

## Change process
1. Propose additions or amendments via an ADR referencing the desired capability name and
   semantics.
2. Update the registry with the new entry, including version, summary, and attributes.
3. Extend unit tests under `Aog.Core.Tests` to cover the new capability and ensure
   descriptors emit the expected metadata.
4. Coordinate with the contracts governance owner before shipping to guarantee compatibility
   across Core, AgIO, and plugin bundles.

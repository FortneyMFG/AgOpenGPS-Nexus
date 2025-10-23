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

The canonical identifier list now lives in the
[Core capability registry SRS reference](../SRS/references/Core_Capability_Registry.md).
Use that appendix when validating manifests or adding new entries; this
runbook focuses on how teams consume the catalog operationally.

## Usage notes
- **Deterministic metadata.** Core uses the registry to seed capability descriptors so that
  manifests, diagnostics, and handshake logs emit consistent versions and summaries.
- **NullMapping semantics.** When no mapping provider is available, Core advertises
  `mapping:unavailable` during the handshake. When the NullMapping shim is active, Core
  instead publishes `mapping:offline` so consumers can degrade gracefully while retaining
  deterministic behaviour.【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-029 - Mapping as a plugin with a minimal geospatial kernel in Core.md†L61-L99】
- **Zone governance.** Zone capabilities align with the layer/zone handshake described in
  ADR-027 and the registry draft, ensuring pose gating and editing surfaces share a uniform
  contract.【F:docs/reference/layer-registry-handshake.md†L1-L58】【F:docs/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md†L13-L33】
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
5. CI enforces this freeze window by running `CapabilityRegistryDocumentationTests` via
   `tools/ci/contracts.ps1`; new capabilities must land with corresponding documentation
   updates.

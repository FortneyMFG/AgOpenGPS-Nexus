# Layer Registry Hash Handshake (Draft)

## Status
Draft — aligns with NX-193 and ADR-068 requirements.

## Purpose
Layer controllers, UI overlays, and transport bridges must only emit telemetry after
verifying they are operating on the same catalog of `LayerDefinition` documents as
Core. The hash handshake prevents drift between controller bindings, PoseStream zone
masks, and persisted TileStore outputs by exchanging deterministic hashes before
controllers begin streaming values.

This draft captures the message fields, sequencing rules, and failure handling Core
expects during the handshake. Future ADRs will promote the contract to a formal proto
and firmware PGN mapping as part of ADR-016.

## Participants
- **Core Registry Authority** — owns the authoritative LayerDefinition store and
  publishes registry snapshots (ADR-010).
- **Layer Controller Host** — aggregation pipelines that emit section telemetry and
  derive overlays (ADR-068).
- **Observers** — UI shells, replay harnesses, or AGiO bridges that validate handshake
  outcomes for diagnostics.

## Message Fields
| Field | Direction | Description |
| --- | --- | --- |
| `handshakeId` | Controller → Core | UUID generated per attempt for tracing. |
| `nodeId` | Controller → Core | Stable identifier for the controller host (matches capability handshake). |
| `registryHash` | Controller → Core | SHA-256 digest (hex) of the LayerDefinition snapshot driving the controller. |
| `layerBindings[]` | Controller → Core | Entries containing `layerId`, `definitionHash`, `role` (`producer`/`consumer`), and optional `capabilityRef`. |
| `zoneRegistryHash` | Controller → Core | Optional SHA-256 digest for the zone registry used to evaluate `PoseZoneMask.zone_registry_hash` so gating decisions can be correlated. |
| `timestamp` | Controller → Core | ISO-8601 timestamp when the digest was computed. |
| `acceptedLayers[]` | Core → Controller | Mirrors `layerBindings` for layers Core validated. |
| `mismatches[]` | Core → Controller | Entries containing `layerId`, expected hash, provided hash, and an error code (`REGISTRY_OUT_OF_DATE`, `LAYER_UNKNOWN`, `HASH_MISMATCH`). |
| `registryVersion` | Core → Controller | Monotonic version string for the snapshot backing `registryHash`. |
| `expiresAt` | Core → Controller | Timestamp when the handshake must be renewed (controllers renew before expiry). |
| `mode` | Core → Controller | `ACTIVE` when telemetry may flow, `DEGRADED` when controllers must hold outputs. |

## Sequence
1. **Snapshot** — Controller retrieves the latest registry snapshot from Core or from
   cached signed artifacts, computes `registryHash`, and resolves bindings for every
   layer it intends to emit or consume.
2. **Request** — Controller issues `HandshakeRequest` with populated fields above.
   Requests are retried with exponential backoff capped at 5 seconds until accepted or
   until a human intervention timer (default 60 seconds) expires.
3. **Verification** — Core compares `registryHash` against the current authority. When
   they match, Core validates each `layerId`/`definitionHash` pair and echoes accepted
   entries. When mismatches occur, Core responds with `mode=DEGRADED`, fills the
   `mismatches[]` array, and publishes a `LayerRegistryMismatch` diagnostic event.
4. **Activation** — Controllers receiving `mode=ACTIVE` may begin streaming telemetry.
   Controllers must include the negotiated `registryHash` and (when provided) the
   `zoneRegistryHash` in PoseStream `PoseZoneMask.zone_registry_hash`, SectionState,
   and TileStore audit metadata.
5. **Renewal** — Controllers renew the handshake whenever `expiresAt` is reached, a
   registry invalidation event is observed, or the controller restarts.

## Failure Handling
- Controllers **must not emit** mutable layer telemetry while operating in
  `DEGRADED` mode. They surface the mismatch state to operators and continue to retry
  the handshake.
- Core emits structured diagnostics (`HandshakeRejected`, `RegistryMismatch`) carrying
  the `handshakeId` so monitoring systems can correlate retries and operator actions.
- When Core rotates the registry snapshot, it broadcasts an invalidation event on the
  capability bus prompting controllers to re-run the handshake immediately.
- Replay tooling verifies recorded telemetry by comparing stored `registryHash`
  values with the authoritative hash for the replay session and flagging deviations.

## Open Questions
- Mapping between this draft and the upcoming ADR-016 PGN payloads.
- Whether controllers should include per-layer capability versions in addition to
  definition hashes for richer diagnostics.
- Extension points for third-party controllers that only consume (not produce) layer
  data but still need registry validation.

## References
- [ADR-010: Layer registry and variable-rate framework](../../development/SRS/sections/3X_Data_Storage/32-ADR-010 - Layer registry and variable-rate framework.md)
- [ADR-027: Spatial constraints & zone policies](../../development/SRS/sections/7X_Mapping_Geospatial/72-ADR-027 - Spatial Constraints & Zone Policies.md)
- [ADR-068: Layer controllers & aggregation runtime](../../development/SRS/sections/2X_System_Architecture/21-ADR-068 - Layer Controllers & Aggregation Runtime.md)
- [SRS §03 — Communications & transports](../../development/SRS/sections/4X_Interprocess_Communications/42_Transports.md)

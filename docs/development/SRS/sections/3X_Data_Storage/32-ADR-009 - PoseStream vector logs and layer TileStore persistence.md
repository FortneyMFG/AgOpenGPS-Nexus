# ADR-009: PoseStream vector logs and layer TileStore persistence

## Status
In Review (target sign-off window: 2025-10-28 week)

**Relevant Plugin(s):** Mapping, Variable Mapping, Telemetry Logging, Replay



## Context
To support deterministic replay, analytics, and long-term storage, Nexus needs an authoritative persistence layer covering both PoseStream/SectionState vectors and aggregated tile data. Current prototypes store pose and coverage separately with inconsistent codecs, making crash recovery and transformation between formats unreliable. ADR-009 establishes shared persistence semantics so downstream ADRs (010, 012, 020) can rely on stable storage contracts.

## Decision
- Author PoseStream/SectionState vector log format with explicit chunking, compression (LZ4/Zstd), and append/compaction guarantees.
- Define a TileStore schema with value/weight/min/max channels, index layout, and deterministic transforms between vector and tile representations.
- Specify registry hash flow and crash-safety guarantees so TileStore compaction and replay integrations can assert integrity.
- Document operating modes allowing deployments to retain vectors, tiles, or both without breaking compatibility.

## Consequences
- Replay and analytics pipelines gain deterministic transforms between PoseStream vectors and tile data, improving reproducibility.
- Storage services must implement compaction and integrity checks, increasing implementation complexity but ensuring crash recovery.
- Legacy archives require migration to the new log structure, adding tooling work but yielding consistent retention policies.

## Governance Updates
- **Recovery drills.** Operations runs quarterly restore rehearsals simulating power loss, partial compaction, and filesystem corruption. Runbooks capture MTTR and remediation steps, and failures create Sev2 incidents for the persistence team.
- **Forward compatibility markers.** TileStore manifests now embed minimum/maximum reader versions. Tooling rejects incompatible payloads and offers guided export to supported formats.
- **Audit logging.** Restore operations and manual repairs append tamper-evident entries to the provenance ledger with operator, timestamp, and validation checksum.

## Validation
- TileStore compaction must maintain ≤ 1.8× write amplification under sustained 20 Hz ingest over two-hour replay fixtures.
- Vector↔tile transforms must round-trip within 0.25% aggregate error for tracked channels using analytical baselines.
- Crash-safety harness must demonstrate zero data loss when power is interrupted mid-write, verified via fsync instrumentation.

## References
- [Data model & storage requirements](../sections/3X_Data_Storage/32_Persistence_Formats.md)
- [ADR-007: PoseStream and SectionState architecture](ADR-007-posestream-sectionstate-architecture.md)
- [ADR-010: Layer registry and variable-rate framework](ADR-010-layer-registry-variable-rate.md)
- [ADR-020: Determinism, replay, and CI](ADR-020-determinism-replay-ci.md)

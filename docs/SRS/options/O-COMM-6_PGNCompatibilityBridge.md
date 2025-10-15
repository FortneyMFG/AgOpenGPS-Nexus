# O-COMM-6: PGN compatibility bridge for new transports

## Summary
Maintain UDP/serial PGN behavior while introducing a core API (gRPC/WebSocket) by standing up a compatibility bridge. The bridge
accepts legacy PGNs, validates them against the [baseline catalog](../references/AgIO_PGN_Baseline.md), translates them into typed
events for the Core, and emits PGNs for legacy hardware.

## Details
- Runs as part of the Linux Core (or sidecar) and binds to UDP ports/serial devices identical to AgIO today.
- Parses PGNs using a shared codec library; validates CRCs, lengths, and ID ranges.
- Translates PGNs into protobuf/internal messages for guidance, section control, rate control, etc.
- Emits PGNs from Core events, preserving timing (10 Hz typical) and heartbeat semantics.
- Provides capability negotiation so new firmware can advertise extended fields while old firmware stays on legacy payloads.
- Supports SocketCAN → PGN translation to let CAN hardware appear as traditional UDP/serial modules.
- Surfaces diagnostics: missed PGNs, invalid payloads, conversion latency, handshake status.

## Pros
- Lets new Core/API evolve without breaking installed hardware.
- Provides a single place to enforce validation and monitoring of PGN traffic.
- Enables gradual migration: firmware can shift to typed API once ready.

## Cons
- Adds another component that must be kept performant and reliable.
- Requires comprehensive PGN regression tests and simulators.
- Bridge bugs could disrupt steering/rate control if not thoroughly verified.

## Risks & mitigations
- **Risk:** Latency increase in translation path → **Mitigation:** Keep bridge native (C++/Rust/C#) with lock-free queues; measure end-to-end delays.
- **Risk:** Drift between PGN spec and bridge implementation → **Mitigation:** Generate codecs from spec + CI fuzzing using recorded PGNs.
- **Risk:** Operator misconfiguration (duplicate ports) → **Mitigation:** Service templates (`systemd`) with guarded defaults.

## Borrowables
- Existing AgIO UDP monitor and serial handling code.
- Field log replayers to test translation logic.
- AOG_Dev experiments with SocketCAN and PGN normalization.

## Rough effort
M (requires new service plus extensive regression validation).

## References
- Legacy PGN documentation maintained in project wiki/forums.
- Discussions on modernizing transport while keeping PGNs for compatibility.
- [Section 03 — Communications & Transports](../sections/03_Comm_Transports.md)
- [Section 07 — Interprocess API](../sections/07_Interprocess_API.md)

## Related ADRs
- [ADR-006 — AgIO Link MCU Communications](../../ADR/ADR-006-aog-link-mcu-communications.md)
- [ADR-015 — Section Control Grouping Semantics](../../ADR/ADR-015-section-control-grouping-semantics.md)
- [ADR-020 — Determinism & Replay CI](../../ADR/ADR-020-determinism-replay-ci.md)

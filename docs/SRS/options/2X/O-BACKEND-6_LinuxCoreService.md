# O-BACKEND-6: Linux "AOG Core" service with remote frontends

## Summary
Split AgOpenGPS into a headless "Core" that runs on Linux (SBC/PC) and exposes APIs that multiple frontends can consume. The core
owns guidance, mapping, section control, PGN compatibility, and data storage. Frontends (desktop, web, tablet) connect via
gRPC/WebSocket to visualize and control the system.

## Details
- Core runs as a `systemd` service (`aog-core.service`) with watchdog + restart policies.
- Packaging targets Ubuntu/Debian via `.deb`, plus Docker images for power users; optional AppImage for GUI bundles.
- File layout standardization: `/var/lib/aog` for tiles/logs, `/etc/aog` for configs, `/usr/bin/aog-core` binaries.
- Hardware interfaces via `/dev/ttyUSB*` (GPS/IMU), SocketCAN (`can0`), UDP/TCP, and NTRIP for RTK. udev rules map serial numbers
to stable device names.
- Core exposes:
  - gRPC API (protobuf) for control/config + streaming telemetry.
  - WebSocket/JSON mirror for browser-based clients and light displays.
  - UDP/serial PGN compatibility server to keep existing modules working (translates PGNs to internal events).
- Maintains compatibility by implementing the [AgIO PGN baseline](../references/AgIO_PGN_Baseline.md) and publishing adapters for
legacy modules.
- Provides CLI/REST health endpoints (`/healthz`, `/metrics`) and structured JSONL logs for observability.
- Includes optional SocketCAN ↔ PGN bridge and remote logging.

## Readiness & dependencies
- Requires OS baseline R-OS-006 hardware profiles and supported architectures to scope pilot devices.
- Depends on transport hardening (R-COMM-012/013) and PGN bridge compatibility (R-COMM-005) before exposing production traffic.
- Service health metrics (R-BE-013) and fail-safe behavior (R-BE-014) must be documented with CI coverage (R-CI-010, R-CI-013) prior to ADR approval.
- Secrets handling and audit expectations from security slice (R-SEC-006/R-SEC-007) are prerequisites for remote API access.

## Pros
- Decouples UI evolution from real-time control logic.
- Enables headless deployments, remote dashboards, and multi-frontend setups.
- Linux ecosystem simplifies driver management (SocketCAN, systemd, packaging).
- Backwards compatible path through PGN compatibility shim.

## Cons
- Significant refactor: extract logic from existing desktop app and harden APIs.
- Requires Linux expertise for maintainers and contributors.
- More components to manage (service supervision, API gateway, TLS, etc.).

## Risks & mitigations
- **Risk:** API drift between Core and frontends → **Mitigation:** Versioned protobufs + semver policy (N and N-1 compatibility).
- **Risk:** Legacy PGN clients break → **Mitigation:** Ship compatibility shim plus field testing on representative rigs.
- **Risk:** Performance on low-end SBCs → **Mitigation:** Profile for <15% CPU at 10 Hz with 64 sections; tune I/O threads.

## Borrowables
- Reuse guidance and mapping algorithms from current C# codebase (ported to service library).
- Adapt AgIO UDP monitor tooling for diagnostics against the gRPC/WS bridge.
- Use existing replay logs to validate service outputs.

## Rough effort
L (multi-phase). Requires architecture refactor, new service host, API contracts, packaging, and compatibility testing.

## References
- Community discussions on cross-platform goals and splitting UI/business logic (Telegram/GitHub threads, Feb 2024).
- [Section 42 — Transports](../sections/4X_Interprocess_Communications/42_Transports.md)
- [Section 21 — System Decomposition & Boundaries](../sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md)
- [Section 91 — UI Shell & Layout](../sections/9X_Frontends_Ops/91_UI_Shell_Layout.md)

## Related ADRs
- [ADR-004 — Composite Simulation](../../ADR/ADR-004-composite-simulation.md)
- [ADR-006 — AgIO Link MCU Communications](../../ADR/ADR-006-aog-link-mcu-communications.md)
- [ADR-020 — Determinism & Replay CI](../../ADR/ADR-020-determinism-replay-ci.md)
- [ADR-028 — Stack Boundaries](../../ADR/ADR-028-stack-boundaries.md)

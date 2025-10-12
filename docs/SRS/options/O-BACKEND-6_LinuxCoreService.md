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

# 21 — System Decomposition & Boundaries (Status: aligned with ADR-068 layer runtime)

## Problem statement
Map the guidance, mapping, field data, and rules services that power the application and how they scale to future architectures.

## Requirements (from contributors)
- R-BE-000 (MUST, current-AgOpenGPS): Retain the ApplicationCore orchestration that wires presenters, view models, and streamers for field management.【F:SourceCode/AgOpenGPS.Core/ApplicationCore.cs†L10-L45】
- R-BE-001 (MUST, current-AgOpenGPS): Continue shipping the field streamer stack that handles boundaries, tram lines, worked area, and recorded paths on disk.【F:SourceCode/AgOpenGPS.Core/Streamers/Field/FieldStreamer.cs†L7-L107】
- R-BE-002 (SHOULD, current-AgOpenGPS): Keep map tile and OpenGL render helpers accessible to both WinForms and WPF consumers.【F:SourceCode/GPS/AgOpenGPS.csproj†L39-L48】【F:SourceCode/AgOpenGPS.Core/AgOpenGPS.Core.csproj†L7-L15】
- R-BE-003 (SHOULD): Provide APIs for automation (e.g., headland, auto-steer) without breaking existing logic loops.
- R-BE-010 (MUST, proposed-variable-layer): Introduce per-section layer controllers that normalize inputs, aggregate overlap math, and expose quality metrics while keeping binary coverage intact.【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L1-L28】
- R-BE-011 (SHOULD, proposed-variable-layer): Separate IO ingestion, aggregation, and rendering via immutable snapshots to protect frame rate and simplify testing.【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L29-L46】【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L7-L18】
- R-BE-004 (SHOULD, proposed-LinuxCore): Extract the business logic into a headless service with defined API boundaries, packaging, and health endpoints while keeping today’s in-process host for Windows builds until parity is proven.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L1-L44】
- R-BE-012 (COULD, proposed-LinuxCore): Provide compatibility shims (PGN bridge, SocketCAN adapters) managed by the core service rather than each UI.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L16-L44】【F:docs/SRS/options/4X/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】
- R-BE-013 (SHOULD, service health): Define target service health metrics for the Core and layer controllers (steady-state CPU <20% on reference hardware, <500 MB RAM, restart <30 s with persisted state replay) before approving ADRs that depend on them.
- R-BE-014 (SHOULD, fail-safe): Specify how the system degrades when the Core, PGN bridge, or controller services drop offline (e.g., auto-disable remote control, surface operator alerts, maintain manual override paths) so safety-critical actions remain bounded.
- R-BE-020 (SHOULD, proposed-composite-sim): Provide a deterministic composite simulation loop (fixed-step clock + seeded RNG) that can drive all Core services and plugins in lockstep while letting hardware inputs preempt simulated values topic-by-topic.

## Options
- O-BE-0: Status quo — in-process C# services anchored in `AgOpenGPS.Core` with shared streamers.
- O-BE-1: Modularize into .NET worker services (gRPC/Web APIs).
- O-BE-2: Move to containerized microservices (e.g., navigation, mapping) talking over a bus.
- O-BE-3: Hybrid — keep real-time services local, push heavy analytics to cloud.
- O-BE-4: Scriptable engine embedded via Lua/Python for business rules.
- O-BE-5: [Layer controllers with aggregation pipelines](../options/6X/O-BACKEND-4_LayerControllers.md) — Metadata-driven ingestion + mapping snapshots.
- O-BE-6: [Linux Core service split from UI](../options/2X/O-BACKEND-6_LinuxCoreService.md) — Headless daemon + API bridge + packaging.
- O-BE-7: AgIO gRPC host with swappable Windows/Linux/Sim backends shipping unified NuGet contracts for Core/UI/Plugins.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L9-L47】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-BE-0 | Zero-deploy, proven integrations | Tight coupling to desktop lifecycle | Harder to scale across machines | `AgOpenGPS.Core` streamers |
| O-BE-1 | Cleaner interfaces, testable | Requires hosting infrastructure | Adds latency if remote | Existing presenter contracts |
| O-BE-2 | Scales analytics | Highest complexity | Network partitions hurt | PGN adapters |
| O-BE-3 | Balanced compute placement | Requires sync fabric | Drift between local/cloud states | Field replay tooling |
| O-BE-4 | Rapid customization | Sandbox & safety concerns | Script mistakes impact field ops | Current headland logic |
| O-BE-5 | Rich telemetry, deterministic pipelines | Large refactor touching many subsystems | Regression risk in overlap math | Mapping + replay harnesses |
| O-BE-6 | Decouples UI from real-time control, packages for Linux | Requires new hosting, monitoring, and compatibility testing | API or bridge regression breaks rigs | Linux Core service plan |
| O-BE-7 | Keeps Core identical across OSes, isolates hardware specifics, supports simulation/replay | Needs disciplined ABI/version governance + CI on both OSes | Backend bugs hit every client; requires vendor SDK wrappers | .NET 8 + Avalonia stack |

## Evaluation criteria
Determinism, offline resilience, ease of customization, testability, deployment footprint.

## Current sentiment
- Keep the current in-process services while cataloging seams where dedicated processes (e.g., telemetry recorder) make sense.
- Broad agreement that the layer-controller refactor should land with replay coverage before any microservice work proceeds.【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L47-L58】【F:docs/SRS/options/9X/O-TEST-4_LayerReplayCI.md†L7-L27】
- The AgIO gRPC host with .NET 8 backends is now positioned as the preferred modernization track because it keeps Core logic identical across OSes and rides on shared NuGet contracts for plugins and UI.【F:docs/SRS/options/1X/O-STACK-1_DotNet8Avalonia.md†L9-L79】
- Contributors want to scope a Linux Core pilot that keeps the Windows host running in parallel until PGN compatibility and performance targets are proven in the field.【F:docs/SRS/options/2X/O-BACKEND-6_LinuxCoreService.md†L21-L44】【F:docs/SRS/options/4X/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】
- Simulation modernization must keep the timeline authoritative inside the Core so replay, plugin simulators, and physical hardware can blend predictably without reimplementing routing logic per executable.

## Open questions
- Where do we draw the boundary between UI thread work and background services today?
- Which services must be isolated before we can offer remote supervision?

## Related ADRs

- [ADR-004 — Composite Simulation](../../ADR/ADR-004-composite-simulation.md)
- [ADR-010 — Layer Registry & Variable Rate](../../ADR/ADR-010-layer-registry-variable-rate.md)
- [ADR-020 — Determinism & Replay CI](../../ADR/ADR-020-determinism-replay-ci.md)
- [ADR-028 — Stack Boundaries](../../ADR/ADR-028-stack-boundaries.md)

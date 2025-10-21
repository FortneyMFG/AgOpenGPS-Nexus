# 21 — System Decomposition & Boundaries
*(Status: aligned with ADR-068 layer runtime)*

**Section ID:** 21
**Version:** 0.2.0
**Editors:** @nexus-specs, @layer-wg
**Last Updated:** 2025-02-14
**Related Sections:** 22, 23, 24
**Upstream Dependencies:** 11, 41, 42
**Downstream Impacts:** 51, 61, 81, 91, 96

---

## 21.1 Purpose & Scope

Define the major runtime seams of Nexus so guidance, mapping, telemetry, and automation services evolve without breaking determinism or legacy rigs.
This section establishes the components owned by the Core service, UI shells, and plugin surfaces, and it traces how proposed refactors (layer controllers, Linux Core, PGN bridge) satisfy the roadmap.【F:SourceCode/AgOpenGPS.Core/ApplicationCore.cs†L10-L45】【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L1-L58】

---

## 21.2 Context

- Retains the proven in-process orchestration so Windows deployments continue to function during modernization.【F:SourceCode/AgOpenGPS.Core/ApplicationCore.cs†L10-L45】
- Must expose seams that let deterministic replay, telemetry capture, and headless automation reuse the same business logic.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L14-L46】
- Linux Core pilots rely on the same contracts and PGN compatibility layer to avoid fragmenting plugins and operator workflows.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】
- Out of scope: UI look-and-feel, field data schemas, and agronomic analytics algorithms (covered in 8X, 9X, and 6X sections).

---

## 21.3 Legacy Comparison

| Area / Theme | Legacy Behavior | Identified Limitation | Modernization Opportunity | Reference / Source |
|--------------|-----------------|-----------------------|---------------------------|--------------------|
| Architecture | Monolithic `ApplicationCore` wires presenters, streamers, and AgIO within a single process.【F:SourceCode/AgOpenGPS.Core/ApplicationCore.cs†L10-L45】 | Tight coupling to WinForms lifecycle and manual threading control. | Introduce Core service seams with deterministic controllers and replayable buses.【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L29-L58】 |
| Performance | Field streamers manage overlap math directly against raw GNSS samples.【F:SourceCode/AgOpenGPS.Core/Streamers/Field/FieldStreamer.cs†L7-L107】 | Hard to benchmark or isolate regressions; no shared telemetry budgets. | Adopt layer controllers and SimBus metrics to monitor CPU (<20%) and restart (<30 s) targets.【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L47-L58】 |
| UX / Config | Configurations live near UI hosts with ad-hoc overrides and manual PGN wiring. | Risky for remote deployments; no unified health signals. | Push compatibility shims and configuration policy into the Core daemon with telemetry surfacing.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L16-L64】 |

---

## 21.4 Definitions

| Term | Definition |
|------|------------|
| Core Service | Headless runtime that owns guidance logic, telemetry, and deterministic timing. |
| Layer Controller | Pipeline element that normalizes sensor feeds and emits immutable coverage snapshots. |
| PGN Bridge | Compatibility shim translating legacy AgIO PGNs into Core events. |
| Composite Simulation Fabric | SimClock + SimBus orchestration that blends hardware, replay, and simulation inputs. |

---

## 21.5 Requirements

| ID | Priority | Category | Summary | Source / C-IDs | Key Metrics / Verification |
|----|----------|----------|---------|----------------|-----------------------------|
| R-BE-000 | MUST | Legacy Parity | Maintain `ApplicationCore` orchestration for existing Windows rigs during transition. | Field streamer backlog | Manual regression pass on legacy WinForms host.【F:SourceCode/AgOpenGPS.Core/ApplicationCore.cs†L10-L45】 |
| R-BE-001 | MUST | Field Data | Preserve streamer stack for boundaries, tram lines, worked area, and stored paths. | Legacy AgOpenGPS backlog | Replay scenarios confirm identical geometry outputs.【F:SourceCode/AgOpenGPS.Core/Streamers/Field/FieldStreamer.cs†L7-L107】 |
| R-BE-002 | SHOULD | Rendering | Keep map tile/OpenGL helpers shareable between WinForms and WPF/Avalonia. | Desktop roadmap | UI smoke tests across hosts.【F:SourceCode/GPS/AgOpenGPS.csproj†L39-L48】 |
| R-BE-003 | SHOULD | Automation | Provide automation APIs without breaking current logic loops. | Automation WG | API contract review; integration harness. |
| R-BE-004 | MUST | Service Health | Extract logic into headless service with packaging, API boundaries, and health endpoints. | Linux Core plan | Container smoke tests + health probes.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L64】 |
| R-BE-010 | MUST | Variable Rate | Introduce layer controllers with normalized inputs and aggregation metrics. | Layer controller proposal | Deterministic replay within ±1% coverage error.【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L1-L58】 |
| R-BE-011 | SHOULD | Observability | Separate IO, aggregation, rendering via immutable snapshots. | Replay WG | Replay CI shows no frame drops. |
| R-BE-012 | COULD | Compatibility | Ship PGN/SocketCAN shims managed by Core service. | PGN bridge study | Linux + Windows PGN regression harness. |
| R-BE-013 | SHOULD | Health Metrics | Define Core CPU (<20%), RAM (<500 MB), restart (<30 s) budgets prior to dependent ADRs. | Operations WG | Continuous telemetry alarms. |
| R-BE-014 | SHOULD | Fail-Safe | Specify degraded modes for Core or PGN bridge outages. | Safety review | Field test checklists. |
| R-BE-020 | SHOULD | Simulation | Provide deterministic composite simulation loop for services and plugins. | Simulation WG | Sim replay vs hardware parity within tolerance.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L14-L46】 |

### 21.5.1 Requirement Sources & Rationale

| Req ID | Source (issue/discussion/standard) | Rationale |
|--------|------------------------------------|-----------|
| R-BE-000 | Legacy parity review (2024-11-12) | Prevent regressions during modernization. |
| R-BE-004 | Linux Core pilot notes (2025-01) | Enable cross-OS deployments with identical logic. |
| R-BE-010 | Layer controller prototype replay | Achieve deterministic coverage math with telemetry. |
| R-BE-020 | Simulation working group minutes | CI and plugin validation depend on unified timing. |

---

## 21.6 Acceptance Criteria & Verification

Deterministic replay runs, CI smoke tests, and manual rig validation must demonstrate the Core meets all MUST requirements before approving dependent ADRs.
Automation APIs, PGN shims, and UI bindings require targeted integration tests prior to rollout.

### 21.6.1 Requirement-to-Verification Map

| Req ID | Verification Type | Artifact / Location | Pass/Fail Threshold |
|--------|-------------------|---------------------|---------------------|
| R-BE-000 | Manual regression | `tests/replay/legacy_winforms.md` | Identical coverage footprints |
| R-BE-004 | CI integration | `pipelines/linux-core-smoke.yml` | Health endpoints return 200 within 30 s |
| R-BE-010 | Replay harness | `tests/replay/layer_controller_scenarios.json` | Coverage error ≤1% vs baseline |
| R-BE-020 | Simulation benchmark | `bench/simbus_timing.md` | Loop jitter ≤2 ms p95 |

---

## 21.7 Constraints

- Core service MUST target .NET 8 and remain portable across Windows and Debian-based Linux distributions.【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L47】
- Deterministic SimClock/SimBus MUST govern plugin interactions to avoid diverging timing implementations.【F:docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md†L14-L46】
- PGN compatibility MUST be preserved until all dependent hardware fleets migrate to modern APIs.【F:docs/SRS/sections/4X_Interprocess_Communications/42_Transports.md†L158-L205】

---

## 21.12 Option Evaluation

### 21.12.1 Option Catalog

| Option | Summary |
|--------|---------|
| 21-O0 | Status quo: in-process C# services anchored in `AgOpenGPS.Core`. |
| 21-O1 | Modular worker services exposing gRPC/Web APIs. |
| 21-O2 | Containerized microservices communicating via message bus. |
| 21-O3 | Hybrid compute: local real-time services with cloud analytics. |
| 21-O4 | Scriptable engine (Lua/Python) for business rules. |
| 21-O5 | Layer controllers with aggregation pipelines (metadata-driven ingestion + immutable snapshots).【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L1-L58】 |
| 21-O6 | Linux Core headless service with remote frontends (API bridge + packaging).【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】 |
| 21-O7 | AgIO gRPC host with shared NuGet contracts across Windows/Linux.【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L79】 |

### 21.12.2 Scoring Criteria

Determinism, offline resilience, ease of customization, testability, deployment footprint.

### 21.12.3 Scoring Evidence

| Criterion | 21-O5 Justification | 21-O6 Justification | 21-O7 Justification |
|-----------|---------------------|---------------------|---------------------|
| Determinism | Immutable snapshots maintain replay fidelity in CI.【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L20-L58】 | API bridge keeps authoritative Core timeline when frontends reconnect.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L13-L62】 | Shared contracts ensure identical logic across OS builds.【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L79】 |
| Operability | Layer health metrics expose CPU/RAM budgets. | systemd packaging + health endpoints provide observability. | NuGet-hosted contracts ease CI/backwards compatibility. |
| Maintainability | DI-registered controllers isolate telemetry math. | Centralized Core simplifies plugin compatibility shims. | Unified ABI reduces duplication for UI/Plugins. |

### 21.12.4 Weighted Scoring Table

| Criterion | Weight | 21-O5 | 21-O6 | 21-O7 |
|-----------|--------|-------|-------|-------|
| Determinism | 0.30 | 4.5 | 4.0 | 4.0 |
| Operability | 0.20 | 3.5 | 4.5 | 3.5 |
| Maintainability | 0.20 | 4.0 | 3.5 | 4.5 |
| Deployment Footprint | 0.15 | 3.0 | 3.5 | 4.0 |
| Extensibility | 0.15 | 4.0 | 3.5 | 4.5 |
| **Weighted Total** | **1.0** | **3.85** | **3.95** | **4.15** |

### 21.12.5 Decision Summary

**Selected Option:** 21-O7 — AgIO gRPC host with shared NuGet contracts.
**Rationale:** Highest weighted total; preserves determinism while enabling cross-OS deployments and plugin parity.
**Formal Record:** [21-ADR-068 - Layer Controllers & Aggregation Runtime.md](21-ADR-068 - Layer Controllers & Aggregation Runtime.md)

---

## 21.13 Evaluation & Verification

Benchmarks track replay jitter, CPU/RAM ceilings, and PGN bridge fidelity.
Acceptance criteria require automated replay regression suites and manual CM5 field checks before promoting releases.

---

## 21.14 Implementation Policy

- Register controllers and services through dependency injection to keep plugin discovery deterministic.
- Store configuration under `/etc/aog` (service) and `%PROGRAMDATA%\AgOpenGPS` (Windows) with schema validation (see §24).
- All new telemetry publishers MUST attach SimBus topic metadata and health metrics for dashboard surfacing.

---

## 21.15 Community Sentiment

- Contributors favor incremental Core seam extraction while keeping in-process mode for legacy rigs.【F:docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md†L1-L62】
- Layer-controller refactor is prioritized alongside replay coverage before expanding microservice ambitions.【F:docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md†L47-L58】
- Working group supports gRPC-based AgIO host for shared contracts across OS platforms.【F:docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md†L9-L79】

### 21.15.1 Section Change Log

| Date | Summary | PR / Issue |
|------|---------|------------|
| 2025-02-14 | Reformatted to SRS v2 template; updated option scoring. | #0000 |
| 2024-12-10 | Added layer controller modernization requirements. | #0000 |

---

## 21.16 Traceability

| Requirement ID | Related Option(s) | ADR(s) | Verification Artifact | Implementation Reference |
|----------------|-------------------|--------|-----------------------|--------------------------|
| R-BE-000 | 21-O0, 21-O7 | — | `tests/replay/legacy_winforms.md` | `SourceCode/AgOpenGPS.Core/ApplicationCore.cs` |
| R-BE-004 | 21-O6, 21-O7 | 21-ADR-028 | `pipelines/linux-core-smoke.yml` | `docs/SRS/sections/2X_System_Architecture/21-O6 - Linux Core service with remote frontends.md` |
| R-BE-010 | 21-O5, 21-O7 | 21-ADR-068 | `tests/replay/layer_controller_scenarios.json` | `docs/SRS/sections/2X_System_Architecture/21-O5 - Layer controllers with aggregation pipelines.md` |
| R-BE-020 | 21-O5, 21-O7 | 21-ADR-004 | `bench/simbus_timing.md` | `docs/SRS/sections/2X_System_Architecture/21-ADR-004 - Establish the composite simulation fabric (SimClock + SimBus).md` |

---

## 21.17 Conformance

An implementation **conforms** when:
1. All **MUST** requirements (R-BE-000, R-BE-001, R-BE-004, R-BE-010, R-BE-020) are verified by mapped artifacts.
2. All **SHOULD** requirements include either verification evidence or documented exceptions approved by the working group.
3. No **MUST NOT** constraint is violated and PGN compatibility remains intact during rollout.

---

## Standards Context

This section aligns with ISO/IEC/IEEE 29148:2018 requirement structure and IEEE 1016:2017 design description expectations for interface boundaries and traceability.

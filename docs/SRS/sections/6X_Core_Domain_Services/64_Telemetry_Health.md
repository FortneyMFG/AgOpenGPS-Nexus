# 64 — Telemetry & Health (Status: collecting proposals)

## Problem statement
Detail how we observe system health, log events, and surface telemetry (packet rates, errors) to operators and maintainers.

## Requirements (from contributors)
- R-TH-000 (MUST, current-AgIO): Retain UDP and serial monitor dialogs that log and export raw traffic for troubleshooting.【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L8-L100】【F:SourceCode/AgIO/Source/Forms/FormSerialMonitor.cs†L1-L160】
- R-TH-001 (MUST, current-AgIO): Keep event log viewers that consolidate historical and in-session diagnostics.【F:SourceCode/AgIO/Source/Forms/FormEventViewer.cs†L8-L72】
- R-TH-002 (SHOULD, current-AgOpenGPS): Continue exposing PGN inspector tools and message guides in the UI.【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L52-L99】
- R-TH-003 (SHOULD): Provide machine-readable telemetry feeds (e.g., metrics, status) for remote monitoring.
- R-TH-004 (SHOULD, proposed-LinuxCore): Expose `/healthz` endpoints, structured JSONL logs, and watchdog hooks from the Core service so systemd and remote dashboards can supervise it.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L21-L33】
- R-TH-010 (SHOULD, proposed-variable-layer): Surface packet-rate monitors, legend parity tests, and bad-sample counters tied to layer metadata so operators can spot telemetry degradation quickly.【F:docs/SRS/sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md†L1-L27】
- R-TH-011 (COULD, proposed-variable-layer): Publish operator guidance and overlays that borrow AgDiag tooling while gating heavy diagnostics behind opt-in toggles.【F:docs/SRS/sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md†L28-L57】
- R-TH-005 (COULD): Add health scoring/alerting that correlates GNSS quality, network status, and module firmware levels.
- R-TH-012 (SHOULD, governance): Define log retention periods, alert routing expectations (local alarms vs. remote notifications), and privacy constraints when structured telemetry leaves the cab so future monitoring features align with operator consent and regional regulations.
- R-TH-020 (MUST, mapping pipeline): Document how PoseStream samples feed ribbons, heatmaps, and contour layers with deterministic interpolation so UI, analytics, and exports render identical coverage.
- R-TH-021 (SHOULD, visualization LOD): Specify near-vehicle level-of-detail, opacity stacking, and legend behaviors so rendering engines and diagnostics overlays remain consistent across desktop and headless deployments.
- R-TH-022 (MUST, constraint alerts): Emit real-time alerts and annunciators when keep-out zones inhibit guidance or when product is gated by work-disabled areas, including distance-to-violation indicators for autosteer.
- R-TH-023 (SHOULD, constraint audit): Log zone-driven gates and operator overrides (zoneId, action, plugin command, result) to the audit stream so replay and compliance reviews can trace safety decisions.
- R-TH-030 (MUST, collaborative mesh telemetry): Stream presence, trail, coverage, and layer-delta topics over the Live Telemetry Mesh with QoS tiers, stale indicators, and ACL enforcement so multi-machine crews share context safely.【F:docs/ADR/ADR-047_LiveTelemetryMesh.md†L21-L66】
- R-TH-031 (SHOULD, radio diagnostics): Expose RadioBridge counters (RSSI, retry rate, FEC usage, encryption status) in UI dashboards and logs to diagnose ELRS/LoRa links.【F:docs/ADR/ADR-048_RadioBridge.md†L21-L56】
- R-TH-032 (SHOULD, edit history telemetry): Forward `LayerEditEvent` summaries to telemetry feeds so collaborative edits appear in health dashboards and reports.【F:docs/ADR/ADR-044_ZoneDrawingFramework.md†L29-L74】
- R-TH-033 (SHOULD, weather logging health): Track weather auto-logging cadence, API import status, and sensor availability to alert operators when environmental data falls behind schedule.【F:docs/ADR/ADR-053_WeatherPlugin.md†L33-L49】
- R-TH-034 (MUST, equipment maintenance): Aggregate engine hours, hydraulic cycles, fault codes, and alert history from telemetry logs into `EquipmentHealthRecord` documents that drive maintenance schedules and predictive alerts surfaced in Device Manager dashboards.【F:docs/plugins/EquipmentHealth.md†L1-L160】
- R-TH-035 (SHOULD, maintenance workflow): Emit maintenance due/overdue events with recommended tasks, required parts, and linked work orders so TaskService can schedule service alongside field jobs.【F:docs/plugins/EquipmentHealth.md†L45-L160】【F:docs/SRS/sections/6X_Core_Domain_Services/62_Job_Lifecycle.md†L86-L109】
- R-TH-036 (SHOULD, automation safety): Provide rule evaluation telemetry (ruleId, trigger state, action result) for the Automation Engine so operators can audit why actions fired or were suppressed during sessions.【F:docs/plugins/AutomationEngine.md†L1-L150】

## Options
- O-TH-0: Status quo — Manual monitors/logs with operator-driven analysis.
- O-TH-1: Structured metrics pipeline (Prometheus/Influx) sourced from AgIO.
- O-TH-2: Cloud-based telemetry aggregation with dashboards.
- O-TH-3: Embedded analytics module that scores system health locally.
- O-TH-4: Hardware probes that feed additional diagnostics (voltage, temperature).
- O-TH-5: [Layer diagnostics and health monitoring](../6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md) — Shared overlays + counters for variable-rate telemetry.

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-TH-0 | Proven workflow, no infra | Manual effort | Issues caught late | Existing monitors |
| O-TH-1 | Automatable | Adds service dependencies | Overload low-bandwidth links | Log exporters |
| O-TH-2 | Fleet-wide insights | Requires internet | Privacy/data concerns | AgDiag cloud ideas |
| O-TH-3 | Immediate feedback | More CPU usage | False positives annoy operators | Event viewer logic |
| O-TH-4 | Deep visibility | Hardware BOM impact | Sensor failures mislead | SK21 diagnostics |
| O-TH-5 | Makes new telemetry auditable with familiar tools | Extra overlays could clutter UI | Diagnostics must stay performant | AgDiag monitors + layer metadata plan |

## Evaluation criteria
Latency, usability in the cab, offline capability, scalability, data retention policies.

## Current sentiment
- Keep existing monitors while defining minimum telemetry that should be streamed for automated alerting.
- Add layer-aware diagnostics in tandem with the PGN/schema upgrades so operators aren’t blind to quality issues.【F:docs/SRS/sections/6X_Core_Domain_Services/64-O5%20-%20Layer%20diagnostics%20and%20health%20monitoring.md†L28-L57】【F:docs/SRS/sections/4X_Interprocess_Communications/42-O5%20-%20Versioned%20variable-rate%20PGN%20suite.md†L24-L41】
- Linux service health must integrate with metrics/logging expectations before we can deploy headless rigs broadly.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L21-L33】【F:docs/SRS/sections/9X_Frontends_Ops/91-O6%20-%20Remote%20gRPC-WebSocket%20clients%20backed%20by%20the%20Linux%20Core.md†L21-L34】
- Constraint-driven alerts and logs must ship with the same telemetry plumbing so automation stays explainable when Core enforces spatial policies.

## Upcoming ADR coverage
- **ADR-011 Mapping & visualization pipeline** will answer R-TH-020 and R-TH-021 by specifying render ordering, GPU textures, interpolation rules, and overview pyramids shared between UI and replay tooling.【F:docs/ADR/ADR-roadmap.md†L51-L57】
- **ADR-019 Provenance, audit, and QA** will connect telemetry overlays to quality scores and provenance tags defined in Section 11 so dashboards expose lineage along with visualization state.【F:docs/ADR/ADR-roadmap.md†L115-L121】
- **ADR-047 Live telemetry mesh** will define presence/trail QoS, stale detection, and ACL requirements aligned with R-TH-030 and R-TH-032.【F:docs/ADR/ADR-roadmap.md†L211-L232】
- **ADR-048 RadioBridge** covers radio framing, retry policies, and diagnostics referenced by R-TH-031.【F:docs/ADR/ADR-roadmap.md†L233-L248】

## Open questions
- What metrics matter most for field reliability (packet loss, GNSS age, CPU load)?
- How do we store and sync logs between AgOpenGPS and AgIO instances?

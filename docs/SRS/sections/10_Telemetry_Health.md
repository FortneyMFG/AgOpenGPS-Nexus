# Telemetry & Health (Status: collecting proposals)

## Problem statement
Detail how we observe system health, log events, and surface telemetry (packet rates, errors) to operators and maintainers.

## Requirements (from contributors)
- R-TH-000 (MUST, current-AgIO): Retain UDP and serial monitor dialogs that log and export raw traffic for troubleshooting.【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L8-L100】【F:SourceCode/AgIO/Source/Forms/FormSerialMonitor.cs†L1-L160】
- R-TH-001 (MUST, current-AgIO): Keep event log viewers that consolidate historical and in-session diagnostics.【F:SourceCode/AgIO/Source/Forms/FormEventViewer.cs†L8-L72】
- R-TH-002 (SHOULD, current-AgOpenGPS): Continue exposing PGN inspector tools and message guides in the UI.【F:SourceCode/AgIO/Source/Forms/FormUDPMonitor.cs†L52-L99】
- R-TH-003 (SHOULD): Provide machine-readable telemetry feeds (e.g., metrics, status) for remote monitoring.
- R-TH-004 (SHOULD, proposed-LinuxCore): Expose `/healthz` endpoints, structured JSONL logs, and watchdog hooks from the Core service so systemd and remote dashboards can supervise it.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L21-L33】
- R-TH-010 (SHOULD, proposed-variable-layer): Surface packet-rate monitors, legend parity tests, and bad-sample counters tied to layer metadata so operators can spot telemetry degradation quickly.【F:docs/SRS/options/O-TELE-4_LayerDiagnostics.md†L1-L27】
- R-TH-011 (COULD, proposed-variable-layer): Publish operator guidance and overlays that borrow AgDiag tooling while gating heavy diagnostics behind opt-in toggles.【F:docs/SRS/options/O-TELE-4_LayerDiagnostics.md†L28-L57】
- R-TH-005 (COULD): Add health scoring/alerting that correlates GNSS quality, network status, and module firmware levels.
- R-TH-012 (SHOULD, governance): Define log retention periods, alert routing expectations (local alarms vs. remote notifications), and privacy constraints when structured telemetry leaves the cab so future monitoring features align with operator consent and regional regulations.
- R-TH-020 (MUST, mapping pipeline): Document how PoseStream samples feed ribbons, heatmaps, and contour layers with deterministic interpolation so UI, analytics, and exports render identical coverage.
- R-TH-021 (SHOULD, visualization LOD): Specify near-vehicle level-of-detail, opacity stacking, and legend behaviors so rendering engines and diagnostics overlays remain consistent across desktop and headless deployments.

## Options
- O-TH-0: Status quo — Manual monitors/logs with operator-driven analysis.
- O-TH-1: Structured metrics pipeline (Prometheus/Influx) sourced from AgIO.
- O-TH-2: Cloud-based telemetry aggregation with dashboards.
- O-TH-3: Embedded analytics module that scores system health locally.
- O-TH-4: Hardware probes that feed additional diagnostics (voltage, temperature).
- O-TH-5: [Layer diagnostics and health monitoring](../options/O-TELE-4_LayerDiagnostics.md) — Shared overlays + counters for variable-rate telemetry.

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
- Add layer-aware diagnostics in tandem with the PGN/schema upgrades so operators aren’t blind to quality issues.【F:docs/SRS/options/O-TELE-4_LayerDiagnostics.md†L28-L57】【F:docs/SRS/options/O-COMM-5_VariableRatePGNs.md†L24-L41】
- Linux service health must integrate with metrics/logging expectations before we can deploy headless rigs broadly.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L21-L33】【F:docs/SRS/options/O-FRONT-6_RemoteClients.md†L21-L34】

## Upcoming ADR coverage
- **ADR-011 Mapping & visualization pipeline** will answer R-TH-020 and R-TH-021 by specifying render ordering, GPU textures, interpolation rules, and overview pyramids shared between UI and replay tooling.【F:docs/ADR/ADR-roadmap.md†L141-L167】
- **ADR-019 Provenance, audit, and QA** will connect telemetry overlays to quality scores and provenance tags defined in Section 11 so dashboards expose lineage along with visualization state.【F:docs/ADR/ADR-roadmap.md†L254-L278】

## Open questions
- What metrics matter most for field reliability (packet loss, GNSS age, CPU load)?
- How do we store and sync logs between AgOpenGPS and AgIO instances?

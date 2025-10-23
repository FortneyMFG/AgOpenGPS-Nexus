# ADR Governance Telemetry Report

Generated: 2025-10-16 21:27:11 UTC

## Program Board — Section A — Foundations & Contracts

* Totals: 44 tracked / 38 done / 0 in progress / 6 planned / 0 blocked

| Ticket | Stage | State | Title | Notes |
| --- | --- | --- | --- | --- |
| NX-001 | — | done | Repo skeleton + solutions |   |
| NX-002 | — | done | ADRs for runtime, gRPC, Avalonia, and simulation model |   |
| NX-003 | — | done | Protobuf v1 contracts |   |
| NX-004 | — | done | JSON schema suite |   |
| NX-005 | — | done | Capabilities handshake service |   |
| NX-104 | Done | done | Metadata-driven variable-rate layer mapping ADR |   |
| NX-115 | Done | done | AOG-Link protocol specification |   |
| NX-116 | Done | done | Shared aog-link.proto schemas |   |
| NX-126 | Done | done | Mapping plugin architecture ADR |   |
| NX-131 | Done | done | Field job session lifecycle ADR |   |
| NX-152 | Done | done | .NET 8 runtime enforcement per ADR-001 |   |
| NX-190 | Done | done | Comprehensive ADR portfolio review | Backlog gaps captured in NX-610…NX-616 |
| NX-153 | Done | done | gRPC contract governance rollout |   |
| NX-154 | Done | done | Avalonia companion run-mode delivery |   |
| NX-155 | Done | done | Composite simulation fabric GA |   |
| NX-156 | Done | done | AOG-Link transport rollout |   |
| NX-157 | Done | done | Plugin API lease & manifest enforcement |   |
| NX-610 | Done | done | Governance telemetry automation | ADR roadmap program board, dependency digests, and review minutes publishing |
| NX-611 | Planned | planned | PoseStream and SectionState roadmap delivery | ADR-007 services, schemas, and replay fixtures |
| NX-612 | Planned | planned | TileStore durability and compaction rollout | ADR-009 crash-safety, maintenance workers, and audit tooling |
| NX-613 | Planned | planned | Layer registry and visualization expansion | ADR-010…ADR-013 registry automation, renderer cache, fusion provenance, and QA suite |
| NX-614 | Planned | planned | Prescription interop and control semantics | ADR-014…ADR-016 import/export suite, control engine updates, and CAN transport guardrails |
| NX-615 | Planned | planned | Kinematics, plugin platform, and identity governance | ADR-017…ADR-024 kinematics editor, plugin permission gate, provenance DAG, and identity UX |
| NX-616 | Planned | planned | Global retention, performance, and acceptance guardrails | ADR-025…ADR-026 retention planners plus global CI hooks for replay, CPU, interop, and crash recovery |
| NX-191 | Done | done | Zone proto + JSON schema handshake | ADR-027 spatial constraints contract release |
| NX-192 | Done | done | PoseStream zone mask proto update | ADR-027 PoseStream mask contract |
| NX-193 | Done | done | Layer registry hash handshake draft | ADR-068 layer controller registry requirements |
| NX-194 | Done | done | Capability registry expansion for mapping/zone capabilities | ADR-029 mapping kernel contracts; ADR-031 manifest governance |
| NX-195 | Done | done | Plugin manifest schema vNext with capability/lease metadata | ADR-031 official plugin bundle policy |
| NX-196 | Done | done | Job/session schema refresh | ADR-030 job lifecycle; ADR-041 session metadata |
| NX-197 | Done | done | Season organizer schema publication | ADR-040 season organizers data model |
| NX-198 | Done | done | Multi-field job envelope schema updates | ADR-043 multi-field job envelopes |
| NX-199 | Done | done | Layer edit event schema definition | ADR-044 zone drawing framework journal |
| NX-200 | Done | done | Crop layer definitions and registries | ADR-045 crop type plugin requirements |
| NX-201 | Done | done | Genetics layer definitions and registries | ADR-046 genetics plugin contracts |
| NX-202 | Done | done | Yield layer schema refresh | ADR-049 yield analytics plugin |
| NX-203 | Done | done | Cost/profit layer schema | ADR-050 cost & profit plugin |
| NX-204 | Done | done | Field health risk schema | ADR-052 field health plugin |
| NX-205 | Done | done | Weather snapshot schema extensions | ADR-053 weather & environment plugin |
| NX-206 | Done | done | Report template schema + manifest handshake | ADR-051 report builder & exports |
| NX-207 | Done | done | SRS + ADR cross-reference sweep for new layers | ADR-027…ADR-053 portfolio alignment |
| NX-208 | Done | done | Official bundle capability matrix update | ADR-031 manifest governance |
| NX-209 | Done | done | CRS normalization matrix publication | ADR-022 CRS policy |
| NX-210 | Done | done | Contracts freeze automation for new capabilities | ADR-031 governance rollout |

## Dependency Digest Highlights

* Plugins tracked: 12
* Most referenced API: core (used by 8 plugin(s))
* Most common capability: guidance.control (declared by 2 plugin(s))

### Plugin dependency matrix snapshot

| Plugin | Version | Required APIs | Required transports | Capabilities |
| --- | --- | --- | --- | --- |
| AutoSteer | 1.0.0 | agio.transport >=1.0.0, core.runtime >=1.0.0, mapping.layers >=1.0.0, pose.stream >=1.0.0 | can:can0, core://guidance, serial:/dev/ttyS1 | guidance.control |
| AutoSteer Lite | 1.2.0 | core >=1.0.0, sim >=1.0.0 | AOG-Link, core://guidance | guidance.control, guidance.telemetry |
| Device Manager | 1.0.0 | agio >=1.0.0, core >=1.0.0 | agio://inventory, core://devices | devices.firmware, devices.health, devices.inventory |
| File IO | 1.0.0 | core >=1.0.0, storage >=1.0.0 | core://storage, storage://local | fileio.export, fileio.import |
| GNSS + IMU Fusion | 1.0.0 | agio.sensors >=1.0.0, core.runtime >=1.0.0, plugins.ntrip-client >=1.0.0 | can://can0, core://imu, core://pose, serial://serial1 | navigation.imu, navigation.pose, navigation.pose.quality |
| ISOBUS Communications Bridge | 1.0.0 | aog.core.capabilities >=1.0.0, aog.core.telemetry >=1.0.0, aog.job.lifecycle >=1.0.0 | can:can0, udp:239.255.76.67:8888 | bridge.udp-mirror, isobus.task-controller, isobus.universal-terminal |
| Job Tasks | 1.0.0 | core >=1.0.0 | — | — |
| NTRIP Client | 1.0.0 | agio.transport >=1.0.0, core.runtime >=1.0.0 | core://ntrip, udp://localhost:2101 | gnss.corrections, telemetry.corrections |
| Planter Monitor | 1.0.0 | core >=1.0.0, sim >=1.0.0 | AOG-Link, SimBus | planter.monitor.analytics, planter.monitor.telemetry |
| Replay Toolkit | 1.0.0 | core >=1.0.0, sim >=1.0.0 | core://replay, storage://logs | replay.guidance, replay.pose |
| Sections Control | 1.1.0 | core >=1.0.0, sim >=1.0.0 | AOG-Link, core://sections | sections.control, sections.telemetry |
| Telemetry Logging | 1.0.0 | core >=1.0.0 | core://telemetry | telemetry.logging |

## Review Minutes

Recorded meetings: 1 | Open action items: 2 | Completed action items: 1

### 2025-02-17 — Architecture Governance Review

Monthly sync covering the ADR roadmap, plugin manifest dependencies, and telemetry publishing cadence.

**Attendees:** Core Owner, Plugins Owner, QA Steward, UI Owner

**Decisions**
- ADR-031: Manifest governance automation — ratified (Signed digests move to a weekly cadence and must surface resolver deltas in telemetry exports.)
- ADR-roadmap: Roadmap sync telemetry — acknowledged (Program board automation feeds NX backlog dashboards starting with NX-610.)

**Action Items**
- [ ] NX-610-1 — Plugins Owner: Backfill dependency digests for controller plugins into the governance artifact store. (due 2025-03-03)
- [ ] NX-610-2 — QA Steward: Attach governance telemetry JSON to the March review minutes for regression tracking. (due 2025-03-07)
- [x] NX-208-followup — Core Owner: Confirm the UI compatibility dashboard ingests the manifest digest summaries without regression. (completed 2025-02-20)

**Links**
- [Roadmap](docs/development/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md)
- [Dependency Map](docs/Plugins/nexus-plugin-dependency-map.md)

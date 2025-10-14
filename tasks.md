# Nexus Task Tracker

This backlog is the coordination surface for AIs and humans working on Nexus. Update the
status, owner, and human-verification fields as progress is made. Keep entries scoped to a
single NX ticket (≈20 minutes of focused work) unless an ADR states otherwise.

## Section Overview

### Section A — Foundations & Contracts
- [x] NX-001 Repo skeleton + solutions
- [x] NX-002 ADRs for runtime, gRPC, Avalonia, and simulation model
- [x] NX-003 Protobuf v1 contracts
- [x] NX-004 JSON schema suite
- [x] NX-005 Capabilities handshake service
- [x] NX-104 Metadata-driven variable-rate layer mapping ADR _(Done)_
- [x] NX-115 AOG-Link protocol specification _(Done)_
- [ ] NX-116 Shared aog-link.proto schemas _(Planned)_

### Section B — Core (Headless, Sim Graph)
- [x] NX-010 Core host skeleton
- [x] NX-011 Event bus interfaces + tests
- [x] NX-012 Simulation primitives
- [x] NX-013 Simulation provider registry + DAG validation
- [x] NX-014 Per-stream source routing configuration
- [x] NX-015 Parquet telemetry logger
- [x] NX-016 Core health interval hot reload
- [x] NX-017 Source routing service
- [x] NX-018 Source routing map + binder
- [x] NX-019 Routing change events + duplicate guard
- [x] NX-028 Core capabilities handshake bridge

### Section C — AGiO & Backends
- [x] NX-020 AGiO host skeleton + backend loader
- [x] NX-021 Agio.Sim adapters
- [x] NX-022 Agio.Windows NMEA auto-scan + parser
- [x] NX-023 Windows Location API fallback
- [x] NX-024 Agio.Linux serial NMEA auto-scan
- [x] NX-025 Agio.Linux gpsd provider
- [x] NX-026 TimingCaps probe
- [x] NX-027 Legacy UDP gateway skeleton
- [x] NX-029 Agio.Linux SocketCAN backend
- [x] NX-066 GNSS provider policy + TCP/UDP support
- [ ] NX-117 Bridge service host (gRPC ⇄ AOG-Link) _(Planned)_
- [ ] NX-118 gRPC ⇄ AOG-Link translator _(Planned)_
- [ ] NX-119 AOG-Link ⇄ PGN compatibility bridge _(Planned)_
- [ ] NX-120 AOG-Link Ethernet/UDP driver _(Planned)_
- [ ] NX-121 AOG-Link RS-485/serial driver _(Planned)_
- [ ] NX-122 AOG-Link CAN/CAN-FD driver _(Planned)_

### Section D — Plugins (Logic + Sim Providers)
- [x] NX-030 Plugin loader & manifest handling
- [x] NX-031 AutoSteer-Lite plugin v1
- [x] NX-032 Sections plugin v1
- [x] NX-033 PlanterMonitor plugin v1
- [x] NX-034 Sim provider bundle registrations
- [x] NX-035 Replay plugin v1
- [x] NX-036 Plugin telemetry sink
- [x] NX-037 Simulation scenario library
- [x] NX-038 Simulation performance harness _(Done)_
- [x] NX-039 Plugin compatibility CI gate _(Done)_
- [x] NX-098 ISOBUS communications plugin _(Done)_
- [x] NX-100 Combine yield monitoring plugin
- [ ] NX-114 Variable-rate controller plugin _(Planned)_

### Section E — UI (Avalonia) + Sim Bar
- [x] NX-040 Avalonia app bootstrap
- [x] NX-041 Connection/settings panel
- [x] NX-042 Map view
- [x] NX-043 Sim Bar controls
- [x] NX-044 Plugin panels
- [x] NX-045 Scenario editor
- [x] NX-046 Simulation config summary
- [x] NX-047 Plugin dashboards & charts
- [x] NX-048 Coverage & guidance overlays
- [x] NX-049 Replay analysis timeline
- [x] NX-065 UI theming + layout persistence
- [ ] NX-112 Layer-aware section map visualization _(Planned)_

### Section F — Porting from V6 (Algorithms)
- [x] NX-050 V6 math inventory
- [x] NX-051 Coverage math port + tests
- [x] NX-052 AB/curve/headland generation port
- [x] NX-053 Controller gains/tuners
- [x] NX-054 V6 AB/headland import
- [x] NX-055 Coverage analytics parity harness
- [x] NX-056 Machine profile translator
- [x] NX-057 Rate control parity validation
- [x] NX-058 Guidance tuning auto-calculations
- [x] NX-059 Ported math verification report
- [x] NX-102 V6 functionality inventory & gap analysis _(Done)_
- [ ] NX-105 Legacy background imagery import _(Planned)_
- [ ] NX-106 Legacy field overview metadata import _(Planned)_
- [ ] NX-107 Legacy flag importer & UI surfacing _(Planned)_
- [ ] NX-108 Legacy contour resume support _(Planned)_
- [ ] NX-109 Legacy recorded path import & replay _(Planned)_
- [ ] NX-110 Legacy tram line template import _(Planned)_
- [ ] NX-111 Legacy worked area history import _(Planned)_
- [ ] NX-113 External agronomic map ingest pipeline _(Planned)_

### Section G — Packaging, DevEx, Docs
- [x] NX-006 CI matrix (Win x64 + Linux arm64)
- [x] NX-007 SourceCode path normalization
- [x] NX-008 Schema validator registry upgrade
- [x] NX-009 Tooling SourceCode path fixes
- [x] NX-060 Dev scripts (`nexus run core|agio|ui`, `nexus sim`)
- [x] NX-061 Windows packaging (single-file)
- [x] NX-062 Linux (Pi) packaging
- [x] NX-063 How-to: Windows, no hardware
- [x] NX-064 How-to: Pi/CM5 quick start
- [x] NX-067 Release pipeline with signing
- [x] NX-068 Crash & telemetry opt-in service
- [x] NX-069 Installer & update channel documentation _(Done)_
- [x] NX-099 Update SRS for ISOBUS communications plugin _(Done)_
- [x] NX-101 Update SRS for combine yield monitoring plugin _(Done)_
- [x] NX-103 Sync tasks.md with tasks.csv tracker _(Done)_
- [ ] NX-116 README experiment narrative refresh _(In Progress)_
- [ ] NX-124 Spatial constraint zones ADR & SRS sync _(In Progress)_

### Section H — Safety & QA
- [x] NX-070 Heartbeats & failsafe watchdogs
- [x] NX-071 Arming/state machine
- [x] NX-072 Deterministic sim regression
- [x] NX-073 End-to-end failsafe integration tests
- [x] NX-074 Field safety validation checklist _(Done)_
- [x] NX-075 Hardware-in-the-loop automation rig _(Done)_
- [x] NX-076 Fault injection harness _(Done)_
- [x] NX-077 Safety log retention & export tooling
- [x] NX-078 QA dashboard aggregating safety metrics _(Done)_
- [x] NX-079 Post-run report generator _(Done)_

### Section I — Legacy/Teensy Compatibility
- [x] NX-080 UDP discovery + capability exchange _(Legacy-maintained)_
- [x] NX-081 PGN bridge (steer + sections) _(Legacy-maintained)_
- [x] NX-082 UART framing utility
- [x] NX-083 Legacy AB line/boundary import wizard
- [x] NX-084 Legacy configuration translation CLI
- [x] NX-085 Teensy bridge regression test suite
- [x] NX-086 Legacy migration guide & training set
- [x] NX-087 Dealer deployment toolkit
- [x] NX-088 Multi-machine sync workflow
- [x] NX-089 High-rate serial/UDP stress testing _(Legacy-maintained)_
- [x] NX-090 Legacy auto-run scenario pack
- [x] NX-091 Legacy coverage export verification _(Done)_
- [x] NX-092 Legacy data migration utility _(Done)_
- [x] NX-093 Bridging workflow knowledge base
- [x] NX-094 Field feedback telemetry aggregator
- [x] NX-095 Dealer support escalation process
- [x] NX-096 Community preview program
- [x] NX-097 1.0 launch readiness review

## Detailed Tables by Section

### Section A — Foundations & Contracts

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-001 | Repo skeleton + solutions | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Tag v0.1.0-bootstrap after completion |
| NX-002 | ADRs: language/runtime, gRPC, Avalonia, simulation model | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Link from README |
| NX-003 | Protobuf v1 (Header, Pose, Imu, SectionMask, SteerCmd/State, CanFrame, TimingCaps) | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Start Contracts Freeze 1 |
| NX-004 | JSON schemas (Core, AGiO, UI, Simulation) | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Coordinate with Schema Owner |
| NX-005 | Capabilities handshake proto/service | Done |  | — | [SRS §3.2 Capabilities Exchange](docs/SRS/NOTES.md#srs-32-capabilities-exchange) | Smoke test between Core & AGiO |
| NX-104 | ADR: Metadata-driven variable-rate layer mapping & imports | Done |  | — | [SRS §8 Data Model & Storage](docs/SRS/sections/08_Data_Model_Storage.md) | Superseded by ADR-010 roadmap planning; see ADR tracker for replacement scope |
| NX-150 | ADR roadmap: PoseStream, layers, and control revamp | In progress |  | — | [ADR roadmap](docs/ADR/ADR-roadmap.md) | Track upcoming ADR-007…ADR-020 deliverables and linked SRS requirements |
| NX-115 | AOG-Link protocol specification and reference flows | Done |  | — | [SRS §3 Communications & Transports](docs/SRS/sections/03_Comm_Transports.md) | ADR-006 + SRS updates complete |
| NX-116 | Shared `aog-link.proto` schemas with nanopb options | Planned |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Publish contracts aligned with `Aog.Abstractions` |

### Section B — Core (Headless, Sim Graph)

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-010 | Core host skeleton (Generic Host, DI, config, Serilog) | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) |  |
| NX-011 | Event bus interfaces + tests | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) |  |
| NX-012 | Sim primitives (ISimClock/Bus/Rng + fixed-step clock) | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Deterministic tests |
| NX-013 | Sim provider registry + DAG validation | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Reject circular deps |
| NX-014 | Per-stream routing settings + hot reload | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Per-stream routing |
| NX-015 | Telemetry Parquet logger | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Writes plugin telemetry via TelemetryParquetLogger |
| NX-016 | Core health interval hot reload | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Runtime config adjusts heartbeat cadence |
| NX-017 | Source routing service | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Routes topics across sim/hardware/replay |
| NX-018 | Source routing map + options binder | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Applies SourceRoutingOptions snapshots and emits events |
| NX-019 | Stream route change events + duplicate guard | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Publishes StreamRouteChangedEvent and rejects duplicate streams |
| NX-028 | Core capabilities handshake service | Done |  | — | [SRS §3.2 Capabilities Exchange](docs/SRS/NOTES.md#srs-32-capabilities-exchange) | gRPC client/service negotiates capabilities |

### Section C — AGiO & Backends

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-020 | AGiO host skeleton + backend loader | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Loads Agio.Sim by config |
| NX-021 | Agio.Sim adapter to gRPC services | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) |  |
| NX-022 | Windows COM NMEA auto-scan + parser | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Include tests with sample logs |
| NX-023 | Windows Location API fallback | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Optional provider |
| NX-024 | Linux serial NMEA auto-scan | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Reuse parser |
| NX-025 | Linux gpsd provider | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Emulated feed test |
| NX-026 | TimingCaps probe (Linux PPS/PTP) | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Report jitter |
| NX-027 | Legacy UDP gateway skeleton | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Loopback test |
| NX-029 | Agio.Linux SocketCAN backend (CAN→gRPC) | Done |  | — | [SRS Option O-STACK-1](docs/SRS/options/O-STACK-1_DotNet8Avalonia.md) | Streams CAN frames + section relays |
| NX-066 | GNSS source policy + TCP/UDP provider | Done |  | — | [SRS Option O-STACK-1](docs/SRS/options/O-STACK-1_DotNet8Avalonia.md) | Aggregates `IPositionSource` feeds |
| NX-117 | Bridge service host for gRPC ⇄ AOG-Link | Planned |  | — | [ADR-002 gRPC Contracts](docs/ADR/ADR-002-grpc-contracts.md) | Standalone daemon mediating inter-process, AOG-Link, and PGN flows |
| NX-118 | gRPC ⇄ AOG-Link translator layer | Planned |  | — | [ADR-002 gRPC Contracts](docs/ADR/ADR-002-grpc-contracts.md) | Map service calls/streams onto nanopb datagrams with ack/retry semantics |
| NX-119 | AOG-Link ⇄ PGN compatibility bridge | Planned |  | — | [SRS Option O-COMM-6](docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md) | Maintain legacy devices during migration |
| NX-120 | AOG-Link Ethernet/UDP driver | Planned |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Implement multicast/unicast transport with command retries |
| NX-121 | AOG-Link RS-485/serial driver | Planned |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | COBS framing + CRC-16 with token/slot scheduling |
| NX-122 | AOG-Link CAN/CAN-FD driver | Planned |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Implement AOG-CAN ID layout + ISO-TP / fragment support |

### Section D — Plugins (Logic + Sim Providers)

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-030 | Plugin loader & manifest handling | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Includes JSON manifest |
| NX-031 | AutoSteer-Lite plugin v1 | Done |  | — | [SRS §3.6 AutoSteer](docs/SRS/NOTES.md#srs-36-autosteer) | Pure Pursuit & Stanley controller with straight-line sim test |
| NX-032 | Sections plugin v1 | Done |  | — | [SRS §3.7 Sections Control](docs/SRS/NOTES.md#srs-37-sections-control) | Speed gate + look-ahead mask calculator with unit tests |
| NX-033 | PlanterMonitor plugin v1 | Done |  | — | [SRS §3.8 Planter Monitor](docs/SRS/NOTES.md#srs-38-planter-monitor) | Publishes `PlanterRowStatus` via event bus with telemetry headers |
| NX-034 | Sim provider registrations (bundle) | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Manifest helpers register providers with catalog |
| NX-035 | Replay plugin v1 | Done |  | — | [SRS §3.9 Replay Services](docs/SRS/NOTES.md#srs-39-replay-services) | Telemetry Parquet replay controller with play/pause/seek tests |
| NX-036 | Plugin telemetry sink | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | EventBus sink streams plugin telemetry |
| NX-037 | Simulation scenario library with curated sample configs and docs | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Bundle field-ready templates and documentation |
| NX-038 | Simulation performance and stress harness covering plugin combinations | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | CI run ensures deterministic timing under load |
| NX-039 | Plugin compatibility CI gate with versioned manifest regression tests | Done |  | — | [SRS §2.6 Extensibility](docs/SRS/NOTES.md#srs-26-extensibility) | Block incompatible plugin updates before release |
| NX-098 | ISOBUS communications plugin bridging CAN/UDP transports into Nexus routing | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Implement ISO 11783 PGN ingest + emit, handshake, and diagnostics |
| NX-100 | Combine yield monitoring plugin with layer overlays and data export | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Capture live yield/moisture feeds and expose UI dashboards |
| NX-114 | Variable-rate controller plugin consuming layer APIs | Planned |  | — | [SRS §3.7 Sections Control](docs/SRS/NOTES.md#srs-37-sections-control) | Converts imported layers into commanded rates |

### Section E — UI (Avalonia) + Sim Bar

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-040 | Avalonia app bootstrap | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Windows + Linux |
| NX-041 | Connection/settings panel | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Persist config |
| NX-042 | Map view (Skia pan/zoom + vehicle pose) | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | MapView control with viewport tests |
| NX-043 | Sim Bar controls (play/pause/seek/rate + routing UI) | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Requires NX-012/013 |
| NX-044 | Panels: Steer, Sections, Planter | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | UI binds to plugin states |
| NX-045 | Scenario editor (providers + options) | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Loaded scenario reflects in sim |
| NX-046 | Simulation config summary in UI | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Loader + main window summary panel |
| NX-047 | UI plugin dashboards with historical charts and tuning controls | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Graph history + autopilot tuning widgets |
| NX-048 | Map overlays for coverage heatmaps and guidance paths | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Visualize coverage & AB guidance |
| NX-049 | Replay analysis timeline with bookmarks and export options | Done |  | — | [SRS §3.9 Replay Services](docs/SRS/NOTES.md#srs-39-replay-services) | Provide bookmark/export tooling |
| NX-065 | UI theming and layout persistence across sessions | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Save/restore window layout and theme |
| NX-112 | Layer-aware section map visualization aligned with layer registry | Planned |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Replace binary overlay with metadata-driven layers |

### Section F — Porting from V6 (Algorithms)

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-050 | V6 math inventory | Done |  | — | [SRS §5.1 V6 Porting Inventory](docs/SRS/NOTES.md#srs-51-v6-porting-inventory) | docs/porting/V6-Inventory.md |
| NX-051 | Coverage math port + tests | Done |  | — | [SRS §5.2 Coverage Math](docs/SRS/NOTES.md#srs-52-coverage-math) | Compare vs V6 CSVs |
| NX-052 | AB/curve/headland generation port | Done |  | — | [SRS §5.3 Path Generation](docs/SRS/NOTES.md#srs-53-path-generation) | Tests green |
| NX-053 | Controller gains/tuners | Done |  | — | [SRS §5.4 Controller Gains](docs/SRS/NOTES.md#srs-54-controller-gains) | Meets error targets |
| NX-054 | Import V6 AB lines, headlands, and boundaries into Nexus formats | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | CLI + UI wizard |
| NX-055 | Coverage analytics parity harness comparing Nexus and V6 CSV outputs | Done |  | — | [SRS §5.2 Coverage Math](docs/SRS/NOTES.md#srs-52-coverage-math) | Automated diff thresholds |
| NX-056 | Machine profile translator for hydraulics and implement settings | Done |  | — | [SRS §5.6 Machine Profiles](docs/SRS/NOTES.md#srs-56-machine-profiles) | Generate Nexus profiles from V6 |
| NX-057 | Rate control parity validation for sections and planter algorithms | Done |  | — | [SRS §5.7 Rate Control](docs/SRS/NOTES.md#srs-57-rate-control) | Bench + sim comparison |
| NX-058 | Guidance tuning auto-calculations aligned with V6 behavior | Done |  | — | [SRS §5.4 Controller Gains](docs/SRS/NOTES.md#srs-54-controller-gains) | Auto-tune heuristics |
| NX-059 | Ported math verification report and documentation updates | Done |  | — | [SRS §5.8 Verification](docs/SRS/NOTES.md#srs-58-verification) | Summarize parity metrics |
| NX-102 | V6 functionality inventory & gap analysis across Nexus features | Done |  | — | [SRS §5.1 V6 Porting Inventory](docs/SRS/NOTES.md#srs-51-v6-porting-inventory) | Document Bing imagery, field outlines, layers, and other legacy flows |
| NX-105 | Legacy background imagery import and persistence | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Extend importer with BackPic.txt/.png handling |
| NX-106 | Legacy field overview metadata import | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Capture Field.txt origins and operators |
| NX-107 | Legacy flags and annotations importer + UI exposure | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Surface scouting markers in Nexus |
| NX-108 | Legacy contour coverage resume support | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Import Contour.txt buffers |
| NX-109 | Legacy recorded path import feeding replay services | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Map RecPath.txt into Nexus replay |
| NX-110 | Legacy tram line template import and planner integration | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Translate Tram.txt polygons |
| NX-111 | Legacy worked area history import for coverage bootstraps | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Bring Sections.txt history into layer registry |
| NX-113 | External agronomic map ingest pipeline | Planned |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Normalize GeoTIFF/ISOXML maps into layers |

### Section G — Packaging, DevEx, Docs

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-006 | CI matrix (Win x64 + Linux arm64) | Done | AI | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Include lint + headless sim |
| NX-007 | SourceCode path normalization | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Canonical "Nexus SourceCode" references |
| NX-008 | Schema validator registry upgrade | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | `referencing`-based loader resolves `$id` links |
| NX-009 | Tooling SourceCode path fixes | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Run scripts default to SourceCode layout |
| NX-060 | Dev scripts (`nexus run core|agio|ui`, `nexus sim`) | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Bash + PowerShell |
| NX-061 | Windows packaging | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Installer artifact |
| NX-062 | Pi/CM5 packaging (deb + systemd) | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Boots on Pi OS |
| NX-063 | How-to: Windows, no hardware | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | docs/howto/windows-no-hw.md |
| NX-064 | How-to: Pi/CM5 quick start | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | docs/howto/pi-sim.md |
| NX-067 | Release pipeline with signing and artifact promotion stages | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Promote nightly → beta → release |
| NX-068 | Crash and telemetry opt-in service with privacy controls | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Opt-in crash + analytics uploader |
| NX-069 | Installer and update channel documentation for operators | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Covers offline + OTA paths |
| NX-099 | Update SRS with ISOBUS communications plugin requirements | Done |  | — | [SRS §12 Extensibility & Plugins](docs/SRS/sections/12_Extensibility_Plugins.md) | Define PGN mappings, diagnostics, and UI references |
| NX-101 | Update SRS with combine yield monitoring plugin requirements | Done | AI | — | [SRS §15 Engine & Machine Gauges](docs/SRS/sections/15_Engine_Machine_Gauges.md) | Capture yield/moisture ingestion, calibration, overlays, and exports |
| NX-103 | Sync tasks.md with tasks.csv progress tracker | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Align statuses and backlog entries |
| NX-124 | Spatial constraint zones ADR & SRS sync | In Progress | AI | — | [SRS §3 Communications](docs/SRS/sections/03_Comm_Transports.md) | Add ZoneService requirements and constraint policies |

### Section H — Safety & QA

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-070 | Heartbeats & failsafe watchdogs | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Timeout <100 ms |
| NX-071 | Arming/state machine | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Blocks unsafe outputs |
| NX-072 | Deterministic sim regression (10s golden) | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Locks CI expectation |
| NX-073 | End-to-end failsafe integration tests in CI and bench rigs | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Combine sim + hardware watchdog validation |
| NX-074 | Field safety validation checklist and sign-off workflow | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Operator checklist + approvals |
| NX-075 | Hardware-in-the-loop automation rig for regression testing | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Bench harness for AGiO + plugins |
| NX-076 | Fault injection harness for sensors, network, and power events | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Scriptable fault scenarios |
| NX-077 | Safety log retention and export tooling with retention policy | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Archive for audits |
| NX-078 | QA dashboard aggregating simulation, hardware, and safety metrics | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Web dashboard + alerts |
| NX-079 | Post-run report generator summarizing guidance, coverage, and alarms | Done |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | PDF/CSV outputs |

### Section I — Legacy/Teensy Compatibility

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-080 | UDP discovery + caps/version exchange | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Legacy-maintained; compatibility only |
| NX-081 | PGN bridge (steer + sections minimal) | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Legacy-maintained; no new PGNs |
| NX-082 | UART framing (COBS+CRC), 921600 bps option | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Legacy-maintained utility |
| NX-083 | Legacy AB line and boundary import wizard feeding Core routes | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Support shape + CSV inputs |
| NX-084 | Legacy configuration translation CLI for profiles and machine settings | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Convert V6 config bundles |
| NX-085 | Teensy bridge regression test suite with recorded PGN sessions | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Automate nightly bench playback |
| NX-086 | Legacy migration guide and training materials for operators | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Docs/howto + training kit |
| NX-087 | Dealer deployment toolkit with scripts and checklists | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Bundled script set |
| NX-088 | Multi-machine synchronization and licensing workflow definition | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Document license + sync process |
| NX-089 | High-rate serial and UDP stress testing with soak reports | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Legacy-maintained soak logs |
| NX-090 | Legacy auto-run scenario pack with verification logs | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Provide sample fields |
| NX-091 | Legacy coverage export verification against Nexus outputs | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Compare shapefile + CSV exports |
| NX-092 | Legacy data migration utility for logs and field histories | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | CLI for migrating archives |
| NX-093 | Support knowledge base for bridging workflows and troubleshooting | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Publish to docs/support |
| NX-094 | Field feedback telemetry aggregator feeding support dashboards | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Collect anonymized feedback |
| NX-095 | Dealer support escalation process and SLA tracking | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Runbook + contacts |
| NX-096 | Community preview program with opt-in builds and survey loop | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Capture structured feedback |
| NX-097 | 1.0 launch readiness review and sign-off checklist | Done |  | — | [SRS §1.4 Release Management](docs/SRS/NOTES.md#srs-14-release-management) | Cross-team go/no-go |

## Legend

- **Status:** `Planned`, `Ready`, `In Progress`, `Review`, `Done`, `Blocked`.
- **Owner:** GitHub handle or AI tag currently responsible. Leave blank if unassigned.
- **Human QA:** Name + date once someone has confirmed the feature on hardware or a
  realistic sim run. Leave `—` until verified.
- **SRS Ref:** Link to the SRS section describing the requirement.
- **Notes:** Dependencies, follow-ups, or freeze-window considerations.

## Updating This File

- Move tickets to `Done` only after merging to `main`.
- Record freeze windows, cross-team dependencies, or ADR links in the `Notes` column.
- Append new NX-IDs within the appropriate section table; maintain the alphabetical section grouping above.

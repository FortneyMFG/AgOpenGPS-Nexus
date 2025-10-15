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
- [x] NX-116 Shared aog-link.proto schemas _(Done)_
- [ ] NX-126 Mapping plugin architecture ADR _(In Review)_
- [ ] NX-131 Field job session lifecycle ADR _(Proposed)_
- [x] NX-152 .NET 8 runtime enforcement per ADR-001 _(Done)_
- [ ] NX-190 Comprehensive ADR portfolio review _(In Review)_
- [x] NX-153 gRPC contract governance rollout _(Done)_
- [x] NX-154 Avalonia companion run-mode delivery _(Done)_
- [x] NX-155 Composite simulation fabric GA _(Done)_
- [x] NX-156 AOG-Link transport rollout _(Done)_
- [x] NX-157 Plugin API lease & manifest enforcement _(Done)_

- [ ] NX-191 Zone proto + JSON schema handshake _(Planned)_ — ADR-027 spatial constraints contract release
- [ ] NX-192 PoseStream zone mask proto update _(Planned)_ — ADR-027 PoseStream mask contract
- [ ] NX-193 Layer registry hash handshake draft _(Planned)_ — ADR-032 layer controllers registry requirements
- [ ] NX-194 Capability registry expansion for mapping/zone capabilities _(Planned)_ — ADR-029 mapping kernel contracts; ADR-031 manifest governance
- [ ] NX-195 Plugin manifest schema vNext with capability/lease metadata _(Planned)_ — ADR-031 official plugin bundle policy
- [ ] NX-196 Job/session schema refresh _(Planned)_ — ADR-030 job lifecycle; ADR-041 session metadata
- [ ] NX-197 Season organizer schema publication _(Planned)_ — ADR-040 season organizers data model
- [ ] NX-198 Multi-field job envelope schema updates _(Planned)_ — ADR-043 multi-field job envelopes
- [ ] NX-199 Layer edit event schema definition _(Planned)_ — ADR-044 zone drawing framework journal
- [ ] NX-200 Crop layer definitions and registries _(Planned)_ — ADR-045 crop type plugin requirements
- [ ] NX-201 Genetics layer definitions and registries _(Planned)_ — ADR-046 genetics plugin contracts
- [ ] NX-202 Yield layer schema refresh _(Planned)_ — ADR-049 yield analytics plugin
- [ ] NX-203 Cost/profit layer schema _(Planned)_ — ADR-050 cost & profit plugin
- [ ] NX-204 Field health risk schema _(Planned)_ — ADR-052 field health plugin
- [x] NX-205 Weather snapshot schema extensions _(Done)_ — ADR-053 weather & environment plugin
- [ ] NX-206 Report template schema + manifest handshake _(Planned)_ — ADR-051 report builder & exports
- [ ] NX-207 SRS + ADR cross-reference sweep for new layers _(Planned)_ — ADR-027…ADR-053 portfolio alignment
- [ ] NX-208 Official bundle capability matrix update _(Planned)_ — ADR-031 manifest governance
- [x] NX-209 CRS normalization matrix publication _(Done)_ — ADR-022 CRS policy
- [ ] NX-210 Contracts freeze automation for new capabilities _(Planned)_ — ADR-031 governance rollout

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

- [ ] NX-211 ZoneStore persistence service _(Planned)_ — ADR-027 ZoneService storage and indexing
- [ ] NX-212 ZoneService gRPC host & watcher plumbing _(Planned)_ — ADR-027 ZoneService implementation
- [ ] NX-213 PoseStream zone mask propagation _(Planned)_ — ADR-027 constraint mask propagation
- [ ] NX-214 Constraint gate integration into ControlArbiter _(Planned)_ — ADR-027 constraint gating
- [ ] NX-215 Layer controller runtime scaffolding _(Planned)_ — ADR-032 layer controllers
- [ ] NX-216 Layer controller DI registry & buffer pools _(Planned)_ — ADR-032 controller runtime details
- [ ] NX-217 PoseStream ingestion wiring for controllers _(Planned)_ — ADR-032 ingestion pipeline
- [ ] NX-218 Controller quality & diagnostic feeds _(Planned)_ — ADR-032 diagnostics surfacing
- [ ] NX-219 TileStore writer updates for controller outputs _(Planned)_ — ADR-032 TileStore integration
- [ ] NX-220 Deterministic replay fixtures for controllers _(Planned)_ — ADR-032 replay harness
- [ ] NX-221 JobsService host & lifecycle orchestration _(Planned)_ — ADR-030 job sessions service
- [ ] NX-222 Session autosave & journaling pipeline _(Planned)_ — ADR-041 job sessions
- [ ] NX-223 Season aggregator & sync orchestration _(Planned)_ — ADR-040 season organizers
- [ ] NX-224 Multi-field envelope aggregation pipeline _(Planned)_ — ADR-043 multi-field jobs
- [ ] NX-225 LayerEditEvent journal service _(Planned)_ — ADR-044 zone drawing framework
- [ ] NX-226 Live telemetry mesh core service _(Planned)_ — ADR-047 live telemetry mesh
- [ ] NX-227 Mesh diagnostics & ACL enforcement _(Planned)_ — ADR-047 mesh QoS/security
- [ ] NX-228 RadioBridge transport stack in Core _(Planned)_ — ADR-048 radio bridge
- [ ] NX-229 Mesh retention & offline sync workers _(Planned)_ — ADR-047 mesh retention requirements
- [ ] NX-230 Report builder service backend _(Planned)_ — ADR-051 report builder
- [ ] NX-231 Performance budget instrumentation _(Planned)_ — ADR-026 performance budgets
- [ ] NX-232 Timebase drift monitors for sessions _(Planned)_ — ADR-021 timebase sync
- [ ] NX-233 Discovery watcher updates for seasons _(Planned)_ — ADR-024 discovery & identity
- [ ] NX-234 Provenance audit expansion for new layers _(Planned)_ — ADR-019 provenance audit
- [ ] NX-235 Cross-track replay harness slice _(Planned)_ — ADR-roadmap cross-track slice plan

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
- [x] NX-117 Bridge service host (gRPC ⇄ AOG-Link) _(Done)_
- [ ] NX-118 gRPC ⇄ AOG-Link translator _(Planned)_
- [ ] NX-119 AOG-Link ⇄ PGN compatibility bridge _(Planned)_
- [ ] NX-120 AOG-Link Ethernet/UDP driver _(Planned)_
- [ ] NX-121 AOG-Link RS-485/serial driver _(Planned)_
- [ ] NX-122 AOG-Link CAN/CAN-FD driver _(Planned)_

- [ ] NX-236 AgIO ELRS adapter for RadioBridge _(Planned)_ — ADR-048 radio bridge integration
- [ ] NX-237 AgIO LoRa adapter for RadioBridge _(Planned)_ — ADR-048 radio bridge integration
- [ ] NX-238 RadioBridge provisioning & key management CLI _(Planned)_ — ADR-048 provisioning workflow
- [ ] NX-239 Radio diagnostics feed into mesh telemetry _(Planned)_ — ADR-048 diagnostics + ADR-047 mesh
- [ ] NX-240 Mesh bridge to AOG-Link gateways _(Planned)_ — ADR-047 mesh integration with legacy
- [ ] NX-241 RadioBridge firmware stubs & simulators _(Planned)_ — ADR-048 firmware integration
- [ ] NX-242 RadioBridge conformance & retry/FEC tests _(Planned)_ — ADR-048 reliability validation
- [ ] NX-243 Mesh integration with AgIO telemetry aggregator _(Planned)_ — ADR-047 mesh presence trails
- [ ] NX-244 RadioBridge provisioning documentation kit _(Planned)_ — ADR-048 provisioning docs
- [ ] NX-245 Mesh-aware legacy UDP gateway updates _(Planned)_ — ADR-047 presence integration
- [ ] NX-246 GNSS correction services bootstrap _(Planned)_ — ADR-066 GNSS correction services

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
- [ ] NX-160 Official AutoSteer plugin GA _(Planned)_
- [ ] NX-161 Official Mapping plugin GA _(Planned)_
- [ ] NX-162 Official Sections plugin GA _(Planned)_
- [ ] NX-163 Official Rate Control plugin GA _(Planned)_
- [ ] NX-164 Official Variable Mapping plugin GA _(Planned)_
- [ ] NX-165 Official ISOBUS Bridge plugin GA _(Planned)_
- [ ] NX-166 Official GNSS/IMU Fusion plugin GA _(Planned)_
- [ ] NX-167 Official NTRIP Client plugin GA _(Planned)_
- [ ] NX-168 Official Device Manager plugin GA _(Planned)_
- [ ] NX-169 Official Planter Monitor plugin GA _(Planned)_
- [ ] NX-170 Official Job Tasks plugin GA _(Planned)_
- [ ] NX-171 Official Telemetry Logging plugin GA _(Planned)_
- [ ] NX-172 Official File IO plugin GA _(Planned)_

- [ ] NX-247 Crop plugin layer ingestion pipeline _(Planned)_ — ADR-045 crop type plugin
- [ ] NX-248 Crop analytics API surface _(Planned)_ — ADR-045 rotation analytics
- [ ] NX-249 Crop report sections for report builder _(Planned)_ — ADR-045 reporting integration
- [ ] NX-250 Crop plugin regression fixtures _(Planned)_ — ADR-045 QA hooks
- [ ] NX-251 Genetics plugin layer ingestion pipeline _(Planned)_ — ADR-046 genetics plugin
- [ ] NX-252 Genetics barcode & lot tracking integration _(Planned)_ — ADR-046 barcode workflows
- [ ] NX-253 Genetics export pipelines (CSV/GeoJSON/ISOXML) _(Planned)_ — ADR-046 export formats
- [ ] NX-254 Genetics analytics callbacks _(Planned)_ — ADR-046 analytics integration
- [ ] NX-255 Genetics plugin regression fixtures _(Planned)_ — ADR-046 QA hooks
- [ ] NX-256 Yield sensor normalization module _(Planned)_ — ADR-049 yield plugin
- [ ] NX-257 Yield smoothing & binning pipeline _(Planned)_ — ADR-049 analytics pipelines
- [ ] NX-258 Yield import wizard plumbing _(Planned)_ — ADR-049 import workflows
- [ ] NX-259 Yield analytics API surface _(Planned)_ — ADR-049 analytics integration
- [ ] NX-260 Yield plugin regression fixtures _(Planned)_ — ADR-049 QA hooks
- [ ] NX-261 Cost/profit plugin ingestion & ledger _(Planned)_ — ADR-050 cost/profit plugin
- [ ] NX-262 Cost entry orchestration service _(Planned)_ — ADR-050 cost capture flows
- [ ] NX-263 Profit analytics rollups _(Planned)_ — ADR-050 analytics integration
- [ ] NX-264 Profit export pipelines _(Planned)_ — ADR-050 export formats
- [ ] NX-265 Profit plugin regression fixtures _(Planned)_ — ADR-050 QA hooks
- [ ] NX-266 Field health plugin ingestion pipeline _(Planned)_ — ADR-052 field health plugin
- [ ] NX-267 Field health analytics callbacks _(Planned)_ — ADR-052 analytics integration
- [ ] NX-268 Field health history + toggle persistence _(Planned)_ — ADR-052 historical toggles
- [ ] NX-269 Field health report sections _(Planned)_ — ADR-052 reporting integration
- [ ] NX-270 Field health plugin regression fixtures _(Planned)_ — ADR-052 QA hooks
- [ ] NX-271 Weather ingest pipeline _(Planned)_ — ADR-053 weather plugin
- [ ] NX-272 Weather sensor adapter integrations _(Planned)_ — ADR-053 sensor integrations
- [ ] NX-273 Weather overlay data feed _(Planned)_ — ADR-053 visualization pipeline
- [ ] NX-274 Weather report sections _(Planned)_ — ADR-053 reporting integration
- [ ] NX-275 Weather plugin regression fixtures _(Planned)_ — ADR-053 QA hooks
- [ ] NX-276 Autosteer plugin constraint gating updates _(Planned)_ — ADR-027 gating + ADR-033 guidance planner
- [ ] NX-277 Sections plugin constraint gating updates _(Planned)_ — ADR-027 gating
- [ ] NX-278 Guidance lane publishing contracts _(Planned)_ — ADR-033 guidance planner
- [ ] NX-279 Turn planner integration in guidance plugin _(Planned)_ — ADR-033 guidance planner
- [ ] NX-280 Guidance plugin regression suite _(Planned)_ — ADR-033 QA coverage
- [ ] NX-281 Mapping plugin zone overlay updates _(Planned)_ — ADR-027 zones + ADR-029 mapping kernel
- [ ] NX-282 Variable rate plugin zone gating _(Planned)_ — ADR-027 gating semantics
- [ ] NX-283 Device Manager plugin capability surfacing _(Planned)_ — ADR-031 compatibility dashboard
- [ ] NX-284 Plugin manifest compliance CI gate _(Planned)_ — ADR-031 manifest governance
- [ ] NX-285 Telemetry logging plugin season/session updates _(Planned)_ — ADR-040/041 lifecycle data
- [ ] NX-286 Telemetry logging mesh event capture _(Planned)_ — ADR-047 live telemetry mesh
- [ ] NX-287 Telemetry export updates for new layers _(Planned)_ — ADR-051 report builder + new layers
- [ ] NX-288 Job Tasks plugin season/session orchestration _(Planned)_ — ADR-030/040 lifecycle
- [ ] NX-289 Job Tasks plugin preset/task orchestration _(Planned)_ — ADR-032 presets & layouts
- [ ] NX-290 Job Tasks plugin regression fixtures _(Planned)_ — ADR-032 orchestration QA

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
- [ ] NX-130 Presets & layout linking ADR _(Proposed)_

- [ ] NX-291 Zone editor toolbar integration _(Planned)_ — ADR-044 zone drawing framework
- [ ] NX-292 Zone override toggles & policy UX _(Planned)_ — ADR-027 constraint UX
- [ ] NX-293 Zone import/export workflows _(Planned)_ — ADR-027 interop contracts
- [ ] NX-294 Season navigator UI flows _(Planned)_ — ADR-040 season organizers
- [ ] NX-295 Session start/stop UI refresh _(Planned)_ — ADR-041 job sessions UX
- [ ] NX-296 Multi-field job selection UX _(Planned)_ — ADR-043 multi-field envelopes
- [ ] NX-297 Preset switcher with orchestration status _(Planned)_ — ADR-032 presets & layout linking
- [ ] NX-298 Layout diff viewer & rollback UX _(Planned)_ — ADR-032 presets & layout linking
- [ ] NX-299 Metadata-driven dashboard refactor _(Planned)_ — ADR-034 metadata dashboards
- [ ] NX-300 Inspector & legend components _(Planned)_ — ADR-034 inspector surfaces
- [ ] NX-301 Dashboard automation test harness _(Planned)_ — ADR-034 QA automation
- [ ] NX-302 Crop quick-select UI _(Planned)_ — ADR-045 crop plugin UX
- [ ] NX-303 Genetics picker & barcode UI _(Planned)_ — ADR-046 genetics UX
- [ ] NX-304 Yield overlay UX updates _(Planned)_ — ADR-049 yield visualization
- [ ] NX-305 Profit heatmap & analytics UI _(Planned)_ — ADR-050 profit visualization
- [ ] NX-306 Field health severity UX _(Planned)_ — ADR-052 field health visualization
- [ ] NX-307 Weather timeline & overlay UX _(Planned)_ — ADR-053 weather visualization
- [ ] NX-308 Report builder preview & share UI _(Planned)_ — ADR-051 report builder UI
- [ ] NX-309 Device Manager compatibility dashboard _(Planned)_ — ADR-031 manifest governance UI
- [ ] NX-310 Mesh share/subscribe UI _(Planned)_ — ADR-047 live mesh UX
- [ ] NX-311 Radio provisioning UI flows _(Planned)_ — ADR-048 provisioning UX
- [ ] NX-312 Companion metadata-driven parity pass _(Planned)_ — ADR-034 remote parity

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
- [x] NX-105 Legacy background imagery import _(Done)_
- [ ] NX-106 Legacy field overview metadata import _(Planned)_
- [ ] NX-107 Legacy flag importer & UI surfacing _(Planned)_
- [ ] NX-108 Legacy contour resume support _(Planned)_
- [ ] NX-109 Legacy recorded path import & replay _(Planned)_
- [ ] NX-110 Legacy tram line template import _(Planned)_
- [ ] NX-111 Legacy worked area history import _(Planned)_
- [ ] NX-113 External agronomic map ingest pipeline _(Planned)_

- [ ] NX-313 Stanley controller parity harness _(Planned)_ — ADR-033 guidance planner porting
- [ ] NX-314 Pure pursuit control port with fixtures _(Planned)_ — ADR-033 guidance planner porting
- [ ] NX-315 Turn planner library port _(Planned)_ — ADR-033 guidance planner porting
- [ ] NX-316 Constraint-aware lookahead tuning _(Planned)_ — ADR-033 lookahead + ADR-027 gating
- [ ] NX-317 Firmware-in-loop stability validation _(Planned)_ — ADR-033 closed-loop validation

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
- [x] NX-125 Stack responsibility reference ADR _(Done)_
- [x] NX-151 Backlog update for ADR + official plugin tasks _(Done)_
- [ ] NX-116 README experiment narrative refresh _(In Progress)_
- [ ] NX-124 Spatial constraint zones ADR & SRS sync _(In Progress)_
- [x] NX-134 Official plugin dependency map & manifests _(Done)_

- [ ] NX-318 Plugin manifest governance documentation _(Planned)_ — ADR-031 manifest governance
- [ ] NX-319 Zone policy operator guide _(Planned)_ — ADR-027 constraint UX docs
- [ ] NX-320 Season/session migration playbook _(Planned)_ — ADR-040/041 lifecycle rollout
- [ ] NX-321 Mesh provisioning runbook _(Planned)_ — ADR-047/048 connectivity rollout
- [ ] NX-322 Report template catalog documentation _(Planned)_ — ADR-051 report builder
- [ ] NX-323 Performance budget telemetry dashboards _(Planned)_ — ADR-026 instrumentation rollout
- [ ] NX-324 Mesh retention & privacy operations guide _(Planned)_ — ADR-047 retention planner
- [ ] NX-325 Weather compliance export documentation _(Planned)_ — ADR-053 compliance outputs
- [ ] NX-326 Metadata-driven UI style guide _(Planned)_ — ADR-034 UI refactor
- [ ] NX-327 Plugin QA handshake update _(Planned)_ — ADR-031 manifest governance QA

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

- [ ] NX-328 Constraint fault-injection regression suite _(Planned)_ — ADR-027 gating QA
- [ ] NX-329 Mesh security and penetration tests _(Planned)_ — ADR-047/048 security validation
- [ ] NX-330 Session crash-recovery regression _(Planned)_ — ADR-041 session durability
- [ ] NX-331 Report export audit & diff tests _(Planned)_ — ADR-051 report builder QA
- [ ] NX-332 Autosteer closed-loop bench tests _(Planned)_ — ADR-033 guidance QA

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

- [ ] NX-333 Legacy zone importer & converter _(Planned)_ — ADR-027 zone interop
- [ ] NX-334 Legacy job migration tooling _(Planned)_ — ADR-040/041 season/session migration
- [ ] NX-335 Legacy multi-field envelope translator _(Planned)_ — ADR-043 multi-field interop
- [ ] NX-336 Legacy telemetry remap to new layers _(Planned)_ — ADR-049/052 telemetry parity
- [ ] NX-337 Legacy weather log migration utilities _(Planned)_ — ADR-053 weather parity

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
| NX-116 | Shared `aog-link.proto` schemas with nanopb options | Done |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Publish contracts aligned with `Aog.Abstractions` |
| NX-152 | Enforce ADR-001 .NET 8 runtime baselines across solutions and CI | Done | AI | — | [ADR-001 .NET 8 runtime](docs/ADR/ADR-001-dotnet8-runtime.md) | Runtime baseline playbook + SDK pinning in `global.json` |
| NX-190 | Comprehensive ADR portfolio review | In Review |  | — | [ADR roadmap](docs/ADR/ADR-roadmap.md) | Apply 2025 governance updates across accepted and draft ADRs |
| NX-153 | Operationalize ADR-002 gRPC contract governance and compatibility gates | Done | AI | — | [ADR-002 gRPC contracts](docs/ADR/ADR-002-grpc-contracts.md) | Checklist for clinics, golden fixtures, and release gating |
| NX-154 | Deliver ADR-003 Avalonia shell run modes (CompanionRemote/Local) | Done | AI | — | [ADR-003 Avalonia UI](docs/ADR/ADR-003-avalonia-ui.md) | Run-mode configuration + smoke test guide published |
| NX-155 | Complete ADR-004 composite simulation fabric with regression packs | Done | AI | — | [ADR-004 Composite simulation](docs/ADR/ADR-004-composite-simulation.md) | GA validation checklist covering topics, seeds, replay |
| NX-156 | Roll out ADR-006 AOG-Link transports and bridge translation layers | Done | AI | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Transport rollout guide + validation/support checklist |
| NX-157 | Implement ADR-018 plugin API leases, manifests, and AgIO migration | Done | AI | — | [ADR-018 Plugin API](docs/ADR/ADR-018-plugin-api.md) | Lease + manifest governance guide for plugin authors |
| NX-191 | Zone proto + JSON schema handshake | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 spatial constraints contract release |
| NX-192 | PoseStream zone mask proto update | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 PoseStream mask contract |
| NX-193 | Layer registry hash handshake draft | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 layer controllers registry requirements |
| NX-194 | Capability registry expansion for mapping/zone capabilities | Planned |  | — | [ADR-029](docs/ADR/ADR-029-mapping-plugin-architecture.md), [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-029 mapping kernel contracts; ADR-031 manifest governance |
| NX-195 | Plugin manifest schema vNext with capability/lease metadata | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 official plugin bundle policy |
| NX-196 | Job/session schema refresh | Planned |  | — | [ADR-030](docs/ADR/ADR-030-field-job-sessions.md), [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-030 job lifecycle; ADR-041 session metadata |
| NX-197 | Season organizer schema publication | Planned |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040 season organizers data model |
| NX-198 | Multi-field job envelope schema updates | Planned |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field job envelopes |
| NX-199 | Layer edit event schema definition | Planned |  | — | [ADR-044](docs/ADR/ADR-044_ZoneDrawingFramework.md) | ADR-044 zone drawing framework journal |
| NX-200 | Crop layer definitions and registries | Planned |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 crop type plugin requirements |
| NX-201 | Genetics layer definitions and registries | Planned |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 genetics plugin contracts |
| NX-202 | Yield layer schema refresh | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 yield analytics plugin |
| NX-203 | Cost/profit layer schema | Planned |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 cost & profit plugin |
| NX-204 | Field health risk schema | Planned |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 field health plugin |
| NX-205 | Weather snapshot schema extensions | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather & environment plugin |
| NX-206 | Report template schema + manifest handshake | Planned |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder & exports |
| NX-207 | SRS + ADR cross-reference sweep for new layers | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md), [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-027…ADR-053 portfolio alignment |
| NX-208 | Official bundle capability matrix update | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance |
| NX-209 | CRS normalization matrix publication | Done |  | — | [ADR-022](docs/ADR/ADR-022-crs-units-precision-policy.md) | Published [reference matrix](docs/reference/crs-normalization-matrix.md) |
| NX-210 | Contracts freeze automation for new capabilities | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 governance rollout |

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
| NX-211 | ZoneStore persistence service | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 ZoneService storage and indexing |
| NX-212 | ZoneService gRPC host & watcher plumbing | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 ZoneService implementation |
| NX-213 | PoseStream zone mask propagation | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 constraint mask propagation |
| NX-214 | Constraint gate integration into ControlArbiter | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 constraint gating |
| NX-215 | Layer controller runtime scaffolding | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 layer controllers |
| NX-216 | Layer controller DI registry & buffer pools | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 controller runtime details |
| NX-217 | PoseStream ingestion wiring for controllers | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 ingestion pipeline |
| NX-218 | Controller quality & diagnostic feeds | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 diagnostics surfacing |
| NX-219 | TileStore writer updates for controller outputs | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 TileStore integration |
| NX-220 | Deterministic replay fixtures for controllers | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 replay harness |
| NX-221 | JobsService host & lifecycle orchestration | Planned |  | — | [ADR-030](docs/ADR/ADR-030-field-job-sessions.md) | ADR-030 job sessions service |
| NX-222 | Session autosave & journaling pipeline | Planned |  | — | [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-041 job sessions |
| NX-223 | Season aggregator & sync orchestration | Planned |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040 season organizers |
| NX-224 | Multi-field envelope aggregation pipeline | Planned |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field jobs |
| NX-225 | LayerEditEvent journal service | Planned |  | — | [ADR-044](docs/ADR/ADR-044_ZoneDrawingFramework.md) | ADR-044 zone drawing framework |
| NX-226 | Live telemetry mesh core service | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 live telemetry mesh |
| NX-227 | Mesh diagnostics & ACL enforcement | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh QoS/security |
| NX-228 | RadioBridge transport stack in Core | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 radio bridge |
| NX-229 | Mesh retention & offline sync workers | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh retention requirements |
| NX-230 | Report builder service backend | Planned |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder |
| NX-231 | Performance budget instrumentation | Planned |  | — | [ADR-026](docs/ADR/ADR-026-performance-budgets.md) | ADR-026 performance budgets |
| NX-232 | Timebase drift monitors for sessions | Planned |  | — | [ADR-021](docs/ADR/ADR-021-timebase-clock-sync.md) | ADR-021 timebase sync |
| NX-233 | Discovery watcher updates for seasons | Planned |  | — | [ADR-024](docs/ADR/ADR-024-discovery-identity.md) | ADR-024 discovery & identity |
| NX-234 | Provenance audit expansion for new layers | Planned |  | — | [ADR-019](docs/ADR/ADR-019-provenance-audit-qa.md) | ADR-019 provenance audit |
| NX-235 | Cross-track replay harness slice | Planned |  | — | [ADR roadmap](docs/ADR/ADR-roadmap.md) | ADR-roadmap cross-track slice plan |

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
| NX-117 | Bridge service host for gRPC ⇄ AOG-Link | Done |  | — | [ADR-002 gRPC Contracts](docs/ADR/ADR-002-grpc-contracts.md) | Standalone daemon mediating inter-process, AOG-Link, and PGN flows |
| NX-118 | gRPC ⇄ AOG-Link translator layer | Planned |  | — | [ADR-002 gRPC Contracts](docs/ADR/ADR-002-grpc-contracts.md) | Map service calls/streams onto nanopb datagrams with ack/retry semantics |
| NX-119 | AOG-Link ⇄ PGN compatibility bridge | Planned |  | — | [SRS Option O-COMM-6](docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md) | Maintain legacy devices during migration |
| NX-120 | AOG-Link Ethernet/UDP driver | Planned |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Implement multicast/unicast transport with command retries |
| NX-121 | AOG-Link RS-485/serial driver | Planned |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | COBS framing + CRC-16 with token/slot scheduling |
| NX-122 | AOG-Link CAN/CAN-FD driver | Planned |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Implement AOG-CAN ID layout + ISO-TP / fragment support |
| NX-236 | AgIO ELRS adapter for RadioBridge | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 radio bridge integration |
| NX-237 | AgIO LoRa adapter for RadioBridge | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 radio bridge integration |
| NX-238 | RadioBridge provisioning & key management CLI | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 provisioning workflow |
| NX-239 | Radio diagnostics feed into mesh telemetry | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md), [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-048 diagnostics + ADR-047 mesh |
| NX-240 | Mesh bridge to AOG-Link gateways | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh integration with legacy |
| NX-241 | RadioBridge firmware stubs & simulators | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 firmware integration |
| NX-242 | RadioBridge conformance & retry/FEC tests | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 reliability validation |
| NX-243 | Mesh integration with AgIO telemetry aggregator | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh presence trails |
| NX-244 | RadioBridge provisioning documentation kit | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 provisioning docs |
| NX-245 | Mesh-aware legacy UDP gateway updates | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 presence integration |
| NX-246 | GNSS correction services bootstrap | Planned |  | — | [ADR-066](docs/ADR/ADR-roadmap.md) | ADR-066 GNSS correction services |

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
| NX-160 | Official AutoSteer plugin GA (pose fusion + actuator transport) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: mapping, GNSS/IMU fusion, AgIO; Soft: sections, device manager; transports per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-161 | Official Mapping plugin GA (field state store + AB/coverage feeds) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core store; Soft: file-io, job tasks, telemetry logging per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-162 | Official Sections plugin GA (coverage gating + IO orchestration) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: mapping + AgIO valves; Soft: rate-control, variable-mapping, telemetry per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-163 | Official Rate Control plugin GA (setpoint + actuator coordination) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: variable-mapping, AgIO; Soft: ISOBUS bridge, mapping telemetry per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-164 | Official Variable Mapping plugin GA (grid ingestion + setpoints) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: mapping layers, file-io; Soft: job-tasks, telemetry logging per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-165 | Official ISOBUS Bridge plugin GA (TC/UT translation) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core contracts + AgIO interface; Soft: rate-control diagnostics per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-166 | Official GNSS/IMU Fusion plugin GA (pose publisher) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: ntrip-client, AgIO sensors; Soft: telemetry logging, mapping overlays per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-167 | Official NTRIP Client plugin GA (RTCM streaming) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: AgIO network transport; Soft: telemetry logging, device manager per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-168 | Official Device Manager plugin GA (inventory + health) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: AgIO hardware inventory; Soft: UI shell telemetry badges per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-169 | Official Planter Monitor plugin GA (row sensing + analytics) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: AgIO row sensors + Core session store; Soft: mapping overlays, telemetry logging per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-170 | Official Job Tasks plugin GA (save/resume lifecycle) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core job services; Soft: file-io, mapping, telemetry per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-171 | Official Telemetry Logging plugin GA (replay + export) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core telemetry bus; Soft: plugin feeds (mapping, autosteer, device manager) per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-172 | Official File IO plugin GA (import/export surfaces) | Planned |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core storage APIs; Soft: mapping, variable-mapping, job-tasks per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-247 | Crop plugin layer ingestion pipeline | Planned |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 crop type plugin |
| NX-248 | Crop analytics API surface | Planned |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 rotation analytics |
| NX-249 | Crop report sections for report builder | Planned |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 reporting integration |
| NX-250 | Crop plugin regression fixtures | Planned |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 QA hooks |
| NX-251 | Genetics plugin layer ingestion pipeline | Planned |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 genetics plugin |
| NX-252 | Genetics barcode & lot tracking integration | Planned |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 barcode workflows |
| NX-253 | Genetics export pipelines (CSV/GeoJSON/ISOXML) | Planned |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 export formats |
| NX-254 | Genetics analytics callbacks | Planned |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 analytics integration |
| NX-255 | Genetics plugin regression fixtures | Planned |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 QA hooks |
| NX-256 | Yield sensor normalization module | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 yield plugin |
| NX-257 | Yield smoothing & binning pipeline | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 analytics pipelines |
| NX-258 | Yield import wizard plumbing | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 import workflows |
| NX-259 | Yield analytics API surface | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 analytics integration |
| NX-260 | Yield plugin regression fixtures | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 QA hooks |
| NX-261 | Cost/profit plugin ingestion & ledger | Planned |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 cost/profit plugin |
| NX-262 | Cost entry orchestration service | Planned |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 cost capture flows |
| NX-263 | Profit analytics rollups | Planned |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 analytics integration |
| NX-264 | Profit export pipelines | Planned |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 export formats |
| NX-265 | Profit plugin regression fixtures | Planned |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 QA hooks |
| NX-266 | Field health plugin ingestion pipeline | Planned |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 field health plugin |
| NX-267 | Field health analytics callbacks | Planned |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 analytics integration |
| NX-268 | Field health history + toggle persistence | Planned |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 historical toggles |
| NX-269 | Field health report sections | Planned |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 reporting integration |
| NX-270 | Field health plugin regression fixtures | Planned |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 QA hooks |
| NX-271 | Weather ingest pipeline | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather plugin |
| NX-272 | Weather sensor adapter integrations | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 sensor integrations |
| NX-273 | Weather overlay data feed | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 visualization pipeline |
| NX-274 | Weather report sections | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 reporting integration |
| NX-275 | Weather plugin regression fixtures | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 QA hooks |
| NX-276 | Autosteer plugin constraint gating updates | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md), [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-027 gating + ADR-033 guidance planner |
| NX-277 | Sections plugin constraint gating updates | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 gating |
| NX-278 | Guidance lane publishing contracts | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner |
| NX-279 | Turn planner integration in guidance plugin | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner |
| NX-280 | Guidance plugin regression suite | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 QA coverage |
| NX-281 | Mapping plugin zone overlay updates | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md), [ADR-029](docs/ADR/ADR-029-mapping-plugin-architecture.md) | ADR-027 zones + ADR-029 mapping kernel |
| NX-282 | Variable rate plugin zone gating | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 gating semantics |
| NX-283 | Device Manager plugin capability surfacing | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 compatibility dashboard |
| NX-284 | Plugin manifest compliance CI gate | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance |
| NX-285 | Telemetry logging plugin season/session updates | Planned |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040/041 lifecycle data |
| NX-286 | Telemetry logging mesh event capture | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 live telemetry mesh |
| NX-287 | Telemetry export updates for new layers | Planned |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder + new layers |
| NX-288 | Job Tasks plugin season/session orchestration | Planned |  | — | [ADR-030](docs/ADR/ADR-030-field-job-sessions.md) | ADR-030/040 lifecycle |
| NX-289 | Job Tasks plugin preset/task orchestration | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 presets & layouts |
| NX-290 | Job Tasks plugin regression fixtures | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 orchestration QA |

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
| NX-291 | Zone editor toolbar integration | Planned |  | — | [ADR-044](docs/ADR/ADR-044_ZoneDrawingFramework.md) | ADR-044 zone drawing framework |
| NX-292 | Zone override toggles & policy UX | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 constraint UX |
| NX-293 | Zone import/export workflows | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 interop contracts |
| NX-294 | Season navigator UI flows | Planned |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040 season organizers |
| NX-295 | Session start/stop UI refresh | Planned |  | — | [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-041 job sessions UX |
| NX-296 | Multi-field job selection UX | Planned |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field envelopes |
| NX-297 | Preset switcher with orchestration status | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 presets & layout linking |
| NX-298 | Layout diff viewer & rollback UX | Planned |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 presets & layout linking |
| NX-299 | Metadata-driven dashboard refactor | Planned |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 metadata dashboards |
| NX-300 | Inspector & legend components | Planned |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 inspector surfaces |
| NX-301 | Dashboard automation test harness | Planned |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 QA automation |
| NX-302 | Crop quick-select UI | Planned |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 crop plugin UX |
| NX-303 | Genetics picker & barcode UI | Planned |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 genetics UX |
| NX-304 | Yield overlay UX updates | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 yield visualization |
| NX-305 | Profit heatmap & analytics UI | Planned |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 profit visualization |
| NX-306 | Field health severity UX | Planned |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 field health visualization |
| NX-307 | Weather timeline & overlay UX | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather visualization |
| NX-308 | Report builder preview & share UI | Planned |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder UI |
| NX-309 | Device Manager compatibility dashboard | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance UI |
| NX-310 | Mesh share/subscribe UI | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 live mesh UX |
| NX-311 | Radio provisioning UI flows | Planned |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 provisioning UX |
| NX-312 | Companion metadata-driven parity pass | Planned |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 remote parity |

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
| NX-313 | Stanley controller parity harness | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner porting |
| NX-314 | Pure pursuit control port with fixtures | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner porting |
| NX-315 | Turn planner library port | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner porting |
| NX-316 | Constraint-aware lookahead tuning | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md), [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-033 lookahead + ADR-027 gating |
| NX-317 | Firmware-in-loop stability validation | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 closed-loop validation |

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
| NX-125 | Stack responsibility reference ADR | Done |  | — | [ADR-028](docs/ADR/ADR-028-stack-boundaries.md) | Document layer boundaries for AgIO, Core, and plugins |
| NX-151 | Backlog update for ADR + official plugin tasks | Done | AI | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Extend tasks.md/.csv with ADR coverage and plugin roadmap |
| NX-124 | Spatial constraint zones ADR & SRS sync | In Progress | AI | — | [SRS §3 Communications](docs/SRS/sections/03_Comm_Transports.md) | Add ZoneService requirements and constraint policies |
| NX-141 | Companion/mobile stack rollout documentation | In Progress | AI | — | [SRS §5 Frontends](docs/SRS/sections/05_Frontends.md) | Capture CompanionRemote, LocalInProc, and LocalOutOfProc run modes across docs |
| NX-318 | Plugin manifest governance documentation | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance |
| NX-319 | Zone policy operator guide | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 constraint UX docs |
| NX-320 | Season/session migration playbook | Planned |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040/041 lifecycle rollout |
| NX-321 | Mesh provisioning runbook | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047/048 connectivity rollout |
| NX-322 | Report template catalog documentation | Planned |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder |
| NX-323 | Performance budget telemetry dashboards | Planned |  | — | [ADR-026](docs/ADR/ADR-026-performance-budgets.md) | ADR-026 instrumentation rollout |
| NX-324 | Mesh retention & privacy operations guide | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 retention planner |
| NX-325 | Weather compliance export documentation | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 compliance outputs |
| NX-326 | Metadata-driven UI style guide | Planned |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 UI refactor |
| NX-327 | Plugin QA handshake update | Planned |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance QA |

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
| NX-328 | Constraint fault-injection regression suite | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 gating QA |
| NX-329 | Mesh security and penetration tests | Planned |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047/048 security validation |
| NX-330 | Session crash-recovery regression | Planned |  | — | [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-041 session durability |
| NX-331 | Report export audit & diff tests | Planned |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder QA |
| NX-332 | Autosteer closed-loop bench tests | Planned |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance QA |

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
| NX-333 | Legacy zone importer & converter | Planned |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 zone interop |
| NX-334 | Legacy job migration tooling | Planned |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040/041 season/session migration |
| NX-335 | Legacy multi-field envelope translator | Planned |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field interop |
| NX-336 | Legacy telemetry remap to new layers | Planned |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049/052 telemetry parity |
| NX-337 | Legacy weather log migration utilities | Planned |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather parity |


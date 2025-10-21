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
- [x] NX-126 Mapping plugin architecture ADR _(Done)_
- [x] NX-131 Field job session lifecycle ADR _(Done)_
- [x] NX-152 .NET 8 runtime enforcement per ADR-001 _(Done)_
- [x] NX-190 Comprehensive ADR portfolio review _(Done)_ — Backlog gaps captured in NX-610…NX-616
- [ ] NX-1314 Zip plugin architecture foundations — Introduce SDK surface + manifest tooling for plugin packaging
- [x] NX-1405 Mapping core/plugin boundary contracts & doc _(Done)_
- [x] NX-153 gRPC contract governance rollout _(Done)_
- [x] NX-154 Avalonia companion run-mode delivery _(Done)_
- [x] NX-155 Composite simulation fabric GA _(Done)_
- [x] NX-156 AOG-Link transport rollout _(Done)_
- [x] NX-157 Plugin API lease & manifest enforcement _(Done)_
- [ ] NX-1014 Contract baseline generator tooling resilience — Ensure baseline generator works without direct project references

- [x] NX-610 Governance telemetry automation _(Done)_ — ADR roadmap program board, dependency digests, and review minutes publishing
- [x] NX-611 PoseStream and SectionState roadmap delivery _(Done)_ — ADR-007 services, schemas, and replay fixtures
- [x] NX-612 TileStore durability and compaction rollout _(Done)_ — ADR-009 crash-safety, maintenance workers, and audit tooling
- [x] NX-613 Layer registry and visualization expansion _(Done)_ — ADR-010…ADR-013 registry automation, renderer cache, fusion provenance, and QA suite
- [x] NX-614 Prescription interop and control semantics _(Done)_ — ADR-014…ADR-016 import/export suite, control engine updates, and CAN transport guardrails
- [x] NX-615 Kinematics, plugin platform, and identity governance _(Done)_ — ADR-017…ADR-024 kinematics editor, plugin permission gate, provenance DAG, and identity UX
- [x] NX-616 Global retention, performance, and acceptance guardrails _(Done)_ — ADR-025…ADR-026 retention planners plus global CI hooks for replay, CPU, interop, and crash recovery

- [x] NX-191 Zone proto + JSON schema handshake _(Done)_ — ADR-027 spatial constraints contract release
- [x] NX-192 PoseStream zone mask proto update _(Done)_ — ADR-027 PoseStream mask contract
- [x] NX-193 Layer registry hash handshake draft _(Done)_ — ADR-068 layer controller registry requirements
- [x] NX-194 Capability registry expansion for mapping/zone capabilities _(Done)_ — ADR-029 mapping kernel contracts; ADR-031 manifest governance
- [x] NX-195 Plugin manifest schema vNext with capability/lease metadata _(Done)_ — ADR-031 official plugin bundle policy
- [x] NX-196 Job/session schema refresh _(Done)_ — ADR-030 job lifecycle; ADR-041 session metadata
- [x] NX-197 Season organizer schema publication _(Done)_ — ADR-040 season organizers data model
- [x] NX-198 Multi-field job envelope schema updates _(Done)_ — ADR-043 multi-field job envelopes
- [x] NX-199 Layer edit event schema definition _(Done)_ — ADR-044 zone drawing framework journal
- [x] NX-200 Crop layer definitions and registries _(Done)_ — ADR-045 crop type plugin requirements
- [x] NX-201 Genetics layer definitions and registries _(Done)_ — ADR-046 genetics plugin contracts
- [x] NX-202 Yield layer schema refresh _(Done)_ — ADR-049 yield analytics plugin
- [x] NX-203 Cost/profit layer schema _(Done)_ — ADR-050 cost & profit plugin
- [x] NX-204 Field health risk schema _(Done)_ — ADR-052 field health plugin
- [x] NX-205 Weather snapshot schema extensions _(Done)_ — ADR-053 weather & environment plugin
- [x] NX-206 Report template schema + manifest handshake _(Done)_ — ADR-051 report builder & exports
- [x] NX-207 SRS + ADR cross-reference sweep for new layers _(Done)_ — ADR-027…ADR-053 portfolio alignment
- [x] NX-208 Official bundle capability matrix update _(Done)_ — ADR-031 manifest governance
- [x] NX-209 CRS normalization matrix publication _(Done)_ — ADR-022 CRS policy
- [x] NX-210 Contracts freeze automation for new capabilities _(Done)_ — ADR-031 governance rollout

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

- [x] NX-211 ZoneStore persistence service _(Done)_ — ADR-027 ZoneService storage and indexing
- [x] NX-212 ZoneService gRPC host & watcher plumbing _(Done)_ — ADR-027 ZoneService implementation
- [x] NX-213 PoseStream zone mask propagation _(Done)_ — ADR-027 constraint mask propagation
- [x] NX-214 Constraint gate integration into ControlArbiter _(Done)_ — ADR-027 constraint gating
- [x] NX-215 Layer controller runtime scaffolding _(Done)_ — ADR-068 layer controllers
- [x] NX-216 Layer controller DI registry & buffer pools _(Done)_ — ADR-068 controller runtime details
- [x] NX-217 PoseStream ingestion wiring for controllers _(Done)_ — ADR-068 ingestion pipeline
- [x] NX-218 Controller quality & diagnostic feeds _(Done)_ — ADR-068 diagnostics surfacing
- [x] NX-219 TileStore writer updates for controller outputs _(Done)_ — ADR-068 TileStore integration
- [x] NX-220 Deterministic replay fixtures for controllers _(Done)_ — ADR-068 replay harness
- [x] NX-221 JobsService host & lifecycle orchestration _(Done)_ — ADR-030 job sessions service
- [x] NX-222 Session autosave & journaling pipeline _(Done)_ — ADR-041 job sessions
- [x] NX-223 Season aggregator & sync orchestration _(Done)_ — ADR-040 season organizers
- [x] NX-224 Multi-field envelope aggregation pipeline _(Done)_ — ADR-043 multi-field jobs
- [x] NX-225 LayerEditEvent journal service _(Done)_ — ADR-044 zone drawing framework
- [x] NX-226 Live telemetry mesh core service _(Done)_ — ADR-047 live telemetry mesh
- [x] NX-227 Mesh diagnostics & ACL enforcement _(Done)_ — ADR-047 mesh QoS/security
- [x] NX-228 RadioBridge transport stack in Core _(Done)_ — ADR-048 radio bridge
- [x] NX-229 Mesh retention & offline sync workers _(Done)_ — ADR-047 mesh retention requirements
- [x] NX-230 Report builder service backend _(Done)_ — ADR-051 report builder
- [x] NX-231 Performance budget instrumentation _(Done)_ — ADR-026 performance budgets
- [x] NX-232 Timebase drift monitors for sessions _(Done)_ — ADR-021 timebase sync
- [x] NX-233 Discovery watcher updates for seasons _(Done)_ — ADR-024 discovery & identity
- [x] NX-234 Provenance audit expansion for new layers _(Done)_ — ADR-019 provenance audit
- [x] NX-235 Cross-track replay harness slice _(Done)_ — ADR-roadmap cross-track slice plan
- [x] NX-341 GitHub Actions release packaging (Win/Linux zips) _(Done)_ — SRS §2.7 Packaging & DevEx
- [x] NX-414 Multi-steer configurator export schema GA _(Done)_ — ADR-067 axle-centric profiles
- [x] NX-452 Core ingestion service for axle-centric profiles _(Complete)_ — ADR-067 runtime ingestion
- [x] NX-453 Automation integration for axle-centric limits _(Complete)_ — ADR-067 planner/controller wiring
- [x] NX-454 Calibration workflows & fixtures _(Complete)_ — ADR-067 calibration suite delivery
- [x] NX-455 Documentation & preset libraries for axle-centric rigs _(Complete)_ — ADR-067 rollout playbook
- [x] NX-462 Linux Core service packaging & systemd units _(Done)_ — SRS option O-BACKEND-6
- [x] NX-463 Linux Core operations & observability playbook _(Done)_ — ADR-068 diagnostics + SRS §10 Telemetry
- [x] NX-464 Headless Core + AGiO integration validation _(Done)_ — ADR-068 replay + O-BACKEND-6 smoke
- [x] NX-1209 Tools & tests stream compatibility fix _(Done)_ — Align plugin compliance stream handling with updated API and repair cross-track/zone test scaffolding
- [ ] NX-1210 Cross-track harness and yield regression test compilation fix — Resolve nested harness accessibility and missing provenance imports
- [x] NX-1211 Plugin compliance CLI stream guard _(Done)_ — Handle stdout output without assuming FileStream and repair manifest test template serialization
- [ ] NX-1213 Linux/Core test compilation fixes — Repair test infrastructure regressions (GpsdClientTests scopes, PlanarPoint type usage, TemporaryDirectory helpers, ValueTask delegates)

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
- [x] NX-118 gRPC ⇄ AOG-Link translator _(Done)_
- [x] NX-119 AOG-Link ⇄ PGN compatibility bridge _(Done)_
- [x] NX-120 AOG-Link Ethernet/UDP driver _(Done)_
- [ ] NX-1015 NTRIP client logging cleanup for implicit usings — Remove redundant using directives flagged by analyzers
- [x] NX-121 AOG-Link RS-485/serial driver _(Done)_
- [x] NX-122 AOG-Link CAN/CAN-FD driver _(Done)_

- [x] NX-236 AgIO ELRS adapter for RadioBridge _(Done)_ — ADR-048 radio bridge integration
- [x] NX-237 AgIO LoRa adapter for RadioBridge _(Done)_ — ADR-048 radio bridge integration
- [x] NX-238 RadioBridge provisioning & key management CLI _(Done)_ — ADR-048 provisioning workflow
- [x] NX-239 Radio diagnostics feed into mesh telemetry _(Done)_ — ADR-048 diagnostics + ADR-047 mesh
- [x] NX-240 Mesh bridge to AOG-Link gateways _(Done)_ — ADR-047 mesh integration with legacy
- [x] NX-241 RadioBridge firmware stubs & simulators _(Done)_ — ADR-048 firmware integration
- [x] NX-242 RadioBridge conformance & retry/FEC tests _(Done)_ — ADR-048 reliability validation
- [x] NX-243 Mesh integration with AgIO telemetry aggregator _(Done)_ — ADR-047 mesh presence trails
- [x] NX-244 RadioBridge provisioning documentation kit _(Done)_ — ADR-048 provisioning docs
- [x] NX-245 Mesh-aware legacy UDP gateway updates _(Done)_ — ADR-047 presence integration
- [x] NX-246 GNSS correction services bootstrap _(Done)_ — ADR-066 GNSS correction services
- [x] NX-671 gpsd disable toggle for Linux backend _(Done)_ — Allows operators to opt out of gpsd monitoring when the daemon is not present
- [x] NX-701 SocketCAN timeout handling fix _(Done)_ — Avoid double delay after read timeouts
- [x] NX-673 Linux backend multi-stack wiring _(Done)_ — Registers serial, gpsd, and SocketCAN services together with docs/tests
- [x] NX-956 SocketCAN reconnect delay clamp _(Done)_ — Guard zero/negative delays by restoring the default backoff
- [x] NX-935 Legacy pose codec default source fallback _(Done)_ — Default main antenna source address when metadata is omitted
- [x] NX-963 Legacy pose dual heading sanitization fix _(Done)_ — Guard secondary heading against non-finite values
- [ ] NX-986 Linux serial symlink deduplication fix
- [ ] NX-938 Legacy steer command speed encoding fix
- [ ] NX-915 gpsd TPV null field handling regression test
- [ ] NX-934 Legacy UDP section mask snapshot fix
- [x] NX-921 NMEA parser timestamp bounds check _(Done)_ — Guard invalid hh/mm/ss values before constructing TimeOnly
- [ ] NX-932 gpsd monitor restart on socket availability
- [ ] NX-952 Aog.Agio.Windows targeting pack restore guard
- [x] NX-953 SocketCAN burst tolerance for transient backpressure
- [x] NX-967 Linux serial enumerator canonical dedupe
- [x] NX-997 Linux NMEA stream switch logging
- [x] NX-1016 RadioBridge factory analyzer cleanup _(Done)_ — Remove redundant using directives flagged by IDE0005
- [ ] NX-971 Linux NMEA VTG null speed log fix
- [ ] NX-988 Parquet.Net v5 compatibility fixes
- [ ] NX-989 SocketCAN pump cancellation regression fix
- [x] NX-990 TimeProvider registration for NMEA auto scanner
- [x] NX-1003 AGiO host configuration namespace cleanup _(Done)_ — Remove redundant configuration using to silence IDE0005
- [x] NX-1015 AogLinkBridge protobuf helper import cleanup — Remove redundant Google.Protobuf using directive from bridge serializer
- [ ] NX-1136 Core XML documentation warning cleanup — Resolve CS1572/CS1573 parameter comment mismatches in layer runtime and job snapshots

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
- [x] NX-114 Variable-rate controller plugin _(Done)_
- [x] NX-401 Simulation catalog atomic provider registration _(Done)_
- [x] NX-160 Official AutoSteer plugin GA _(Done)_
- [x] NX-161 Official Mapping plugin GA _(Done)_
- [x] NX-162 Official Sections plugin GA _(Done)_
- [x] NX-163 Official Rate Control plugin GA _(Done)_
- [x] NX-164 Official Variable Mapping plugin GA _(Done)_
- [x] NX-165 Official ISOBUS Bridge plugin GA _(Done)_
- [x] NX-166 Official GNSS/IMU Fusion plugin GA _(Done)_
- [x] NX-167 Official NTRIP Client plugin GA _(Done)_
- [x] NX-168 Official Device Manager plugin GA _(Done)_
- [x] NX-169 Official Planter Monitor plugin GA _(Done)_
- [x] NX-170 Official Job Tasks plugin GA _(Done)_
- [x] NX-171 Official Telemetry Logging plugin GA _(Done)_
- [x] NX-172 Official File IO plugin GA _(Done)_
- [x] NX-1110 Telemetry log manifest cleanup _(Done)_ — Remove redundant usings flagged by analyzers
- [ ] NX-1114 Capability registry XML documentation cleanup — Add XML docs for capability registry and weather snapshot models
- [x] NX-1115 Section mask XML documentation cleanup — Align SectionObservation doc comments with struct properties
- [ ] NX-910 Job manifest cross-drive fallback fix
- [x] NX-1017 Job Tasks orchestrator analyzer cleanup — Remove redundant Channel using directive
- [x] NX-1018 Core nullability warning cleanup — Season aggregator, capability registry, job orchestrator, report builder adjustments
- [x] NX-1020 Aog.Plugins nullable warning cleanup — Resolve CS86xx warnings in lease manager, crop ingestion, job orchestrator, genetics pipelines
- [ ] NX-1112 Legacy bridge & Linux adapter dependency alignment — Restore build after upstream API changes

- [x] NX-247 Crop plugin layer ingestion pipeline _(Done)_ — ADR-045 crop type plugin
- [x] NX-248 Crop analytics API surface _(Done)_ — ADR-045 rotation analytics
- [x] NX-249 Crop report sections for report builder _(Done)_ — ADR-045 reporting integration
- [x] NX-250 Crop plugin regression fixtures _(Done)_ — ADR-045 QA hooks
- [x] NX-1015 Crop plugin analyzer cleanup _(Done)_ — Remove stale using directives flagged by IDE0005
- [x] NX-251 Genetics plugin layer ingestion pipeline _(Done)_ — ADR-046 genetics plugin
- [x] NX-252 Genetics barcode & lot tracking integration _(Done)_ — ADR-046 barcode workflows
- [x] NX-253 Genetics export pipelines (CSV/GeoJSON/ISOXML) _(Done)_ — ADR-046 export formats
- [x] NX-254 Genetics analytics callbacks _(Done)_ — ADR-046 analytics integration
- [x] NX-255 Genetics plugin regression fixtures _(Done)_ — ADR-046 QA hooks
- [x] NX-256 Yield sensor normalization module _(Done)_ — ADR-049 yield plugin
- [x] NX-257 Yield smoothing & binning pipeline _(Done)_ — ADR-049 analytics pipelines
- [x] NX-258 Yield import wizard plumbing _(Done)_ — ADR-049 import workflows
- [x] NX-259 Yield analytics API surface _(Done)_ — ADR-049 analytics integration
- [x] NX-260 Yield plugin regression fixtures _(Done)_ — ADR-049 QA hooks
- [x] NX-261 Cost/profit plugin ingestion & ledger _(Done)_ — ADR-050 cost/profit plugin
- [x] NX-262 Cost entry orchestration service _(Done)_ — ADR-050 cost capture flows
- [x] NX-263 Profit analytics rollups _(Done)_ — ADR-050 analytics integration
- [x] NX-264 Profit export pipelines _(Done)_ — ADR-050 export formats
- [x] NX-265 Profit plugin regression fixtures _(Done)_ — ADR-050 QA hooks
- [x] NX-266 Field health plugin ingestion pipeline _(Done)_ — ADR-052 field health plugin
- [x] NX-267 Field health analytics callbacks _(Done)_ — ADR-052 analytics integration
- [x] NX-268 Field health history + toggle persistence _(Done)_ — ADR-052 historical toggles
- [x] NX-269 Field health report sections _(Done)_ — ADR-052 reporting integration
- [x] NX-270 Field health plugin regression fixtures _(Done)_ — ADR-052 QA hooks
- [x] NX-271 Weather ingest pipeline _(Done)_ — ADR-053 weather plugin
- [x] NX-272 Weather sensor adapter integrations _(Done)_ — ADR-053 sensor integrations
- [x] NX-273 Weather overlay data feed _(Done)_ — ADR-053 visualization pipeline
- [x] NX-274 Weather report sections _(Done)_ — ADR-053 reporting integration
- [x] NX-275 Weather plugin regression fixtures _(Done)_ — ADR-053 QA hooks
- [x] NX-276 Autosteer plugin constraint gating updates _(Done)_ — ADR-027 gating + ADR-033 guidance planner
- [x] NX-277 Sections plugin constraint gating updates _(Done)_ — ADR-027 gating
- [x] NX-278 Guidance lane publishing contracts _(Done)_ — ADR-033 guidance planner
- [x] NX-279 Turn planner integration in guidance plugin _(Done)_ — ADR-033 guidance planner
- [x] NX-280 Guidance plugin regression suite _(Done)_ — ADR-033 QA coverage
- [ ] NX-998 Guidance Orchestrator plugin SRS/ADR scaffolding — ADR-069 Guidance Orchestrator
- [x] NX-281 Mapping plugin zone overlay updates _(Done)_ — ADR-027 zones + ADR-029 mapping kernel
- [x] NX-282 Variable rate plugin zone gating _(Done)_ — ADR-027 gating semantics
- [x] NX-283 Device Manager plugin capability surfacing _(Done)_ — ADR-031 compatibility dashboard
- [x] NX-284 Plugin manifest compliance CI gate _(Done)_ — ADR-031 manifest governance
- [x] NX-925 Plugin manifest requiredTransports validation _(Done)_
- [x] NX-285 Telemetry logging plugin season/session updates _(Done)_ — ADR-040/041 lifecycle data
- [x] NX-286 Telemetry logging mesh event capture _(Done)_ — ADR-047 live telemetry mesh
- [x] NX-287 Telemetry export updates for new layers _(Done)_ — ADR-051 report builder + new layers
- [x] NX-1320 Mapping plugin rendering stack duplication — Copy map engine components into plugin project and expose initial PoseStream wiring
- [x] NX-288 Job Tasks plugin season/session orchestration _(Done)_ — ADR-030/040 lifecycle
- [x] NX-289 Job Tasks plugin preset/task orchestration _(Done)_ — ADR-032 presets & layouts
- [x] NX-290 Job Tasks plugin regression fixtures _(Done)_ — ADR-032 orchestration QA

### Section E — UI (Avalonia) + Sim Bar
- [ ] NX-942 Planter panel stale row cleanup regression
- [x] NX-954 Simulation route mode case-insensitive selection _(Done)_ — Mirror SelectedMode setter canonicalization and tests
- [x] NX-980 Sections panel sixteen-toggle support _(Done)_ — Expand UI and tests for 16-section masks
- [x] NX-1120 Avalonia App.axaml include cleanup _(Done)_ — Exclude build output directories from AvaloniaResource glob
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
- [x] NX-112 Layer-aware section map visualization _(Done)_
- [x] NX-130 Presets & layout linking ADR _(Done)_
- [ ] NX-948 MainWindow deterministic dashboard seeding
- [ ] NX-995 Simulation bar design-time view model compile fix — Unseal view-model for design-time designer usage
- [ ] NX-990 Shell menu tooltip timezone clarification
- [ ] NX-1116 Legacy shell layout parity pass — Mirror V6 header and side strips in Avalonia shell

- [x] NX-291 Zone editor toolbar integration _(Done)_ — ADR-044 zone drawing framework
- [x] NX-292 Zone override toggles & policy UX _(Done)_ — ADR-027 constraint UX
- [x] NX-293 Zone import/export workflows _(Done)_ — ADR-027 interop contracts
- [x] NX-294 Season navigator UI flows _(Done)_ — ADR-040 season organizers
- [x] NX-295 Session start/stop UI refresh _(Done)_ — ADR-041 job sessions UX
- [x] NX-296 Multi-field job selection UX _(Done)_ — ADR-043 multi-field envelopes
- [x] NX-297 Preset switcher with orchestration status _(Done)_ — ADR-032 presets & layout linking
- [x] NX-298 Layout diff viewer & rollback UX _(Done)_ — ADR-032 presets & layout linking
- [x] NX-299 Metadata-driven dashboard refactor _(Done)_ — ADR-034 metadata dashboards
- [x] NX-300 Inspector & legend components _(Done)_ — ADR-034 inspector surfaces
- [x] NX-301 Dashboard automation test harness _(Done)_ — ADR-034 QA automation
- [x] NX-302 Crop quick-select UI _(Done)_ — ADR-045 crop plugin UX
- [x] NX-303 Genetics picker & barcode UI _(Done)_ — ADR-046 genetics UX
- [x] NX-304 Yield overlay UX updates _(Done)_ — ADR-049 yield visualization
- [x] NX-305 Profit heatmap & analytics UI _(Done)_ — ADR-050 profit visualization
- [ ] NX-1006 Simulation playback rate clamp guard
- [ ] NX-987 Simulation bar playback rate label multiplier — Format selected rate using multiplier notation
- [ ] NX-989 Simulation bar playback rate option sorting — Ensure legacy imports insert multiplier-sorted options
- [ ] NX-999 Simulation bar playback rate options read-only guard
- [ ] NX-989 Simulation bar cancellation log level reduction — Treat OperationCanceledException as informational noise
- [ ] NX-996 Replay timeline disposal leak — Ensure view-model detaches timeline/exporter handlers
- [x] NX-306 Field health severity UX _(Done)_ — ADR-052 field health visualization
- [x] NX-307 Weather timeline & overlay UX _(Done)_ — ADR-053 weather visualization
- [x] NX-308 Report builder preview & share UI _(Done)_ — ADR-051 report builder UI
- [x] NX-309 Device Manager compatibility dashboard _(Done)_ — ADR-031 manifest governance UI
- [x] NX-310 Mesh share/subscribe UI _(Done)_ — ADR-047 live mesh UX
- [x] NX-311 Radio provisioning UI flows _(Done)_ — ADR-048 provisioning UX
- [x] NX-312 Companion metadata-driven parity pass _(Done)_ — ADR-034 remote parity
- [x] NX-931 SimulationBar replay subscription cleanup _(Done)_ — Remove legacy EnsureReplayControllerSubscription helper
- [x] NX-410 Legacy UI asset migration workbook _(Done)_ — See docs/ui/ui-shell-and-plugin-integration.md
- [x] NX-411 Shell & navigation port from V6/AgValonia _(Done)_ — Aligns with artifacts/ui-core-spec.md
- [x] NX-412 Map canvas & field operations UI port _(Done)_ — Aligns with artifacts/ui-inventory.json
- [x] NX-413 Job & field lifecycle dialogs port _(Done)_ — Aligns with artifacts/ui-backlog.json
- [x] NX-419 Settings, hotkeys, and appearance consolidation _(Done)_ — Aligns with artifacts/ui-theme-tokens.json
- [x] NX-415 Plugin UI surfaces (guidance, device, analytics, video) _(Done)_ — Aligns with artifacts/ui-to-plugin.yaml
- [x] NX-416 Diagnostics & AgIO workspace port _(Done)_ — Aligns with artifacts/ui-core-spec.md
- [x] NX-417 Simulation shell + companion parity automation _(Done)_ — Aligns with docs/ui/metadata-driven-ui-style-guide.md
- [x] NX-418 Documentation, QA, and release readiness _(Done)_ — Aligns with artifacts/ui-screenshots/README.md
- [x] NX-923 Simulation bar replay error logging _(Done)_ — Ensure playback commands surface controller failures
- [x] NX-941 Simulation bar playback rate reset _(Done)_ — Reset configuration default when reverting routes
- [x] NX-988 Simulation bar cancellation logging suppression _(Done)_ — Ignore replay controller cancellations in transport bar
- [x] NX-990 Simulation bar playback dispatcher marshaling _(Done)_ — Marshal playback rate option mutations through UI dispatcher
- [x] NX-989 Simulation bar design-time data parity _(Done)_ — Align design-time view model with runtime dependencies for the Avalonia designer
- [x] NX-989 Simulation bar auto-resume guard _(Done)_ — Skip toggling when no replay session is active
- [x] NX-991 Simulation bar cancellation logging suppression _(Done)_ — Ignore replay controller cancellations in transport bar

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
- [x] NX-106 Legacy field overview metadata import _(Done)_
- [x] NX-107 Legacy flag importer & UI surfacing _(Done)_
- [x] NX-108 Legacy contour resume support _(Done)_
- [x] NX-109 Legacy recorded path import & replay _(Done)_
- [x] NX-110 Legacy tram line template import _(Done)_
- [x] NX-111 Legacy worked area history import _(Done)_
- [x] NX-113 External agronomic map ingest pipeline _(Done)_


- [x] NX-313 Stanley controller parity harness _(Done)_ — ADR-033 guidance planner porting
- [x] NX-314 Pure pursuit control port with fixtures _(Done)_ — ADR-033 guidance planner porting
- [x] NX-315 Turn planner library port _(Done)_ — ADR-033 guidance planner porting
- [x] NX-316 Constraint-aware lookahead tuning _(Done)_ — ADR-033 lookahead + ADR-027 gating
- [x] NX-317 Firmware-in-loop stability validation _(Done)_ — ADR-033 closed-loop validation
- [x] NX-620 V6 guidance row/edge-case study docs _(Done)_ — v6 guidance extraction notes (sections 90/95/99)

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
- [x] NX-123 README experiment narrative refresh _(Done)_
- [x] NX-124 Spatial constraint zones ADR & SRS sync _(Done)_
- [x] NX-134 Official plugin dependency map & manifests _(Done)_

- [x] NX-318 Plugin manifest governance documentation _(Done)_ — ADR-031 manifest governance
- [x] NX-319 Zone policy operator guide _(Done)_ — ADR-027 constraint UX docs
- [x] NX-320 Season/session migration playbook _(Done)_ — ADR-040/041 lifecycle rollout
- [x] NX-321 Mesh provisioning runbook _(Done)_ — ADR-047/048 connectivity rollout
- [x] NX-322 Report template catalog documentation _(Done)_ — ADR-051 report builder
- [x] NX-323 Performance budget telemetry dashboards _(Done)_ — ADR-026 instrumentation rollout
- [x] NX-324 Mesh retention & privacy operations guide _(Done)_ — ADR-047 retention planner
- [x] NX-325 Weather compliance export documentation _(Done)_ — ADR-053 compliance outputs
- [x] NX-326 Metadata-driven UI style guide _(Done)_ — ADR-034 UI refactor
- [x] NX-327 Plugin QA handshake update _(Done)_ — ADR-031 manifest governance QA
- [x] NX-514 Nexus CLI SRS & ADR alignment _(Done)_ — SRS §18 Command Line Interface
- [x] NX-341 GitHub Actions release packaging (Win/Linux zips) _(Done)_ — SRS §2.7 Packaging & DevEx
- [x] NX-342 Developer setup quick start _(Done)_ — SRS §2.8 Documentation
- [x] NX-922 NuGet local feed path normalization _(Done)_ — Ensure artifacts/nuget source resolves cross-platform
- [ ] NX-512 README Nexus guide expansion _(In Progress)_ — docs/README.md narrative refresh
- [x] NX-960 .NET 8 toolchain regression sweep _(Done)_ — Restore CLI, protobuf, and Parquet compatibility after SDK updates
- [x] NX-600 NX CLI Plugin backlog update _(Done)_ — Seed implementation tasks for unified `nx` host
- [x] NX-601 NX CLI Plugin host scaffold _(Done)_ — SRS §18 CLI host with System.CommandLine + Spectre.Console
- [x] NX-602 NX CLI Plugin endpoint resolver & core status _(Done)_ — SRS §18 transport negotiation + Core health probe
- [x] NX-603 NX CLI Plugin output modes & completions _(Done)_ — SRS §18 structured output + shell completion
- [x] NX-604 NX CLI Plugin discovery & adapter loader _(Done)_ — SRS §18 plugin adapters and manifest scanning
- [x] NX-605 Publish Nexus.Plugin.Cli.Abstractions _(Done)_ — SRS §18 plugin SDK packaging
- [x] NX-606 NX CLI Plugin sample verbs _(Done)_ — SRS §18 sample plugin exposing calibrate/sniff commands
- [x] NX-607 NX CLI Plugin gRPC reflection client _(Done)_ — SRS §18 dynamic verb reflection support
- [x] NX-608 NX CLI Plugin packaging pipeline _(Done)_ — SRS §18 dotnet tool + RID single-file builds
- [x] NX-609 NX CLI Plugin docs & completions kit _(Done)_ — SRS §18 CLI docs and shell integration guidance

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

- [x] NX-328 Constraint fault-injection regression suite _(Done)_ — ADR-027 gating QA
- [x] NX-329 Mesh security and penetration tests _(Done)_ — ADR-047/048 security validation
- [x] NX-330 Session crash-recovery regression _(Done)_ — ADR-041 session durability
- [x] NX-331 Report export audit & diff tests _(Done)_ — ADR-051 report builder QA
- [x] NX-332 Autosteer closed-loop bench tests _(Done)_ — ADR-033 guidance QA

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
- [x] NX-955 Field feedback aggregator JSONL regression fix _(Done)_ — Restore newline-delimited parsing and unit coverage
- [x] NX-095 Dealer support escalation process
- [x] NX-096 Community preview program
- [x] NX-097 1.0 launch readiness review
- [ ] NX-943 Legacy steer command guidance status disable fix
- [ ] NX-940 Legacy steer angle saturation clamp

- [x] NX-333 Legacy zone importer & converter _(Done)_ — ADR-027 zone interop
- [x] NX-334 Legacy job migration tooling _(Done)_ — ADR-040/041 season/session migration
- [x] NX-335 Legacy multi-field envelope translator _(Done)_ — ADR-043 multi-field interop
- [x] NX-336 Legacy telemetry remap to new layers _(Done)_ — ADR-049/052 telemetry parity
- [x] NX-337 Legacy weather log migration utilities _(Done)_ — ADR-053 weather parity

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
| NX-150 | ADR roadmap: PoseStream, layers, and control revamp | Done |  | — | [ADR roadmap](docs/ADR/ADR-roadmap.md) | Track upcoming ADR-007…ADR-020 deliverables and linked SRS requirements |
| NX-115 | AOG-Link protocol specification and reference flows | Done |  | — | [SRS §3 Communications & Transports](docs/SRS/sections/03_Comm_Transports.md) | ADR-006 + SRS updates complete |
| NX-116 | Shared `aog-link.proto` schemas with nanopb options | Done |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Publish contracts aligned with `Aog.Abstractions` |
| NX-152 | Enforce ADR-001 .NET 8 runtime baselines across solutions and CI | Done | AI | — | [ADR-001 .NET 8 runtime](docs/ADR/ADR-001-dotnet8-runtime.md) | Runtime baseline playbook + SDK pinning in `global.json` |
| NX-190 | Comprehensive ADR portfolio review | Done |  | — | [ADR roadmap](docs/ADR/ADR-roadmap.md) | Apply 2025 governance updates across accepted and draft ADRs |
| NX-153 | Operationalize ADR-002 gRPC contract governance and compatibility gates | Done | AI | — | [ADR-002 gRPC contracts](docs/ADR/ADR-002-grpc-contracts.md) | Checklist for clinics, golden fixtures, and release gating |
| NX-154 | Deliver ADR-003 Avalonia shell run modes (CompanionRemote/Local) | Done | AI | — | [ADR-003 Avalonia UI](docs/ADR/ADR-003-avalonia-ui.md) | Run-mode configuration + smoke test guide published |
| NX-155 | Complete ADR-004 composite simulation fabric with regression packs | Done | AI | — | [ADR-004 Composite simulation](docs/ADR/ADR-004-composite-simulation.md) | GA validation checklist covering topics, seeds, replay |
| NX-156 | Roll out ADR-006 AOG-Link transports and bridge translation layers | Done | AI | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Transport rollout guide + validation/support checklist |
| NX-157 | Implement ADR-018 plugin API leases, manifests, and AgIO migration | Done | AI | — | [ADR-018 Plugin API](docs/ADR/ADR-018-plugin-api.md) | Lease + manifest governance guide for plugin authors |
| NX-191 | Zone proto + JSON schema handshake | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 spatial constraints contract release |
| NX-192 | PoseStream zone mask proto update | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | PoseStream now carries `PoseZoneMask` with registry hash + zone IDs |
| NX-193 | Layer registry hash handshake draft | Done |  | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md) | Draft handshake spec published for controller boot validation |
| NX-194 | Capability registry expansion for mapping/zone capabilities | Done |  | — | [ADR-029](docs/ADR/ADR-029-mapping-plugin-architecture.md), [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-029 mapping kernel contracts; ADR-031 manifest governance |
| NX-195 | Plugin manifest schema vNext with capability/lease metadata | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 official plugin bundle policy |
| NX-196 | Job/session schema refresh | Done |  | — | [ADR-030](docs/ADR/ADR-030-field-job-sessions.md), [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-030 job lifecycle; ADR-041 session metadata |
| NX-197 | Season organizer schema publication | Done |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040 season organizers data model |
| NX-198 | Multi-field job envelope schema updates | Done |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field job envelopes |
| NX-199 | Layer edit event schema definition | Done |  | — | [ADR-044](docs/ADR/ADR-044_ZoneDrawingFramework.md) | ADR-044 zone drawing framework journal |
| NX-200 | Crop layer definitions and registries | Done |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 crop type plugin requirements |
| NX-201 | Genetics layer definitions and registries | Done |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 genetics plugin contracts |
| NX-202 | Yield layer schema refresh | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 yield analytics plugin |
| NX-203 | Cost/profit layer schema | Done |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 cost & profit plugin |
| NX-204 | Field health risk schema | Done |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 field health plugin |
| NX-205 | Weather snapshot schema extensions | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather & environment plugin |
| NX-206 | Report template schema + manifest handshake | Done |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder & exports |
| NX-207 | SRS + ADR cross-reference sweep for new layers | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md), [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-027…ADR-053 portfolio alignment |
| NX-208 | Official bundle capability matrix update | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance |
| NX-209 | CRS normalization matrix publication | Done |  | — | [ADR-022](docs/ADR/ADR-022-crs-units-precision-policy.md) | Published [reference matrix](docs/reference/crs-normalization-matrix.md) |
| NX-210 | Contracts freeze automation for new capabilities | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 governance rollout |

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
| NX-211 | ZoneStore persistence service | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 ZoneService storage and indexing |
| NX-212 | ZoneService gRPC host & watcher plumbing | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 ZoneService implementation |
| NX-213 | PoseStream zone mask propagation | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 constraint mask propagation |
| NX-214 | Constraint gate integration into ControlArbiter | Done | AI | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | Constraint gate enforces keep-out/work-disabled policies |
| NX-215 | Layer controller runtime scaffolding | Done |  | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md) | ADR-068 layer controllers |
| NX-216 | Layer controller DI registry & buffer pools | Done |  | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md) | ADR-068 controller runtime details |
| NX-217 | PoseStream ingestion wiring for controllers | Done |  | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md) | ADR-068 ingestion pipeline |
| NX-218 | Controller quality & diagnostic feeds | Done |  | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md) | ADR-068 diagnostics surfacing |
| NX-219 | TileStore writer updates for controller outputs | Done |  | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md) | ADR-068 TileStore integration |
| NX-220 | Deterministic replay fixtures for controllers | Done |  | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md) | ADR-068 replay harness |
| NX-221 | JobsService host & lifecycle orchestration | Done |  | — | [ADR-030](docs/ADR/ADR-030-field-job-sessions.md) | ADR-030 job sessions service |
| NX-222 | Session autosave & journaling pipeline | Done |  | — | [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-041 job sessions |
| NX-223 | Season aggregator & sync orchestration | Done |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040 season organizers |
| NX-224 | Multi-field envelope aggregation pipeline | Done |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field jobs |
| NX-225 | LayerEditEvent journal service | Done |  | — | [ADR-044](docs/ADR/ADR-044_ZoneDrawingFramework.md) | ADR-044 zone drawing framework |
| NX-226 | Live telemetry mesh core service | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 live telemetry mesh |
| NX-227 | Mesh diagnostics & ACL enforcement | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh QoS/security |
| NX-228 | RadioBridge transport stack in Core | Done | 2025-03-20 | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 radio bridge |
| NX-229 | Mesh retention & offline sync workers | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh retention requirements |
| NX-230 | Report builder service backend | Done |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder |
| NX-231 | Performance budget instrumentation | Done |  | — | [ADR-026](docs/ADR/ADR-026-performance-budgets.md) | ADR-026 performance budgets |
| NX-232 | Timebase drift monitors for sessions | Done |  | — | [ADR-021](docs/ADR/ADR-021-timebase-clock-sync.md) | ADR-021 timebase sync |
| NX-233 | Discovery watcher updates for seasons | Done |  | — | [ADR-024](docs/ADR/ADR-024-discovery-identity.md) | ADR-024 discovery & identity |
| NX-234 | Provenance audit expansion for new layers | Done |  | — | [ADR-019](docs/ADR/ADR-019-provenance-audit-qa.md) | ADR-019 provenance audit |
| NX-235 | Cross-track replay harness slice | Done |  | — | [ADR roadmap](docs/ADR/ADR-roadmap.md) | ADR-roadmap cross-track slice plan |
| NX-341 | GitHub Actions release packaging (Win/Linux zips) | Done | AI | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Produce zipped release artifacts for Windows and Linux builds |
| NX-414 | Multi-steer configurator export schema GA | Done | Core Owner | — | [ADR-067](docs/ADR/ADR-067-equipment-configuration-kinematics.md) | Finalize JSON schema, validators, and hash tooling |
| NX-452 | Core ingestion service for axle-centric profiles | Done | Core Owner | — | [ADR-067](docs/ADR/ADR-067-equipment-configuration-kinematics.md) | Implement deterministic loader + health telemetry |
| NX-453 | Automation integration for axle-centric limits | Done | Core Owner | — | [ADR-067](docs/ADR/ADR-067-equipment-configuration-kinematics.md) | Wire planners/controllers to curvature/slip limits |
| NX-454 | Calibration workflows & fixtures | Done | Core Owner | — | [ADR-067](docs/ADR/ADR-067-equipment-configuration-kinematics.md) | Deliver Ackermann wizard, hitch zeroing, slip sanity tests |
| NX-455 | Documentation & preset libraries for axle-centric rigs | Done | Core Owner | — | [ADR-067](docs/ADR/ADR-067-equipment-configuration-kinematics.md) | Publish operator guides + preset bundles |
| NX-462 | Linux Core service packaging & systemd units | Done | Core Owner | — | [O-BACKEND-6](docs/SRS/options/O-BACKEND-6_LinuxCoreService.md) | Create deb/rpm installers, systemd units, and upgrade path |
| NX-463 | Linux Core operations & observability playbook | Done | Core Owner | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md), [SRS §10](docs/SRS/sections/10_Telemetry_Health.md) | Document logging, metrics, alerting, and recovery drills |
| NX-464 | Headless Core + AGiO integration validation | Done | Core Owner | — | [ADR-068](docs/ADR/ADR-068-layer-controllers-runtime.md), [O-BACKEND-6](docs/SRS/options/O-BACKEND-6_LinuxCoreService.md) | End-to-end smoke with AGiO backends on Linux headless |

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
| NX-959 | Linux NMEA scan hot reload | In Progress |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Cancel and restart serial scan loop when configuration changes |
| NX-027 | Legacy UDP gateway skeleton | Done |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Loopback test |
| NX-029 | Agio.Linux SocketCAN backend (CAN→gRPC) | Done |  | — | [SRS Option 11-O1](docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md) | Streams CAN frames + section relays |
| NX-066 | GNSS source policy + TCP/UDP provider | Done |  | — | [SRS Option 11-O1](docs/SRS/sections/1X_Platform_Foundations/11-O1_Unified_DotNet8_Avalonia.md) | Aggregates `IPositionSource` feeds |
| NX-117 | Bridge service host for gRPC ⇄ AOG-Link | Done |  | — | [ADR-002 gRPC Contracts](docs/ADR/ADR-002-grpc-contracts.md) | Standalone daemon mediating inter-process, AOG-Link, and PGN flows |
| NX-118 | gRPC ⇄ AOG-Link translator layer | Done |  | — | [ADR-002 gRPC Contracts](docs/ADR/ADR-002-grpc-contracts.md) | Map service calls/streams onto nanopb datagrams with ack/retry semantics |
| NX-119 | AOG-Link ⇄ PGN compatibility bridge | Done |  | — | [SRS Option O-COMM-6](docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md) | Maintain legacy devices during migration |
| NX-120 | AOG-Link Ethernet/UDP driver | Done |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Implement multicast/unicast transport with command retries |
| NX-121 | AOG-Link RS-485/serial driver | Done |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | COBS framing + CRC-16 with token/slot scheduling |
| NX-122 | AOG-Link CAN/CAN-FD driver | Done |  | — | [ADR-006 AOG-Link](docs/ADR/ADR-006-aog-link-mcu-communications.md) | Implement AOG-CAN ID layout + ISO-TP / fragment support |
| NX-236 | AgIO ELRS adapter for RadioBridge | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 radio bridge integration |
| NX-237 | AgIO LoRa adapter for RadioBridge | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 radio bridge integration |
| NX-238 | RadioBridge provisioning & key management CLI | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 provisioning workflow |
| NX-239 | Radio diagnostics feed into mesh telemetry | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md), [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-048 diagnostics + ADR-047 mesh |
| NX-240 | Mesh bridge to AOG-Link gateways | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh integration with legacy |
| NX-241 | RadioBridge firmware stubs & simulators | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 firmware integration |
| NX-242 | RadioBridge conformance & retry/FEC tests | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 reliability validation |
| NX-243 | Mesh integration with AgIO telemetry aggregator | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 mesh presence trails |
| NX-244 | RadioBridge provisioning documentation kit | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 provisioning docs |
| NX-245 | Mesh-aware legacy UDP gateway updates | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 presence integration |
| NX-246 | GNSS correction services bootstrap | Done |  | — | [ADR-066](docs/ADR/ADR-roadmap.md) | ADR-066 GNSS correction services |
| NX-742 | SocketCAN channel backpressure guardrails | Done | AI | — | — | Drop stalled subscribers and stress fast consumers |
| NX-933 | Linux serial scan configuration binding | Done | AI | — | — | Bind `AgioHost:Linux:Serial:Scan` options, document overrides, add tests |
| NX-986 | Linux serial symlink deduplication fix | In Progress | AI | — | — | Collapse duplicate device nodes discovered via symlinks |
| NX-952 | SocketCAN subscriber eviction reset | In Progress | AI | — | — | Ensure eviction clears backpressure and re-subscription regression test |
| NX-967 | Linux serial enumerator canonical dedupe | Done | AI | — | — | Canonicalize enumerated device paths and dedupe results |

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
| NX-114 | Variable-rate controller plugin consuming layer APIs | Done |  | — | [SRS §3.7 Sections Control](docs/SRS/NOTES.md#srs-37-sections-control) | Layer-driven SectionPlacement rate controller |
| NX-160 | Official AutoSteer plugin GA (pose fusion + actuator transport) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: mapping, GNSS/IMU fusion, AgIO; Soft: sections, device manager; transports per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-161 | Official Mapping plugin GA (field state store + AB/coverage feeds) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core store; Soft: file-io, job tasks, telemetry logging per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-162 | Official Sections plugin GA (coverage gating + IO orchestration) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: mapping + AgIO valves; Soft: rate-control, variable-mapping, telemetry per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-163 | Official Rate Control plugin GA (setpoint + actuator coordination) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: variable-mapping, AgIO; Soft: ISOBUS bridge, mapping telemetry per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-164 | Official Variable Mapping plugin GA (grid ingestion + setpoints) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: mapping layers, file-io; Soft: job-tasks, telemetry logging per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-165 | Official ISOBUS Bridge plugin GA (TC/UT translation) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core contracts + AgIO interface; Soft: rate-control diagnostics per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-166 | Official GNSS/IMU Fusion plugin GA (pose publisher) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: ntrip-client, AgIO sensors; Soft: telemetry logging, mapping overlays per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-167 | Official NTRIP Client plugin GA (RTCM streaming) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: AgIO network transport; Soft: telemetry logging, device manager per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-168 | Official Device Manager plugin GA (inventory + health) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: AgIO hardware inventory; Soft: UI shell telemetry badges per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-169 | Official Planter Monitor plugin GA (row sensing + analytics) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: AgIO row sensors + Core session store; Soft: mapping overlays, telemetry logging per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-170 | Official Job Tasks plugin GA (save/resume lifecycle) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core job services; Soft: file-io, mapping, telemetry per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-171 | Official Telemetry Logging plugin GA (replay + export) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core telemetry bus; Soft: plugin feeds (mapping, autosteer, device manager) per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-172 | Official File IO plugin GA (import/export surfaces) | Done |  | — | [ADR-031 Official plugin bundle](docs/ADR/ADR-031-official-plugin-bundle.md) | Hard deps: Core storage APIs; Soft: mapping, variable-mapping, job-tasks per [dependency map](docs/plugins/nexus-plugin-dependency-map.md) |
| NX-247 | Crop plugin layer ingestion pipeline | Done |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 crop type plugin |
| NX-248 | Crop analytics API surface | Done |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 rotation analytics |
| NX-249 | Crop report sections for report builder | Done |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 reporting integration |
| NX-250 | Crop plugin regression fixtures | Done |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 QA hooks |
| NX-251 | Genetics plugin layer ingestion pipeline | Done |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 genetics plugin |
| NX-252 | Genetics barcode & lot tracking integration | Done |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 barcode workflows |
| NX-253 | Genetics export pipelines (CSV/GeoJSON/ISOXML) | Done |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 export formats |
| NX-254 | Genetics analytics callbacks | Done |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 analytics integration |
| NX-255 | Genetics plugin regression fixtures | Done |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 QA hooks |
| NX-256 | Yield sensor normalization module | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 yield plugin |
| NX-257 | Yield smoothing & binning pipeline | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 analytics pipelines |
| NX-258 | Yield import wizard plumbing | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 import workflows |
| NX-259 | Yield analytics API surface | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 analytics integration |
| NX-260 | Yield plugin regression fixtures | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 QA hooks |
| NX-261 | Cost/profit plugin ingestion & ledger | Done |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 cost/profit plugin |
| NX-262 | Cost entry orchestration service | Done |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 cost capture flows |
| NX-263 | Profit analytics rollups | Done |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 analytics integration |
| NX-264 | Profit export pipelines | Done |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 export formats |
| NX-265 | Profit plugin regression fixtures | Done |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 QA hooks |
| NX-266 | Field health plugin ingestion pipeline | Done |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 field health plugin |
| NX-267 | Field health analytics callbacks | Done |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 analytics integration |
| NX-268 | Field health history + toggle persistence | Done |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 historical toggles |
| NX-269 | Field health report sections | Done |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 reporting integration |
| NX-270 | Field health plugin regression fixtures | Done |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 QA hooks |
| NX-271 | Weather ingest pipeline | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather plugin |
| NX-272 | Weather sensor adapter integrations | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 sensor integrations |
| NX-273 | Weather overlay data feed | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 visualization pipeline |
| NX-274 | Weather report sections | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 reporting integration |
| NX-275 | Weather plugin regression fixtures | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 QA hooks |
| NX-1019 | WeatherSensorReading XML documentation cleanup | In Progress | AI | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | Address CS1591 warnings for weather sensor ingest types |
| NX-276 | Autosteer plugin constraint gating updates | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md), [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-027 gating + ADR-033 guidance planner |
| NX-277 | Sections plugin constraint gating updates | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 gating |
| NX-278 | Guidance lane publishing contracts | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner |
| NX-279 | Turn planner integration in guidance plugin | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner |
| NX-280 | Guidance plugin regression suite | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 QA coverage |
| NX-281 | Mapping plugin zone overlay updates | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md), [ADR-029](docs/ADR/ADR-029-mapping-plugin-architecture.md) | ADR-027 zones + ADR-029 mapping kernel |
| NX-282 | Variable rate plugin zone gating | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 gating semantics |
| NX-283 | Device Manager plugin capability surfacing | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 compatibility dashboard |
| NX-284 | Plugin manifest compliance CI gate | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance |
| NX-925 | Plugin manifest requiredTransports validation | Done |  | — | — | Guard against blank/null transport entries |
| NX-285 | Telemetry logging plugin season/session updates | Done |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040/041 lifecycle data |
| NX-286 | Telemetry logging mesh event capture | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 live telemetry mesh |
| NX-287 | Telemetry export updates for new layers | Done |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder + new layers |
| NX-288 | Job Tasks plugin season/session orchestration | Done |  | — | [ADR-030](docs/ADR/ADR-030-field-job-sessions.md) | ADR-030/040 lifecycle |
| NX-289 | Job Tasks plugin preset/task orchestration | Done |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 presets & layouts |
| NX-290 | Job Tasks plugin regression fixtures | Done |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 orchestration QA |

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
| NX-112 | Layer-aware section map visualization aligned with layer registry | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Map view renders registry-backed layers |
| NX-130 | Presets & layout linking ADR | Done |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 accepted in NX-130 |
| NX-291 | Zone editor toolbar integration | Done |  | — | [ADR-044](docs/ADR/ADR-044_ZoneDrawingFramework.md) | ADR-044 zone drawing framework |
| NX-292 | Zone override toggles & policy UX | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 constraint UX |
| NX-293 | Zone import/export workflows | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 interop contracts |
| NX-294 | Season navigator UI flows | Done |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040 season organizers |
| NX-295 | Session start/stop UI refresh | Done |  | — | [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-041 job sessions UX |
| NX-296 | Multi-field job selection UX | Done |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field envelopes |
| NX-297 | Preset switcher with orchestration status | Done |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 presets & layout linking |
| NX-298 | Layout diff viewer & rollback UX | Done |  | — | [ADR-032](docs/ADR/ADR-032-presets-and-layout-linking.md) | ADR-032 presets & layout linking |
| NX-299 | Metadata-driven dashboard refactor | Done |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 metadata dashboards |
| NX-300 | Inspector & legend components | Done |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 inspector surfaces |
| NX-301 | Dashboard automation test harness | Done |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | ADR-034 QA automation |
| NX-302 | Crop quick-select UI | Done |  | — | [ADR-045](docs/ADR/ADR-045_CropTypePlugin.md) | ADR-045 crop plugin UX |
| NX-303 | Genetics picker & barcode UI | Done |  | — | [ADR-046](docs/ADR/ADR-046_GeneticsPlugin.md) | ADR-046 genetics UX |
| NX-304 | Yield overlay UX updates | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049 yield visualization |
| NX-305 | Profit heatmap & analytics UI | Done |  | — | [ADR-050](docs/ADR/ADR-050_CostProfitPlugin.md) | ADR-050 profit visualization |
| NX-306 | Field health severity UX | Done |  | — | [ADR-052](docs/ADR/ADR-052_FieldHealthPlugin.md) | ADR-052 field health visualization |
| NX-307 | Weather timeline & overlay UX | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather visualization |
| NX-308 | Report builder preview & share UI | Done |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder UI |
| NX-309 | Device Manager compatibility dashboard | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance UI |
| NX-310 | Mesh share/subscribe UI | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 live mesh UX |
| NX-311 | Radio provisioning UI flows | Done |  | — | [ADR-048](docs/ADR/ADR-048_RadioBridge.md) | ADR-048 provisioning UX |
| NX-312 | Companion metadata-driven parity pass | Done |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | Snapshot export keeps CompanionRemote dashboards, legends, and inspector metadata aligned. |
| NX-410 | Legacy UI asset migration workbook | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Inventory V6/Dev/AgValonia components per docs/ui/ui-shell-and-plugin-integration.md |
| NX-411 | Shell & navigation port from V6/AgValonia | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Port menus/toolbars per artifacts/ui-core-spec.md |
| NX-412 | Map canvas & field operations UI port | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Integrate map canvas + boundary/flag dialogs per plan |
| NX-413 | Job & field lifecycle dialogs port | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Rebuild job/field dialogs aligned with artifacts/ui-backlog.json |
| NX-419 | Settings, hotkeys, and appearance consolidation | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Port settings dialogs with theme tokens |
| NX-415 | Plugin UI surfaces (sections, autosteer, video) | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Implement plugin injection per artifacts/ui-to-plugin.yaml |
| NX-416 | Diagnostics & AgIO workspace port | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Port diagnostics dialogs aligning with UI core spec |
| NX-417 | Simulation shell + companion parity automation | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Wire simulator dialog + metadata snapshot harness |
| NX-418 | Documentation, QA, and release readiness | Done |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | Update docs/tests per plan |
| NX-1116 | Legacy shell layout parity pass | In Progress |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Mirror V6 header and side strips in Avalonia shell |
| NX-1212 | Block layout contracts & persistence scaffolding | In Progress | AI | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Block catalog, layout seeding, and shell preferences migration |
| NX-946 | Planter panel row index overflow guard | In Progress |  | — | [SRS §3.8 Planter Monitor](docs/SRS/NOTES.md#srs-38-planter-monitor) | Guard invalid planter row indexes in UI summary |

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
| NX-105 | Legacy background imagery import and persistence | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Extend importer with BackPic.txt/.png handling |
| NX-106 | Legacy field overview metadata import | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Field.txt importer populates LegacyFieldOverview |
| NX-107 | Legacy flags and annotations importer + UI exposure | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Flags.txt importer surfaces scouting markers |
| NX-108 | Legacy contour coverage resume support | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Contour.txt resume buffers mapped into Core |
| NX-109 | Legacy recorded path import feeding replay services | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | RecPath.txt translated for replay |
| NX-110 | Legacy tram line template import and planner integration | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Tram.txt templates ingested |
| NX-111 | Legacy worked area history import for coverage bootstraps | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | Sections.txt history converted to layer cells |
| NX-113 | External agronomic map ingest pipeline | Done |  | — | [SRS §5.5 Legacy Data Ingest](docs/SRS/NOTES.md#srs-55-legacy-data-ingest) | CSV importer emits Layer.v1 documents |
| NX-313 | Stanley controller parity harness | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner porting |
| NX-314 | Pure pursuit control port with fixtures | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner porting |
| NX-315 | Turn planner library port | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance planner porting |
| NX-316 | Constraint-aware lookahead tuning | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md), [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-033 lookahead + ADR-027 gating |
| NX-317 | Firmware-in-loop stability validation | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 closed-loop validation |

### Section G — Packaging, DevEx, Docs

| ID | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- |
| NX-006 | CI matrix (Win x64 + Linux arm64) | Done | AI | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Include lint + headless sim |
| NX-007 | SourceCode path normalization | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Canonical "Nexus SourceCode" references |
| NX-008 | Schema validator registry upgrade | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | `referencing`-based loader resolves `$id` links |
| NX-009 | Tooling SourceCode path fixes | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Run scripts default to SourceCode layout |
| NX-060 | Dev scripts (`nexus run core| Done |ui`, `nexus sim`) | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Bash + PowerShell |
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
| NX-124 | Spatial constraint zones ADR & SRS sync | Done | AI | — | [SRS §3 Communications](docs/SRS/sections/03_Comm_Transports.md) | Add ZoneService requirements and constraint policies |
| NX-141 | Companion/mobile stack rollout documentation | Done | AI | — | [SRS §5 Frontends](docs/SRS/sections/05_Frontends.md) | Capture CompanionRemote, LocalInProc, and LocalOutOfProc run modes across docs |
| NX-318 | Plugin manifest governance documentation | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance |
| NX-319 | Zone policy operator guide | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 constraint UX docs |
| NX-320 | Season/session migration playbook | Done |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040/041 lifecycle rollout |
| NX-321 | Mesh provisioning runbook | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047/048 connectivity rollout |
| NX-322 | Report template catalog documentation | Done |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder |
| NX-323 | Performance budget telemetry dashboards | Done |  | — | [ADR-026](docs/ADR/ADR-026-performance-budgets.md) | ADR-026 instrumentation rollout |
| NX-324 | Mesh retention & privacy operations guide | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047 retention planner |
| NX-325 | Weather compliance export documentation | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 compliance outputs |
| NX-326 | Metadata-driven UI style guide | Done |  | — | [ADR-034](docs/ADR/ADR-034-metadata-driven-dashboards.md) | Style tokens cover dashboards, inspectors, and legends across desktop + companion shells. |
| NX-327 | Plugin QA handshake update | Done |  | — | [ADR-031](docs/ADR/ADR-031-official-plugin-bundle.md) | ADR-031 manifest governance QA |
| NX-514 | Nexus CLI SRS & ADR alignment | Done | AI | — | [SRS §18 Command Line Interface](docs/SRS/sections/18_Command_Line_Interface.md) | Capture CLI requirements and ADR-054 design brief |
| NX-341 | GitHub Actions release packaging (Win/Linux zips) | Done | AI | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Produce zipped release artifacts for Windows and Linux builds |
| NX-342 | Developer setup quick start doc | Done | AI | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | docs/howto/developer-setup.md |
| NX-343 | UI modernization AI prompt bundle | Done | AI | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | docs/templates/ui-modernization-ai-prompts.md |
| NX-947 | NuGet dependency alignment for .NET restore | In Progress | AI | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Update pinned versions to published packages |

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
| NX-328 | Constraint fault-injection regression suite | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 gating QA |
| NX-329 | Mesh security and penetration tests | Done |  | — | [ADR-047](docs/ADR/ADR-047_LiveTelemetryMesh.md) | ADR-047/048 security validation |
| NX-330 | Session crash-recovery regression | Done |  | — | [ADR-041](docs/ADR/ADR-041_JobSessions.md) | ADR-041 session durability |
| NX-331 | Report export audit & diff tests | Done |  | — | [ADR-051](docs/ADR/ADR-051_ReportBuilder.md) | ADR-051 report builder QA |
| NX-332 | Autosteer closed-loop bench tests | Done |  | — | [ADR-033](docs/ADR/ADR-033-guidance-planner-autosteer.md) | ADR-033 guidance QA |

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
| NX-936 | Legacy shapefile multi-part boundary import fix | In Progress |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Preserve exterior vertices from multi-part polygons |
| NX-937 | Legacy pose codec non-finite guard | In Progress |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Normalize NaN/∞ pose fields before encoding |
| NX-943 | Legacy steer command guidance status disable fix | Planned |  | — | — | Ensure disabled steer commands encode status zero |
| NX-939 | Legacy pose lat/lon sanitization | In Progress |  | — | — | Clamp invalid coordinates before encoding |
| NX-944 | Legacy steer command source override | In Progress |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Allow overriding UDP source address metadata |
| NX-945 | Legacy frame checksum length guard | In Progress |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Guard short frames before validating checksum |
| NX-940 | Legacy steer angle saturation clamp | In Progress |  | — | — | Limit steer command payload to ±3276 hundredths |
| NX-1113 | Legacy steer state heading metadata persistence regression | In Progress |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Ensure legacy heading metadata survives cloning/serialization |

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
| NX-333 | Legacy zone importer & converter | Done |  | — | [ADR-027](docs/ADR/ADR-027-spatial-constraints.md) | ADR-027 zone interop |
| NX-334 | Legacy job migration tooling | Done |  | — | [ADR-040](docs/ADR/ADR-040_SeasonOrganizers.md) | ADR-040/041 season/session migration |
| NX-335 | Legacy multi-field envelope translator | Done |  | — | [ADR-043](docs/ADR/ADR-043_MultiFieldJobEnvelopes.md) | ADR-043 multi-field interop |
| NX-336 | Legacy telemetry remap to new layers | Done |  | — | [ADR-049](docs/ADR/ADR-049_YieldPlugin.md) | ADR-049/052 telemetry parity |
| NX-337 | Legacy weather log migration utilities | Done |  | — | [ADR-053](docs/ADR/ADR-053_WeatherPlugin.md) | ADR-053 weather parity |


### Section E — Pumpkin Pi Fastpath
- [x] NX-PP-000 Ingest Pumpkin Pi codex brief _(Done)_

#### SRS & ADR
- [x] NX-PP-001 Add §8A CM5 Integrated Controller (AgIO-bypass) to SRS
- [x] NX-PP-002 ADR-00XX documenting SHM fastpath + HAL decision

#### Plugin Scaffold
- [x] NX-PP-003 Create repo structure under `plugins/pumpkin-pi/`
- [x] NX-PP-004 Implement SHM ring (`/dev/shm/aoglink_steer`) + eventfd
- [x] NX-PP-005 HAL backends (GPIO via libgpiod, PWM char dev, SocketCAN)
- [x] NX-PP-006 gRPC handlers: consume `SetSteerTarget`, publish `SteerStatus`
- [x] NX-PP-007 MQTT loopback publishers for status/health

#### Docs & Diagrams
- [x] NX-PP-008 Update plugin catalog & architecture diagrams (Mermaid)
- [x] NX-PP-011 Add CM5 getting-started guidance (systemd, mlockall, priorities)
- [x] NX-PP-012 Document authority token workflows (`aog/v1/ctrl/authority/steer`)

#### Config & Ops
- [x] NX-PP-010 Provide `pumpkin.yaml.example`
- [x] NX-PP-013 Provide `pumpkin-pi.service` systemd unit
- [x] NX-PP-014 Provide minimal AgIO bridge config with external adapters enabled

#### Testing
- [x] NX-PP-015 Bench test NAV → SHM → PWM apply ≤ 2 ms p50
- [x] NX-PP-016 Failure test: setpoint TTL expiry drives safe neutral
- [x] NX-PP-017 Broker restart resilience (SHM unaffected, MQTT mirrors restore)
- [x] NX-PP-018 External MCU join without SHM latency regression

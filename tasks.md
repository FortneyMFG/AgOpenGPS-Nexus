# Nexus Task Tracker

This backlog is the coordination surface for AIs and humans working on Nexus. Update the
status, owner, and human-verification fields as progress is made. Keep entries scoped to a
single NX ticket (≈20 minutes of focused work) unless an ADR states otherwise.

## Section Overview

### Section A — Foundations & Contracts
- **NX-001 Repo skeleton + solutions**
  - Output: tree, .sln/.csproj, EditorConfig, .gitattributes
  - Done: dotnet build on Win+Linux
- **NX-002 ADRs: language/runtime, gRPC, Avalonia, simulation model**
  - Output: /docs/adr/0001-0004.md
  - Done: committed & linked from README
- **NX-003 Protobuf v1: Header, Pose, Imu, SectionMask, SteerCmd/State, CanFrame, TimingCaps**
  - Output: .proto + codegen in Aog.Abstractions
  - Done: stubs compile
- **NX-004 JSON schemas: Core, AGiO, UI, Simulation (providers, routes, options)**
  - Output: /tools/schemas/*.schema.json + validator helper
  - Done: sample config validates
- **NX-005 Capabilities handshake (Capabilities service)**
  - Output: proto + Core/AGiO placeholders
  - Done: smoke test call works

### Section B — Core (Headless, Sim Graph)
- **NX-010 Core host (Generic Host, DI, config, Serilog)**
  - Output: running service skeleton
  - Done: starts/stops; health log
- **NX-011 Event bus (in-proc pub/sub)**
  - Output: interfaces + tests
  - Done: pub/sub test passes
- **NX-012 Sim primitives: ISimClock, ISimBus, ISimRng + fixed-step clock**
  - Output: implementations + tests (deterministic)
  - Done: same seed ⇒ identical series
- **NX-013 Sim catalog/graph builder & provider registry**
  - Output: registry + DAG validation
  - Done: circular deps rejected; summary printed
- **NX-014 Per-stream source selection (hardware/sim/replay) config & runtime routing**
  - Output: routing map in Core; events emitted
  - Done: unit tests cover route changes
- **NX-015 Parquet logger (Pose/Imu/Can/Io/Plugin topics)**
  - Output: writer + schema
  - Done: file created; replay reads

### Section C — AGiO & Backends
- **NX-020 AGiO host skeleton + backend loader**
  - Output: Aog.Agio with DI and backend selection
  - Done: loads Agio.Sim by config
- **NX-021 Agio.Sim: adapt SimBus topics to gRPC services (Gnss/Imu/Steer/Sections/CanBus/Timing)**
  - Output: working stub services using SimBus
  - Done: Core can subscribe
- **NX-022 Agio.Windows: COM NMEA auto-scan + parser (GGA/RMC/VTG)**
  - Output: scanner + parser + tests
  - Done: parses sample NMEA log
- **NX-023 Agio.Windows: Windows Location API provider (optional fallback)**
  - Output: WinRT wrapper
  - Done: emits coarse Pose if no COM
- **NX-024 Agio.Linux: serial NMEA auto-scan on /dev/tty\***
  - Output: provider reusing parser
  - Done: integration test (emulated serial)
- **NX-025 Agio.Linux: gpsd provider (if socket exists)**
  - Output: gpsd client
  - Done: test with recorded feed
- **NX-026 TimingCaps probe: PPS/PTP detection (Linux), none on Windows**
  - Output: caps returned
  - Done: unit/integration prints caps
- **NX-027 Legacy UDP gateway skeleton (AOG PGNs minimal set)**
  - Output: service that translates to/from gRPC
  - Done: loopback test OK

### Section D — Plugins (Logic + Sim Providers)
- **NX-030 Plugin loader & manifest (name, version, required APIs, settings, sim providers)**
  - Output: JSON manifest + loader
  - Done: loads sample plugin
- **NX-031 AutoSteer-Lite plugin v1 (Pure Pursuit + Stanley, choose at runtime)**
  - Output: controller core
  - Done: unit test holds straight line in sim
- **NX-032 Sections plugin v1 (8-bit mask, speed gate, look-ahead)**
  - Output: mask computation
  - Done: drives mask in sim
- **NX-033 PlanterMonitor plugin v1 (rows, skips/doubles, per-row rate)**
  - Output: model + state
  - Done: publishes RowStatus in sim
- **NX-034 Sim Providers bundled per plugin**
  - Output: providers registering with Core
  - Done: appear in sim catalog
  - Providers: gnss.basic (noise/dropout), sections.coverage (coverage map), autosteer.vehicle (bicycle dynamics), planter.basic (rows N, population, failure probabilities)
- **NX-035 Replay plugin v1 (Parquet reader; play/pause/seek/speed)**
  - Output: service & UI hooks
  - Done: replays test file

### Section E — UI (Avalonia) + Sim Bar
- **NX-040 Avalonia app bootstrap (cross-platform)**
  - Output: shell window + DI
  - Done: runs on Win & Linux
- **NX-041 Connection/settings panel (select AGiO endpoint, backend, GPS source policy)**
  - Output: MVVM view + save
  - Done: persists config
- **NX-042 Map view (Skia; pan/zoom; vehicle dot/heading)**
  - Output: draws Pose at 10 Hz
  - Done: follows sim
- **NX-043 Sim Bar (play/pause, 0.5×/1×/2×, seek; source routing UI per stream)**
  - Output: toolbar + bindings
  - Done: controls ISimClock; toggles sources
- **NX-044 Panels: Steer (enable/target/gains), Sections (8 toggles/auto), Planter (rows overview)**
  - Output: UI binds to plugin states
  - Done: interactive in sim
- **NX-045 Scenario editor (choose providers + options; save/load scenario JSON)**
  - Output: dialog + schema validation
  - Done: loaded scenario reflects in sim
- **NX-046 Simulation config summary in UI**
  - Output: Core simulation config loader feeding UI summary panel
  - Done: Sample scenario renders overview in the main window

### Section F — Porting from V6 (Algorithms)
- **NX-050 Inventory V6 math (paths, coverage, filters, sim)**
  - Output: /docs/porting/V6-Inventory.md
  - Done: committed with links
- **NX-051 Port coverage math (+ tests vs V6 CSVs)**
  - Output: Aog.Core.Coverage
  - Done: tolerances within spec
- **NX-052 Port AB/curve/headland generation (+ tests)**
  - Output: Aog.Core.Paths
  - Done: tests pass
- **NX-053 Port/tune controller gains/startup sequences**
  - Output: used by AutoSteer-Lite
  - Done: sim error ≤ target

### Section G — Packaging, DevEx, Docs
- **NX-007 SourceCode path normalization**
  - Output: Canonical `Nexus SourceCode` paths in AGENTS, README, and launch scripts
  - Done: Developer tooling resolves project defaults without manual fixes
- **NX-008 Schema validator registry upgrade**
  - Output: `tools/schemas/validate.py` loads schemas via the `referencing` registry
  - Done: CLI validates bundled samples and explicit configs with `$id` resolution
- **NX-009 Tooling SourceCode path fixes**
  - Output: PowerShell/Bash runner defaults match the `Nexus SourceCode` layout
  - Done: Contributor docs and scripts point at the correct directories
- **NX-060 Dev scripts: nexus run core|agio|ui, nexus sim**
  - Output: PowerShell/Bash in /tools/scripts
  - Done: one-line start for each process
- **NX-061 Windows packaging (single-file publish + zip/installer)**
  - Output: CI artifact
  - Done: runs on FZ-G1 clean install
- **NX-062 Linux (Pi) packaging (deb + systemd units)**
  - Output: .deb + *.service templates
  - Done: boots on Pi OS; services active
- **NX-063 Docs: “Windows, no hardware: mapping in 2 minutes”**
  - Output: /docs/howto/windows-no-hw.md
  - Done: verified on a fresh VM
- **NX-064 Docs: “Pi/CM5 quick start (Sim first, then GPS)”**
  - Output: /docs/howto/pi-sim.md
  - Done: verified on a Pi

### Section H — Safety & QA
- **NX-070 Heartbeats & failsafe (AGiO↔Plugins; outputs safe on timeout)**
  - Output: watchdogs + tests
  - Done: safe in <100 ms on kill
- **NX-071 Arming/state machine (no outputs until armed & profile valid)**
  - Output: enforced in Core
  - Done: attempts blocked when disarmed
- **NX-072 Deterministic sim regression (10 s run → same CSV)**
  - Output: CI test & golden vectors
  - Done: byte-identical outputs

### Section I — Legacy/Teensy Compatibility
- **NX-080 UDP discovery + caps/version exchange**
  - Output: small discovery packet
  - Done: AIO responds; caps logged
- **NX-081 PGN bridge (steer + sections minimal)**
  - Output: translation table + code
  - Done: drives a real AIO on bench
- **NX-082 UART framing (COBS+CRC), 921600 bps option**
  - Output: serial helper + test
  - Done: loopback verified

## Legend

- **Status:** `Planned`, `In Progress`, `Review`, `Done`, `Blocked`.
- **Owner:** GitHub handle or AI tag currently responsible. Leave blank if unassigned.
- **Human QA:** Name + date once someone has confirmed the feature on hardware or a
  realistic sim run. Leave `—` until verified.
- **SRS Ref:** Link to the SRS section describing the requirement.
- **Notes:** Dependencies, follow-ups, or freeze-window considerations.

## Wave 0 – Bootstrap & Ownership

| ID | Section | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NX-001 | A | Repo skeleton + solutions | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Tag v0.1.0-bootstrap after completion |
| NX-002 | A | ADRs: language/runtime, gRPC, Avalonia, simulation model | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Link from README |
| NX-006 | G | CI matrix (Win x64 + Linux arm64) | Done | AI | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Include lint + headless sim |
| NX-007 | G | SourceCode path normalization | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Canonical "Nexus SourceCode" references |
| NX-008 | G | Schema validator registry upgrade | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | `referencing`-based loader resolves `$id` links |
| NX-009 | G | Tooling SourceCode path fixes | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Run scripts default to SourceCode layout |

## Wave 1 – Contracts & Scaffolds

| ID | Section | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NX-003 | A | Protobuf v1 (Header, Pose, Imu, SectionMask, SteerCmd/State, CanFrame, TimingCaps) | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Start Contracts Freeze 1 |
| NX-004 | A | JSON schemas (Core, AGiO, UI, Simulation) | Done |  | — | [SRS §2.1 Foundations & Contracts](docs/SRS/NOTES.md#srs-21-foundations--contracts) | Coordinate with Schema Owner |
| NX-005 | A | Capabilities handshake proto/service | Done |  | — | [SRS §3.2 Capabilities Exchange](docs/SRS/NOTES.md#srs-32-capabilities-exchange) | Smoke test between Core & AGiO |
| NX-010 | B | Core host skeleton (Generic Host, DI, config, Serilog) | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) |  |
| NX-011 | B | Event bus interfaces + tests | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) |  |
| NX-014 | B | Settings + hot reload / route config | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Per-stream routing |
| NX-015 | B | Telemetry Parquet logger | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Writes plugin telemetry via TelemetryParquetLogger |
| NX-016 | B | Core health interval hot reload | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Runtime config adjusts heartbeat cadence |
| NX-017 | B | Source routing service | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Routes topics across sim/hardware/replay |
| NX-020 | C | AGiO host skeleton + backend loader | Done |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Loads Agio.Sim by config |
| NX-021 | C | Agio.Sim adapter to gRPC services | Planned |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) |  |
| NX-028 | B | Core capabilities handshake service | Done |  | — | [SRS §3.2 Capabilities Exchange](docs/SRS/NOTES.md#srs-32-capabilities-exchange) | gRPC client/service negotiates capabilities |
| NX-040 | E | Avalonia app bootstrap | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Windows + Linux |
| NX-041 | E | Connection/settings panel | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Persist config |

## Wave 2 – Composite Simulation Core

| ID | Section | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NX-012 | B | Sim primitives (ISimClock/Bus/Rng + fixed-step clock) | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Deterministic tests |
| NX-013 | B | Sim provider registry + DAG validation | Done |  | — | [SRS §3.1 Core Services](docs/SRS/NOTES.md#srs-31-core-services) | Reject circular deps |
| NX-072 | H | Deterministic sim regression (10s golden) | Planned |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Locks CI expectation |
| NX-034a | D | Sim provider: gnss.basic | Planned |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Register via Simulation plugin |
| NX-034b | D | Sim provider: sections.coverage | Planned |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) |  |
| NX-034c | D | Sim provider: autosteer.vehicle dynamics | Planned |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) |  |
| NX-034d | D | Sim provider: planter.basic | Planned |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) |  |
| NX-042 | E | Map view (Skia pan/zoom + vehicle pose) | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | MapView control with viewport tests |
| NX-043 | E | Sim Bar controls (play/pause/seek/rate + routing UI) | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Requires NX-012/013 |
| NX-045 | E | Scenario editor (providers + options) | Planned |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) |  |
| NX-046 | E | Simulation config summary in UI | Done |  | — | [SRS §3.4 UI Shell](docs/SRS/NOTES.md#srs-34-ui-shell) | Loader + main window summary panel |

## Wave 3 – Hardware Abstraction

| ID | Section | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NX-022 | C | Windows COM NMEA auto-scan + parser | Planned |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Include tests with sample logs |
| NX-023 | C | Windows Location API fallback | Planned |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Optional provider |
| NX-024 | C | Linux serial NMEA auto-scan | Planned |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Reuse parser |
| NX-025 | C | Linux gpsd provider | Planned |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Emulated feed test |
| NX-026 | C | TimingCaps probe (Linux PPS/PTP) | Planned |  | — | [SRS §3.3 AGiO Services](docs/SRS/NOTES.md#srs-33-agio-services) | Report jitter |
| NX-027 | C | Legacy UDP gateway skeleton | Planned |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Loopback test |

## Wave 4 – Core Plugins

| ID | Section | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NX-030 | D | Plugin loader & manifest handling | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Includes JSON manifest |
| NX-031 | D | AutoSteer-Lite plugin v1 | Planned |  | — | [SRS §3.6 AutoSteer](docs/SRS/NOTES.md#srs-36-autosteer) | Unit test holds AB line |
| NX-032 | D | Sections plugin v1 | Planned |  | — | [SRS §3.7 Sections Control](docs/SRS/NOTES.md#srs-37-sections-control) | Speed gate + look-ahead |
| NX-033 | D | PlanterMonitor plugin v1 | Planned |  | — | [SRS §3.8 Planter Monitor](docs/SRS/NOTES.md#srs-38-planter-monitor) | Publishes RowStatus |
| NX-034 | D | Sim provider registrations (bundle) | Planned |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | Combine with NX-034a–d |
| NX-035 | D | Replay plugin v1 | Planned |  | — | [SRS §3.9 Replay Services](docs/SRS/NOTES.md#srs-39-replay-services) | UI hooks |
| NX-036 | D | Plugin telemetry sink | Done |  | — | [SRS §3.5 Simulation Providers](docs/SRS/NOTES.md#srs-35-simulation-providers) | EventBus sink streams plugin telemetry |

## Wave 5 – V6 Porting

| ID | Section | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NX-050 | F | V6 math inventory | Planned |  | — | [SRS §5.1 V6 Porting Inventory](docs/SRS/NOTES.md#srs-51-v6-porting-inventory) | docs/porting/V6-Inventory.md |
| NX-051 | F | Coverage math port + tests | Planned |  | — | [SRS §5.2 Coverage Math](docs/SRS/NOTES.md#srs-52-coverage-math) | Compare vs V6 CSVs |
| NX-052 | F | AB/curve/headland generation port | Planned |  | — | [SRS §5.3 Path Generation](docs/SRS/NOTES.md#srs-53-path-generation) | Tests green |
| NX-053 | F | Controller gains/tuners | Planned |  | — | [SRS §5.4 Controller Gains](docs/SRS/NOTES.md#srs-54-controller-gains) | Meets error targets |

## Wave 6 – Packaging, Docs, Safety

| ID | Section | Description | Status | Owner | Human QA | SRS Ref | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| NX-060 | G | Dev scripts (`nexus run core|agio|ui`, `nexus sim`) | Done |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Bash + PowerShell |
| NX-061 | G | Windows packaging | Planned |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Installer artifact |
| NX-062 | G | Pi/CM5 packaging (deb + systemd) | Planned |  | — | [SRS §2.7 Packaging & DevEx](docs/SRS/NOTES.md#srs-27-packaging--devex) | Boots on Pi OS |
| NX-063 | G | How-to: Windows, no hardware | Planned |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | docs/howto/windows-no-hw.md |
| NX-064 | G | How-to: Pi/CM5 quick start | Planned |  | — | [SRS §2.8 Documentation](docs/SRS/NOTES.md#srs-28-documentation) | docs/howto/pi-sim.md |
| NX-070 | H | Heartbeats & failsafe watchdogs | Planned |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Timeout <100 ms |
| NX-071 | H | Arming/state machine | Planned |  | — | [SRS §4.2 Safety & QA](docs/SRS/NOTES.md#srs-42-safety--qa) | Blocks unsafe outputs |
| NX-081 | I | Legacy UDP discovery + caps exchange | Planned |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | Teensy AIO response |
| NX-082 | I | Legacy UART framing (COBS+CRC) | Planned |  | — | [SRS §4.3 Legacy Compatibility](docs/SRS/NOTES.md#srs-43-legacy-compatibility) | 921600 bps option |

## Updating This File

- Move tickets to `Done` only after merging to `main`.
- Record freeze windows, cross-team dependencies, or ADR links in the `Notes` column.
- Append new NX-IDs as they are created; keep the wave groupings intact.

# Hardware I/O (Status: collecting proposals)

## Problem statement
Capture how AgOpenGPS and AgIO interface with GNSS, steer, rate, machine, and sensor hardware, including wiring, protocols, and diagnostics.

## Requirements (from contributors)
- R-HW-000 (MUST, current-AgIO): Maintain serial port routing for GPS, IMU, steer, machine, and RTCM channels configurable from the UI.【F:SourceCode/AgIO/Source/Forms/FormCommSetGPS.cs†L20-L160】
- R-HW-001 (MUST, current-AgOpenGPS): Keep PGN-based machine and section control framing for auto-steer and relay modules.【F:SourceCode/GPS/Forms/PGN.Designer.cs†L430-L491】
- R-HW-002 (SHOULD, current-SK21-ROC): Ensure compatibility with SK21 rate control hardware referenced by the project ecosystem.【F:README.md†L70-L76】
- R-HW-003 (SHOULD, current-AgIO): Retain UDP scanning and diagnostic tools that locate steer, machine, IMU, and GPS modules on the network.【F:SourceCode/AgIO/Source/Forms/FormUDP.cs†L13-L160】
- R-HW-010 (MUST, proposed-variable-layer): Advertise firmware capabilities, layer support, and channel-to-layer mappings so configuration flows can link hardware inputs to logical telemetry.【F:docs/SRS/options/O-HW-5_ModularLayerFirmware.md†L1-L27】
- R-HW-011 (SHOULD, proposed-variable-layer): Provide presets, validation, and throttling controls for high-frequency sensors to prevent operator misconfiguration and UDP congestion.【F:docs/SRS/options/O-HW-5_ModularLayerFirmware.md†L28-L45】
- R-HW-004 (SHOULD, proposed-LinuxCore): Offer SocketCAN, udev-based serial naming, and bridge services so Linux SBCs can connect to GNSS/IMU/section hardware without bespoke drivers.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L11-L20】
- R-HW-005 (MUST, proposed-PGNBridge): Document the existing PGN catalog and ensure any new hardware abstraction keeps backward compatibility or provides adapters.【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L120】【F:docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】
- R-HW-006 (COULD): Add standardized hardware capability discovery (e.g., via PGN handshake) beyond today’s manual settings.
- R-HW-012 (SHOULD, proposed-ISOBUS-alignment): Introduce ISOBUS-inspired condensed work state PGNs (289-291, 141, 161-162, 367)
  while maintaining legacy message support so section controllers and future ISOBUS bridges share a documented structure.【F:docs/SRS/references/ISOBUS_Section_Control.md†L1-L33】

- R-HW-013 (MUST, safety): Require watchdogs, fail-safe defaults, and manual override paths when introducing modular firmware so richer telemetry cannot block steer/section cutoffs during controller faults.
- R-HW-014 (SHOULD, compliance): Reserve placeholders for required certifications or field validation (e.g., ISO 25119 functional safety notes) whenever hardware abstractions or PGN bridges change safety envelopes.

## Options
- O-HW-0: Status quo — Serial + UDP PGNs managed by AgIO with manual module discovery.
- O-HW-1: Introduce a hardware abstraction layer with per-device drivers.
- O-HW-2: Move to CANopen/ISOBUS-first modules with UDP as a bridge.
- O-HW-3: Add plug-and-play USB/HID devices for sensors and switches.
- O-HW-4: Provide a modular gateway (e.g., embedded Linux) that proxies between transports.
- O-HW-5: [Modular firmware publishing variable-rate layers](../options/O-HW-5_ModularLayerFirmware.md) — Discovery + mapping for flexible layer telemetry.
- O-HW-6: Linux gateway + PGN bridge that proxies SocketCAN/serial into Core APIs.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L11-L44】【F:docs/SRS/options/O-COMM-6_PGNCompatibilityBridge.md†L1-L35】

## Comparison (quick matrix)
| Option | Pros | Cons | Risks | Borrow from existing |
|---|---|---|---|---|
| O-HW-0 | Works with today’s Arduino-based modules | Manual config per rig | Operator error on setup | AgIO dialogs + PGNs |
| O-HW-1 | Cleaner extension story | Driver maintenance burden | Testing matrix expands | AgLibrary abstractions |
| O-HW-2 | Standards-based | Firmware updates required | Vendor lock-in | PGN knowledge |
| O-HW-3 | Quick add-ons | Windows driver headaches | USB reliability in cab | Keypad project |
| O-HW-4 | Decouples desktop from hardware | More hardware to ship | Gateway failure path | Existing UDP monitor |
| O-HW-5 | Firmware-led mapping of rich telemetry | New configuration complexity | Capability drift between modules | AgIO discovery + layer registry plan |
| O-HW-6 | Lets existing hardware talk to a Linux Core with minimal rewiring | Adds gateway component + service management | Misconfigured bridge could break safety I/O | Linux Core + PGN bridge |

## Evaluation criteria
Field reliability, ease of install, compatibility with existing rigs, firmware update path, diagnostics clarity.

## Current sentiment
- Keep current modules online while exploring what a hardware abstraction would require to avoid regressions for SK21 and similar systems.
- Early adopters want firmware-discovered layer capabilities, but insist on a “legacy-only” toggle for rigs that cannot yet stream the richer telemetry.【F:docs/SRS/options/O-HW-5_ModularLayerFirmware.md†L46-L57】
- Linux pilots must prove SocketCAN + PGN bridging works before operators consider retiring Windows tablets in the cab.【F:docs/SRS/options/O-BACKEND-6_LinuxCoreService.md†L21-L44】【F:docs/SRS/references/AgIO_PGN_Baseline.md†L1-L120】

## Open questions
- How do we validate new hardware in CI without requiring physical rigs?
- What handshake (if any) should new modules implement to advertise capabilities?
- How quickly should AgOpenGPS mark ISOBUS-style condensed work state feedback as stale when PGNs are missed, and should UDP keep
  the legacy CRC for serial parity?【F:docs/SRS/references/ISOBUS_Section_Control.md†L35-L40】

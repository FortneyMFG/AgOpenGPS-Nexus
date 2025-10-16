# CM5 Integrated Controller (Status: drafting requirements)

## Problem statement
When a Compute Module 5 (CM5) runs AgOpenGPS Core, navigation, steer control, and the
AgIO Bridge on the same device, the platform needs a deterministic control fast-path
while keeping external microcontrollers interoperable. The SRS must codify how the
CM5 bypasses AgIO for on-box traffic, preserves MQTT/UDP fan-out for visibility, and
manages controller authority so legacy hardware can join without bespoke firmware.

## Requirements (from contributors)
- R-CM5-000 (MUST, fast-path): Provide a shared-memory (or equivalent in-process) fast
  path for `SteerTarget` updates between NAV and steer-ctrl when both run on the CM5,
  maintaining <2 ms p50 latency and mirroring the latest target onto MQTT for
  observers and external controllers.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L286-L305】【F:docs/plugins/pumpkin-pi.md†L3-L22】
- R-CM5-001 (MUST, AgIO coexistence): Keep the AgIO Bridge online for UDP, serial,
  CAN-FD, and MQTT-SN adapters even when the CM5 consumes the fast-path, ensuring
  Pumpkin Pi mirrors setpoints/status back to bridge topics so dashboards and
  off-box hardware remain synchronized.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L308-L323】【F:docs/plugins/pumpkin-pi.md†L23-L55】
- R-CM5-002 (MUST, authority): Implement retained authority tokens on
  `aog/v1/ctrl/authority/{group}` with ≤150 ms acknowledgement so integrated CM5
  controllers and external MCUs can negotiate control safely. Controllers must ignore
  setpoints older than the negotiated TTL and fall back to safe outputs when no
  authority holder is present.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L324-L347】【F:docs/getting-started/cm5.md†L34-L38】
- R-CM5-003 (MUST, scheduling): Run steer-ctrl and Pumpkin Pi with real-time
  scheduling, isolated CPU affinity, and locked memory on PREEMPT_RT kernels to avoid
  jitter when AgIO or UI workloads spike. Document the required capabilities and
  service configuration for CM5 images.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L330-L341】【F:docs/getting-started/cm5.md†L6-L33】
- R-CM5-004 (SHOULD, MCU coexistence): Allow external MCUs to observe fast-path
  setpoints, subscribe via standard transports, and assume specific actuator groups
  without schema changes, relying on authority tokens for arbitration.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L308-L327】
- R-CM5-005 (MUST, fault handling): Detect expired authority tokens or missing health
  heartbeats within three intervals, reclaim control locally, and drive neutral
  outputs until NAV resumes publishing valid setpoints.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L338-L347】
- R-CM5-006 (SHOULD, deployment): Ship reference configuration (`pumpkin.yaml`) and
  systemd units so CM5 images start the fast-path HAL automatically, verify shared
  memory compatibility, and expose health logs for validation.【F:docs/plugins/pumpkin-pi.md†L57-L63】【F:docs/getting-started/cm5.md†L10-L33】

## Context and scope
CM5 integrated mode targets arm64 deployments where NAV, steer-ctrl, the AgIO Bridge,
MQTT broker, and optional UI share a single host. External devices—legacy AgIO
modules, ISOBUS bridges, rate controllers—continue using documented UDP, serial, CAN,
and MQTT transports. The scope covers deterministic control loops, authority
negotiation, and observability needed to keep mixed deployments interoperable.

## Architecture overview
- **Process layout:** User-space services split into host/broker roles (Core gRPC,
  adapters, MQTT) and real-time control roles (GPS ingest, IMU ingest, steer-ctrl)
  with Pumpkin Pi owning the fast-path HAL.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L294-L311】【F:docs/plugins/pumpkin-pi.md†L3-L29】
- **Data paths:** Navigation publishes `SteerTarget` through shared memory first,
  mirroring MQTT topics (`aog/v1/bus/nav/steer_target`) so authority arbitration and
  diagnostics stay visible. External controllers may continue to receive UDP/serial
  fast-path frames via AgIO adapters.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L300-L323】【F:docs/plugins/pumpkin-pi.md†L23-L44】
- **Authority:** Retained MQTT tokens coordinate which node currently drives each
  actuator group. Default ownership remains with CM5 unless reassigned; Pumpkin Pi
  must acknowledge or relinquish control promptly.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L324-L337】【F:docs/getting-started/cm5.md†L34-L38】

## Operational guidance
- Enable PREEMPT_RT kernels, grant `cap_sys_nice`, and pin steer-ctrl to dedicated
  cores to honour latency budgets. Maintain documentation for CM5 images describing
  kernel, scheduler, and capability expectations.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L330-L341】【F:docs/getting-started/cm5.md†L6-L33】
- Keep `/dev/shm/aoglink_steer` versions matched between NAV and steer-ctrl builds;
  service start-up must fail fast when ABI versions diverge and log actionable
  remediation steps.【F:docs/plugins/pumpkin-pi.md†L57-L63】
- Mirror `SteerStatus`, health, and fast-path diagnostics back to MQTT so UI shells
  and AgIO telemetry remain aware of on-box activity, even when the fast-path bypasses
  network transports.【F:docs/plugins/pumpkin-pi.md†L3-L44】
- Preserve deterministic fallbacks: if NAV stalls or MQTT link breaks, controllers
  revert to neutral outputs, publish health warnings, and reassert authority locally
  once heartbeats recover.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L338-L347】

## Verification
- Fast-path latency meets ≤2 ms p50 / ≤5 ms p95 targets with shared memory enabled
  and remains ≤8 ms p95 when falling back to MQTT/UDP only.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L360-L368】
- Authority handoff completes within ≤150 ms on-device and ≤300 ms over LAN, with
  controllers ignoring stale setpoints beyond negotiated TTLs.【F:docs/SRS/sections/03A_AOG_Link_v1.md†L334-L347】
- Pumpkin Pi service starts automatically on CM5 images, validates shared memory
  compatibility, and exposes health logs through systemd and MQTT topics for
  operators.【F:docs/plugins/pumpkin-pi.md†L57-L63】【F:docs/getting-started/cm5.md†L10-L33】

## Related ADRs and sections
- [Section 03A — AOG Link v1](03A_AOG_Link_v1.md)
- [Pumpkin Pi Plugin](../../plugins/pumpkin-pi.md)
- [CM5 Integrated Controller setup](../../getting-started/cm5.md)

# ADR-00XX: CM5 SHM Fastpath + HAL Plugin (“Pumpkin Pi”)

## Status
Accepted (target release: AOG-Link v1.0)

**Relevant Plugin(s):** Pumpkin Pi (HAL, Autosteer Integration)

## Context
Running navigation and steer control on the same Compute Module 5 (CM5) introduces avoidable jitter when setpoints traverse the broker or AgIO sockets. The CM5 integrated-controller requirements demand a deterministic shared-memory fast path that keeps <2 ms p50 latency while still mirroring telemetry for external hardware and UI observers.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L3-L78】【F:docs/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L287-L339】 At the same time, the hardware I/O plan preserves AgIO as a privileged plugin so UDP, serial, CAN-FD, and MQTT-SN adapters remain online for legacy modules and future HAL backends.【F:docs/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L6-L27】 Pumpkin Pi formalizes this integrated path by owning a CM5-local hardware abstraction layer (HAL) and mediating authority so external controllers can still participate without bespoke firmware.【F:docs/Plugins/briefs/pumpkin-pi.md†L1-L30】【F:docs/AgIO/cm5.md†L1-L38】

## Decision
Pumpkin Pi will ship as a CM5-local plugin that:

- Exposes a versioned shared-memory ring (`/dev/shm/aoglink_steer`) and eventfd notifications so NAV publishes the latest `SteerTarget` with <2 ms p50 latency and mirrors retained copies on MQTT topics for observability and fallback.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L11-L33】【F:docs/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L298-L323】【F:docs/Plugins/briefs/pumpkin-pi.md†L5-L28】
- Owns the CM5 HAL, providing libgpiod, PWM, SocketCAN, I²C, and SPI backends that apply setpoints deterministically while health and status telemetry flow back to MQTT and the AgIO Bridge.【F:docs/Plugins/briefs/pumpkin-pi.md†L5-L30】
- Keeps the AgIO Bridge enabled so legacy transports, MQTT-SN gateways, and v0 PGN bridges continue to operate, with Pumpkin Pi mirroring targets and status into bridge topics to maintain UI dashboards and mixed-fleet compatibility.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L14-L30】【F:docs/AgIO/README.md†L10-L76】
- Implements retained authority tokens on `aog/v1/ctrl/authority/{group}`; CM5 holds authority by default, responds within 150 ms, and relinquishes control when external controllers request takeover, reverting to safe outputs if heartbeats or TTLs expire.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L19-L35】【F:docs/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L312-L337】【F:docs/AgIO/cm5.md†L33-L38】
- Runs with PREEMPT_RT scheduling hints (FIFO ≥80, CPU pinning, `mlockall`) and fails fast when SHM ABI versions mismatch so deterministic behavior is preserved during upgrades.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L24-L69】【F:docs/AgIO/cm5.md†L3-L31】
- Publishes version metadata and health summaries over MQTT so CI benches and operators can verify fast-path timing, HAL initialization, and authority state alongside existing AgIO diagnostics.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L71-L78】【F:docs/Plugins/briefs/pumpkin-pi.md†L54-L63】

## Consequences
- ✅ Steering loops on CM5 meet the <2–5 ms latency target without bypassing observability or legacy transports, improving closed-loop stability under combined workloads.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L11-L33】【F:docs/AgIO/README.md†L10-L76】
- ✅ External MCUs continue to join via AgIO transports and observe fast-path telemetry, reducing migration risk for mixed fleets.【F:docs/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L305-L339】
- ⚠️ The SHM ABI requires strict versioning and startup validation; release engineering must coordinate NAV, steer-ctrl, and Pumpkin Pi updates to prevent mismatched rings.【F:docs/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md†L407-L409】【F:docs/Plugins/briefs/pumpkin-pi.md†L57-L63】
- ⚠️ Real-time scheduling and capability grants (`cap_sys_nice`, CPU isolation) add operational steps for CM5 images, demanding dedicated deployment documentation and checks.【F:docs/sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md†L24-L69】【F:docs/AgIO/cm5.md†L3-L31】
- ❌ SHM-only builds without AgIO cannot serve external hardware, so production images must continue bundling the bridge until a HAL federation strategy exists.【F:docs/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L22-L27】【F:docs/Plugins/briefs/pumpkin-pi.md†L28-L30】
- Follow-ups: define the SHM ABI schema, land HAL backend tests, extend CI benches for latency verification, and publish systemd units plus `pumpkin.yaml` defaults as separate NX-PP tasks.【F:tasks.md†L747-L773】

## Legacy Implementation Notes
### AgOpenGPS v6
Legacy deployments route steer setpoints exclusively through AgIO-managed serial/UDP PGNs without a CM5-local HAL, so latency depends on broker and bridge scheduling.【F:docs/sections/4X_Interprocess_Communications/42_Transports.md†L6-L15】 Pumpkin Pi introduces the first integrated fast path for Nexus.

### Legacy Dev Branch
The legacy dev branch prototypes Linux bridges and PGN compatibility but still relies on AgIO services for hardware arbitration; no dedicated HAL plugin or SHM ring exists, underscoring the need for Pumpkin Pi in CM5 integrated mode.【F:docs/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L6-L35】【F:docs/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md†L11-L44】

## References
- [Section 53 — AOG-Link Compatibility](../sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md)
- [Section 51 — Sensor & Actuator Abstractions](../sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md)
- [Section 54 — CM5 Integrated Controller](../sections/5X_Hardware_IO_Device_Layer/54_CM5_Integrated_Controller.md)
- [Pumpkin Pi Plugin Guide](../Plugins/briefs/pumpkin-pi.md)
- [CM5 Integrated Controller Setup](../AgIO/cm5.md)
- [AgIO subsystem overview](../AgIO/README.md)
- [NX Task Tracker — Section E](../../tasks.md)

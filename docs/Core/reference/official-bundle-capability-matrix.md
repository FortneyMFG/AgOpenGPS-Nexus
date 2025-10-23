# Official Plugin Bundle Capability Matrix

*Updated: 2025-10-15 UTC*

## Overview

ADR-031 designates the official plugin bundle as the authoritative reference for Nexus
capability governance. This matrix aggregates the capabilities declared in the bundled
manifests, the lease posture Core expects at runtime, and links back to the source
artifacts. Use it alongside the [capability registry](../development/SRS/references/core/capability-registry.md) when
reviewing manifest changes or diagnosing loader warnings so that required capabilities
remain consistent across releases.【F:docs/development/SRS/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L12-L45】

## Capability matrix

| Plugin | Capability | Lease mode | Purpose | Manifest |
| --- | --- | --- | --- | --- |
| AutoSteer | `guidance.control` | Exclusive | Provides fused-pose steering control over Core arbitration channels. | [autosteer 1.0.0](../Plugins/manifests/autosteer/1.0.0.json) |
| AutoSteer Lite | `guidance.control` | Exclusive | Pure pursuit steering controller used for lab validation and fallback rigs. | [autosteer-lite 1.2.0](../Plugins/manifests/autosteer-lite/1.2.0.json) |
| AutoSteer Lite | `guidance.telemetry` | Shared | Streams controller status and engage state for dashboards and health panels. | [autosteer-lite 1.2.0](../Plugins/manifests/autosteer-lite/1.2.0.json) |
| Device Manager | `devices.inventory` | Exclusive | Discovers and catalogs attached hardware with transport bindings. | [device-manager 1.0.0](../Plugins/manifests/device-manager/1.0.0.json) |
| Device Manager | `devices.health` | Shared | Publishes hardware health and fault telemetry for operator visibility. | [device-manager 1.0.0](../Plugins/manifests/device-manager/1.0.0.json) |
| Device Manager | `devices.firmware` | Exclusive | Orchestrates firmware eligibility checks and update workflows. | [device-manager 1.0.0](../Plugins/manifests/device-manager/1.0.0.json) |
| File I/O | `fileio.import` | Shared | Handles agronomic data import pipelines and provenance hydration. | [file-io 1.0.0](../Plugins/manifests/file-io/1.0.0.json) |
| File I/O | `fileio.export` | Shared | Exports layers, jobs, and manifest data in supported formats. | [file-io 1.0.0](../Plugins/manifests/file-io/1.0.0.json) |
| GNSS/IMU Fusion | `navigation.pose` | Exclusive | Publishes fused pose estimates aligned with Core timing requirements. | [gnss-imu-fusion 1.0.0](../Plugins/manifests/gnss-imu-fusion/1.0.0.json) |
| GNSS/IMU Fusion | `navigation.imu` | Shared | Streams raw IMU telemetry for fusion, diagnostics, and replay. | [gnss-imu-fusion 1.0.0](../Plugins/manifests/gnss-imu-fusion/1.0.0.json) |
| GNSS/IMU Fusion | `navigation.pose.quality` | Shared | Publishes covariance estimates and health metrics for downstream gating. | [gnss-imu-fusion 1.0.0](../Plugins/manifests/gnss-imu-fusion/1.0.0.json) |
| ISOBUS Bridge | `isobus.task-controller` | Exclusive | Bridges Task Controller command channels onto Nexus transports. | [isobus-bridge 1.0.0](../Plugins/manifests/isobus-bridge/1.0.0.json) |
| ISOBUS Bridge | `isobus.universal-terminal` | Exclusive | Exposes Universal Terminal UI flows for compatible implements. | [isobus-bridge 1.0.0](../Plugins/manifests/isobus-bridge/1.0.0.json) |
| ISOBUS Bridge | `bridge.udp-mirror` | Shared | Mirrors ISOBUS frames to UDP for diagnostics and remote tooling. | [isobus-bridge 1.0.0](../Plugins/manifests/isobus-bridge/1.0.0.json) |
| Job Tasks | — | — | Current manifest does not publish runtime capabilities; lifecycle orchestration remains internal. | [job-tasks 1.0.0](../Plugins/manifests/job-tasks/1.0.0.json) |
| NTRIP Client | `gnss.corrections` | Exclusive | Streams RTCM corrections to pose fusion providers via AGiO. | [ntrip-client 1.0.0](../Plugins/manifests/ntrip-client/1.0.0.json) |
| NTRIP Client | `telemetry.corrections` | Shared | Publishes correction stream health metrics for operator dashboards. | [ntrip-client 1.0.0](../Plugins/manifests/ntrip-client/1.0.0.json) |
| Planter Monitor | `planter.monitor.telemetry` | Exclusive | Streams planter sensor telemetry for row-level monitoring. | [planter-monitor 1.0.0](../Plugins/manifests/planter-monitor/1.0.0.json) |
| Planter Monitor | `planter.monitor.analytics` | Shared | Publishes derived agronomic analytics for dashboards and reports. | [planter-monitor 1.0.0](../Plugins/manifests/planter-monitor/1.0.0.json) |
| Replay | `replay.guidance` | Shared | Provides deterministic guidance command replay streams for analysis. | [replay 1.0.0](../Plugins/manifests/replay/1.0.0.json) |
| Replay | `replay.pose` | Shared | Provides deterministic pose replay streams for overlay and validation. | [replay 1.0.0](../Plugins/manifests/replay/1.0.0.json) |
| Sections | `sections.control` | Exclusive | Commands boom and row actuators using Core arbitration policies. | [sections 1.1.0](../Plugins/manifests/sections/1.1.0.json) |
| Sections | `sections.telemetry` | Shared | Streams duty cycle, switch feedback, and diagnostics. | [sections 1.1.0](../Plugins/manifests/sections/1.1.0.json) |
| Telemetry Logging | `telemetry.logging` | Shared | Journals structured telemetry for replay and diagnostics pipelines. | [telemetry-logging 1.0.0](../Plugins/manifests/telemetry-logging/1.0.0.json) |

## Usage

- **Governance:** Compare manifest updates against this matrix during reviews. Hard/soft
  dependency posture should match the lease mode listed here; deviations require ADR-031
  sign-off.
- **Diagnostics:** Device Manager and UI Shell surface capability gaps using the same
  identifiers, enabling operators to reconcile missing plugins quickly.
- **Extensibility:** Community plugins should reference these canonical identifiers when
  declaring compatibility with the official bundle to avoid drift in capability naming.

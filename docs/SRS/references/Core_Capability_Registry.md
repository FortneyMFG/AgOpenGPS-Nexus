# Core Capability Registry Reference

The capability registry enumerates the canonical identifiers that Core, AgIO, and
plugins negotiate during the capability handshake. This appendix is the
normative source for identifier names, default semantic versions, and required
attributes referenced by [Section 62 — Job Lifecycle](../sections/6X_Core_Domain_Services/62_Job_Lifecycle.md)
and [ADR-031](../sections/9X_Frontends_Ops/94-ADR-031%20-%20Official%20Plugin%20Bundle%20Dependency%20Governance.md).

## Capability catalog

| Name | Category | Default version | Summary | Attributes |
| --- | --- | --- | --- | --- |
| `guidance.control` | Guidance | 1.0.0 | Provides closed-loop autosteer control and arbitration services. | `bundle=guidance`, `mode=exclusive` |
| `guidance.telemetry` | Guidance | 1.0.0 | Publishes guidance status, engage state, and controller diagnostics for operator dashboards. | `bundle=guidance`, `mode=shared` |
| `mapping:raster` | Mapping | 1.0.0 | Publishes raster coverage tiles, rate surfaces, and diagnostics. | `bundle=mapping`, `surface=raster` |
| `mapping:vector` | Mapping | 1.0.0 | Provides vector layer ingestion, editing, and export pipelines. | `bundle=mapping`, `surface=vector` |
| `mapping:offline` | Mapping | 1.0.0 | Indicates the NullMapping provider is active and mapping features are offline. | `bundle=mapping`, `status=degraded` |
| `mapping:unavailable` | Mapping | 1.0.0 | Signals that no mapping provider is available on the host. | `bundle=mapping`, `status=absent` |
| `zones:evaluate` | Zones | 1.0.0 | Evaluates pose samples against zone constraints and publishes PoseZoneMask state. | `bundle=zones`, `role=evaluation` |
| `zones:registry` | Zones | 1.0.0 | Publishes zone registry snapshots and validation hashes for consumers. | `bundle=zones`, `role=authority` |
| `zones:edit` | Zones | 1.0.0 | Supports collaborative zone editing, journaling, and reconciliation workflows. | `bundle=zones`, `role=editor` |
| `sections.control` | Sections | 1.0.0 | Commands boom and row actuators using Core arbitration policies. | `bundle=sections`, `mode=exclusive` |
| `sections.telemetry` | Sections | 1.0.0 | Streams duty cycle, switch feedback, and diagnostics from section controllers. | `bundle=sections`, `mode=shared` |
| `fileio.import` | Data Operations | 1.0.0 | Handles import workflows for agronomic layers, jobs, and provenance manifests. | `bundle=file-io`, `mode=shared` |
| `fileio.export` | Data Operations | 1.0.0 | Exports agronomic layers, jobs, and provenance manifests in supported formats. | `bundle=file-io`, `mode=shared` |
| `replay.guidance` | Replay | 1.0.0 | Provides deterministic guidance command replay streams for analysis. | `bundle=replay`, `mode=shared` |
| `replay.pose` | Replay | 1.0.0 | Provides deterministic pose replay streams for overlay and validation. | `bundle=replay`, `mode=shared` |
| `devices.inventory` | Devices | 1.0.0 | Publishes discovered devices, hardware identifiers, and transport bindings. | `bundle=devices`, `mode=exclusive` |
| `devices.health` | Devices | 1.0.0 | Streams device health, fault states, and telemetry for operator dashboards. | `bundle=devices`, `mode=shared` |
| `devices.firmware` | Devices | 1.0.0 | Coordinates firmware update orchestration and eligibility checks for managed devices. | `bundle=devices`, `mode=exclusive` |
| `navigation.pose` | Navigation | 1.0.0 | Publishes fused pose estimates aligned with Core timing requirements. | `bundle=navigation`, `stream=pose` |
| `navigation.imu` | Navigation | 1.0.0 | Streams raw IMU telemetry for pose fusion and diagnostics. | `bundle=navigation`, `stream=imu` |
| `navigation.pose.quality` | Navigation | 1.0.0 | Publishes pose quality metrics and covariance estimates for downstream gating. | `bundle=navigation`, `stream=pose-quality` |
| `gnss.corrections` | Navigation | 1.0.0 | Streams RTCM or equivalent GNSS correction data to pose fusion providers. | `bundle=navigation`, `channel=rtcm` |
| `isobus.task-controller` | Transports | 1.0.0 | Bridges ISOBUS Task Controller (TC) workflows to Core capability consumers. | `bundle=isobus`, `mode=exclusive` |
| `isobus.universal-terminal` | Transports | 1.0.0 | Exposes ISOBUS Universal Terminal (UT) UI channels for compatible implements. | `bundle=isobus`, `mode=exclusive` |
| `bridge.udp-mirror` | Transports | 1.0.0 | Mirrors ISOBUS frames onto UDP for diagnostics and remote tooling integration. | `bundle=isobus`, `mode=shared` |
| `telemetry.corrections` | Telemetry | 1.0.0 | Publishes correction stream health metrics for operator visibility. | `bundle=telemetry`, `mode=shared` |
| `telemetry.logging` | Telemetry | 1.0.0 | Provides structured telemetry journaling for replay and diagnostics. | `bundle=telemetry`, `mode=shared` |
| `planter.monitor.telemetry` | Agronomy | 1.0.0 | Streams planter sensor telemetry for row-level monitoring. | `bundle=planter-monitor`, `mode=exclusive` |
| `planter.monitor.analytics` | Agronomy | 1.0.0 | Publishes planter analytics and derived agronomic metrics. | `bundle=planter-monitor`, `mode=shared` |

## Stewardship

* Changes to this catalog require ADR approval and test coverage updates per
  [Section 64 — Telemetry Health](../sections/6X_Core_Domain_Services/64_Telemetry_Health.md).
* Plugin manifests must only advertise identifiers listed here or in an approved
  extension namespace while ADR-031 governance tooling remains authoritative.

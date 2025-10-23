# Plugin Briefs

This directory houses design briefs and runbooks for individual Nexus plugins.
They outline responsibilities, dependencies, and planned UX flows so you can
coordinate updates across Core, AgIO, and the UI.

## Guidance & Automation

- [Guidance](Guidance.md) — Guidance orchestrator feeds, coverage policies, and steering lifecycle.
- [Automation Engine](AutomationEngine.md) — Planned rule system that reacts to telemetry and task events.
- [Rate Control](RateControl.md) — Setpoint coordination with actuators and safety gating.
- [Multi-Machine](MultiMachine.md) — Peer-to-peer coordination for fleets and convoy workflows.
- [Core Lifecycle](CoreLifecycle.md) — Session boot, lease negotiation, and plugin activation hooks.
- [Sync Dashboard](SyncDashboard.md) — Remote monitoring surfaces for status, alerts, and bundles.

## Mapping & Agronomy

- [Mapping](Mapping.md) — Layer storage, field context, and overlay orchestration.
- [Map Composer](MapComposer.md) — Offline map blending, exports, and styling pipelines.
- [Variable Mapping](VariableMapping.md) — Grid ingestion, zoning tools, and prescription feeds.
- [Agronomic Advisor](AgronomicAdvisor.md) — Recommendation loops tied to crop, soil, and yield layers.
- [Soil Lab](SoilLab.md) — Sampling workflows and lab result ingestion.
- [Terrain 3D](Terrain3D.md) — Elevation meshes, terrain shading, and rendering requirements.
- [Profit](Profit.md) — Cost tracking, break-even analytics, and profitability surfaces.
- [Yield](Yield.md) — Yield normalization, analytics, and comparison overlays.
- [Weather](Weather.md) — Weather ingest, forecasts, and compliance messaging.
- [Genetics](Genetics.md) — Hybrid tracking, trait overlays, and genetic analytics.

## Operations & Compliance

- [Job Tasks](JobTasks.md) — Save/resume lifecycle, job metadata, and operator workflow.
- [Regulatory](Regulatory.md) — Compliance exports, audit logging, and record retention.
- [Replay](Replay.md) — Deterministic playback, export options, and validation tooling.
- [Telemetry Logging](TelemetryLogging.md) — Session capture, retention policies, and replay hooks.
- [Device Manager](DeviceManager.md) — Hardware inventory, health monitoring, and provisioning.
- [Equipment Health](EquipmentHealth.md) — Maintenance analytics and diagnostic flags.
- [File IO](FileIO.md) — Import/export pipelines for layers, jobs, and supporting data.
- [CLI Extensions](CLIExtensions.md) — Command-line helpers, automation entry points, and scripting.

## Hardware & Connectivity

- [ISOBUS Bridge](IsobusBridge.md) — TC/UT translation, diagnostics, and compatibility guardrails.
- [GNSS Corrections](GnssCorrections.md) — Correction ingest, provider support, and fallback plans.
- [GNSS/IMU Fusion](GnssImuFusion.md) — Pose estimation, sensor alignment, and failover behavior.

## Analytics & Dashboards

- [Analytics](Analytics.md) — Insight pipelines, dashboards, and KPI governance.
- [Plugin Catalog](PluginCatalog.md) — Marketplace UX, dependency resolution, and signing expectations.

## Specialty & Experimental

- [Planter Monitor](planter-monitor.md) — Row-unit sensing, diagnostics, and agronomic tie-ins.
- [Pumpkin Pi](pumpkin-pi.md) — Experimental Pi-based bundle used for hardware bring-up drills.

## Related References

- [Performance guardrails](../performance.md)
- [Security guidelines](../security.md)
- [Dependency map](../nexus-plugin-dependency-map.md)

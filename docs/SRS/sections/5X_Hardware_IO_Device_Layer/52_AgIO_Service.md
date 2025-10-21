# 52 — AgIO Service (Status: collecting proposals)

## Problem statement
Define the responsibilities, lifecycle, and hot-swap behavior for AgIO as the hardware integration host, especially when split from the desktop UI or deployed on CM5/Linux headless rigs.【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L12-L116】【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L1-L44】

## Requirements (from contributors)
- R-AGIO-000 (MUST, process boundary): Support running AgIO as an independent service that exposes transports over gRPC/WebSocket while still embedding in-process for legacy WinForms builds.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L1-L44】
- R-AGIO-001 (MUST, driver lifecycle): Provide hot-pluggable driver model with discovery, start/stop, retry, and watchdog policies so USB/Serial/CAN devices can be swapped without rebooting Core.【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L28-L116】
- R-AGIO-002 (SHOULD, capability registry): Publish available transports, IO boards, and capabilities through the plugin registry so Core/UI can present availability and fallback options.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L120】
- R-AGIO-003 (MUST, safety gating): Enforce permission checks before forwarding control commands (autosteer, section toggles) and drop to manual override if communications degrade.【F:docs/SRS/sections/9X_Frontends_Ops/95_Security_Permissions.md†L24-L120】
- R-AGIO-004 (SHOULD, health reporting): Emit per-transport metrics (latency, packet loss, device faults) via telemetry and CLI status commands to support field diagnostics.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L126】
- R-AGIO-005 (MUST, simulation parity): Mirror hardware drivers with simulation shims so deterministic replay and CI lanes can exercise the same interfaces without physical devices.【F:docs/SRS/sections/9X_Frontends_Ops/96-O5%20-%20Replay-driven%20CI%20and%20rollout%20for%20layers.md†L7-L44】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L19-L74】

## Current sentiment
AgIO remains the canonical hardware host, but contributors want it packaged as a managed service with consistent telemetry and permission gating so it can power both desktop and headless deployments.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L1-L44】【F:docs/SRS/sections/5X_Hardware_IO_Device_Layer/51_Sensor_Actuator_Abstractions.md†L12-L116】

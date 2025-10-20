# 24 — Configuration & Environment (Status: collecting proposals)

## Problem statement
Specify how Nexus captures environment configuration—profiles, secrets, feature flags, per-device overrides—so deployments stay reproducible across desktops, CM5 controllers, and managed fleets.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L12-L128】【F:docs/SRS/sections/9X/94_Extensibility_Packaging_Updates.md†L17-L211】

## Requirements (from contributors)
- R-CONF-000 (MUST, configuration store): Store settings in versioned JSON/TOML profiles with schema validation and provenance metadata so changes can be audited and rolled back.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L28-L96】【F:docs/SRS/options/6X/O-API-5_VersionedLayerSchemas.md†L32-L49】
- R-CONF-001 (MUST, secrets management): Isolate API keys, RTK credentials, and signing secrets in encrypted stores (Windows DPAPI, Linux keyring, or Vault integrations) with runtime leases; configs should reference logical keys, not raw values.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L94-L126】
- R-CONF-002 (SHOULD, profile switching): Support operator-selectable profiles (field rig, kiosk, dev) that swap transports, plugin sets, and feature flags without editing config files manually.【F:docs/SRS/sections/9X/91_UI_Shell_Layout.md†L70-L126】【F:docs/SRS/sections/9X/94_Extensibility_Packaging_Updates.md†L17-L211】
- R-CONF-003 (MUST, feature flags): Provide centralized feature-flag definitions consumed by UI, Core, and plugins; flags must include rollout notes, default states, and telemetry hooks to measure adoption.【F:docs/SRS/sections/9X/96_Quality_Engineering_Release.md†L149-L211】
- R-CONF-004 (SHOULD, environment detection): Detect hardware capabilities (CM5 IO map, GPU presence, GNSS hardware) and auto-apply safe defaults while still allowing manual overrides for testing.【F:docs/SRS/sections/5X/51_Sensor_Actuator_Abstractions.md†L12-L116】
- R-CONF-005 (MUST, health integration): Surface configuration drift and missing secrets through telemetry dashboards and CLI health checks so operators can diagnose misconfiguration quickly.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L96-L146】【F:docs/SRS/options/6X/O-TELE-4_LayerDiagnostics.md†L1-L38】

## Current sentiment
Reliable configuration management is seen as a prerequisite for remote deployments and plugin governance; contributors prefer schema-validated JSON profiles with managed secrets over ad-hoc INI files to maintain parity across Windows and Linux hosts.【F:docs/SRS/sections/6X/64_Telemetry_Health.md†L28-L146】【F:docs/SRS/sections/9X/94_Extensibility_Packaging_Updates.md†L17-L211】

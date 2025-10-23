# Device Manager Plugin (GA)

## Overview

The Device Manager plugin provides a consolidated inventory of Nexus-connected hardware, firmware lineage, and live health signals so operators can quickly assess readiness before heading to the field. It coordinates with AgIO discovery services to enumerate adapters, aggregates telemetry faults, and routes firmware campaigns published through the update service described in ADR-031.

## Core responsibilities

- Maintain an authoritative list of connected controllers, sensors, and bridges with firmware, serial, and transport metadata for each device.
- Surface real-time health telemetry (faults, warnings, lifecycle events) and persist recent history to support predictive maintenance workflows outlined in the Equipment Health concept note.【F:docs/Plugins/briefs/EquipmentHealth.md†L1-L160】
- Broker firmware rollouts by mapping device identities to curated update feeds so operators can apply patches directly from the Nexus UI.

## Simulation providers

Two deterministic simulation providers ship with the GA manifest to help validate integrations without physical hardware:

- `device-manager.inventory` publishes repeatable inventory snapshots derived from simulated AgIO discovery events so lease arbitration and UI elements can be smoke-tested end to end.
- `device-manager.health` generates controllable warning and fault profiles, enabling regression tests for dashboards and notification flows that depend on device health state.

## Leases and dependencies

The manifest declares exclusive leases for inventory and firmware management capabilities alongside a shared lease for health telemetry so complementary plugins can subscribe without contention. It requires Core runtime services for registry access, AgIO runtime bindings for hardware discovery, and recommends pairing with the UI Shell to surface the Devices panel badges enumerated in the dependency matrix.【F:docs/Plugins/nexus-plugin-dependency-map.md†L934-L969】

## Operator experience

When installed, Device Manager adds a hardware-focused navigation entry and dedicated panel that summarize fleet status, highlight incompatible firmware, and link directly to remediation workflows. Job Tasks integration is suggested so equipment readiness can be displayed alongside job context, mirroring the guidance called out in ADR-031 for official plugin bundles.【F:docs/development/SRS/sections/9X_Frontends_Ops/94-ADR-031 - Official Plugin Bundle Dependency Governance.md†L16-L45】

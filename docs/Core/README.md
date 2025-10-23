# Core Runtime Overview

This directory now houses Core runbooks and operational guides. The
authoritative requirements, data contracts, and determinism budgets live
in the SRS — start with the [Core data flow reference](../development/SRS/references/core/data-flow.md)
and [Section 21 — System Decomposition & Boundaries](../development/SRS/sections/2X_System_Architecture/21_System_Decomposition_Boundaries.md).
Use these pages for procedures; consult the SRS for normative details.

## Directory map

- [`guidance/`](guidance/) — delivery briefs and orchestration playbooks tied to ADR-004 and related SRS slices.
- [`howto/`](howto/) — operational runbooks and provisioning guides referenced by development QA checklists.
- [`reference/`](reference/) — parity studies, migration notes, and report catalogs aligned with the Core SRS.
- [`support/`](support/) — customer-facing escalation paths and retention policies coordinated with release operations.

The Core runtime orchestrates deterministic scheduling, capability
exchange, and orchestration services that connect plugins, UI shells, and
AgIO transports. It enforces contracts, routes telemetry, and manages the
lifecycle of simulation or hardware-backed workloads.

```mermaid
flowchart TD
  Plugins[Plugin Hosts] -->|Capabilities + events| Core
  UI[UI Shells] -->|Commands + telemetry| Core
  Core -->|Lease orchestration| AgIO
  Core -->|Data products| Storage[(Data Stores)]
  Sim[Simulation Fabric] -->|Fixed-step clock| Core
```

## Responsibilities
- Host the gRPC services, event bus, and deterministic schedulers that
  power navigation, automation, and telemetry flows.
- Broker capability negotiation between plugins, UI clients, and AgIO to
  ensure the correct leases and topics are active for each deployment.
- Provide operational guardrails, including observability dashboards and
  multi-machine coordination workflows.

## Feature Highlights
- **Data flow contracts:** The [Core data flow overview](../development/SRS/references/core/data-flow.md)
  summarizes how pose, control, and analytics streams move between
  services.
- **Capability registry:** [Capability registry reference](../development/SRS/references/core/capability-registry.md)
  captures the identifiers shared across Core, AgIO, and plugins.
- **Operations playbooks:** [Linux Core operations](linux-core-operations-playbook.md)
  and [performance budget dashboards](performance-budget-telemetry-dashboards.md)
  outline deployment guardrails and observability expectations.
- **Fleet coordination:** The [multi-machine sync guide](multi-machine-sync.md)
  documents how Core instances coordinate state across head units.

## Plugin Touchpoints
- [Core Lifecycle plugin](../Plugins/CoreLifecycle.md) manages host
  startup, shutdown, and rolling upgrades across clusters.
- [Telemetry Logging](../Plugins/TelemetryLogging.md) and
  [Automation Engine](../Plugins/AutomationEngine.md) plugins rely on
  Core’s event bus and scheduling primitives.
- Guidance and control plugins such as
  [Autosteer](../Plugins/Guidance.md) and
  [Rate Control](../Plugins/RateControl.md) consume capability and
  telemetry feeds negotiated through Core.

## Additional References
- [Core data flow](../development/SRS/references/core/data-flow.md)
- [Capability registry](../development/SRS/references/core/capability-registry.md)
- [Linux operations playbook](linux-core-operations-playbook.md)
- [Multi-machine synchronization](multi-machine-sync.md)
- [Performance budget dashboards](performance-budget-telemetry-dashboards.md)

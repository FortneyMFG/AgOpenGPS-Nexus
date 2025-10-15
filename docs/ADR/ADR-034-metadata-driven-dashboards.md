# ADR-034: Metadata-driven dashboards and inspector surfaces

## Status
Drafting (target review window: 2025-11-28 week)

## Context
UI overlays, dashboards, and inspectors need to consume layer metadata without hard-coded IDs so Nexus can adapt to new layers and plugins. Current implementations tightly couple widgets to specific datasets, impeding reuse across desktop and companion clients. ADR-034 describes the metadata-driven UI model that leverages ADR-010 layer registry and ADR-032 layer controllers to deliver declarative visualization.

## Decision
- Bind UI widgets to layer definitions and controller metadata rather than fixed identifiers, enabling declarative dashboard composition.
- Provide preset catalogs and layout persistence tied to metadata, allowing operators to save and reuse configurations across devices.
- Implement inspector and tooltip components that render schema-driven data while respecting performance budgets for rich overlays.
- Ensure remote/headless modes reuse the same metadata-driven components, sharing contracts with companion clients and telemetry tooling.

## Consequences
- UI teams can extend dashboards quickly by adding metadata rather than hard-coded logic, increasing flexibility.
- Declarative layouts require rigorous metadata validation and performance tuning to maintain responsiveness on target GPUs.
- Companion and remote clients must support the same metadata contracts, increasing coordination but improving consistency.

## Validation
- UI automation must cover at least 30 metadata-driven widgets with > 90% branch coverage in Avalonia tests.
- Replay benchmarks must render 48-row rigs at ≥ 45 FPS average with ≤ 5 dropped frames per minute on the reference MX450 GPU.
- Remote companion mode must pass contract conformance tests validating schema parity and field-level ACLs.

## References
- [Frontend requirements](../SRS/sections/05_Frontends.md)
- [Telemetry & health requirements](../SRS/sections/10_Telemetry_Health.md)
- [Extensibility & plugin requirements](../SRS/sections/12_Extensibility_Plugins.md)
- [ADR-010: Layer registry and variable-rate framework](ADR-010-layer-registry-variable-rate.md)
- [ADR-032: Layer controllers and aggregation runtime](ADR-032-presets-and-layout-linking.md)

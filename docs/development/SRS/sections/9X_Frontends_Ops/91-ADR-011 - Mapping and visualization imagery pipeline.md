# ADR-011: Mapping and visualization imagery pipeline

## Status
Drafting (target review window: 2025-11-12 week)

**Relevant Plugin(s):** Mapping, UI Shell (Avalonia/Web), Telemetry Logging



## Context
Nexus must render PoseStream-derived ribbons, heatmaps, and telemetry overlays with deterministic performance across desktop and companion clients. Current UI code mixes basemap handling, interpolation rules, and attribution requirements across components, making caching and offline workflows fragile. ADR-011 consolidates mapping and imagery decisions so the UI owner can deliver consistent visualization while coordinating with ADR-029 mapping plugins, ADR-010 layer registry metadata, and ADR-034 dashboard refactors.

## Decision
- Refactor the renderer to support bilinear sampling, near-vehicle supersampling, and shared color-ramp utilities informed by the layer registry.
- Implement basemap caching (disk LRU plus offline fallbacks) with explicit attribution overlay requirements.
- Define ribbon, contour, and legend rendering order and interpolation policies to ensure deterministic output across devices.
- Share metadata contracts with dashboard components so overlays and inspectors consume the same layer-aware APIs.

## Consequences
- Visualization features gain predictable performance budgets but require coordinated caching and GPU strategy updates.
- Offline use cases improve through basemap cache and attribution handling but introduce storage management responsibilities.
- Rendering pipeline refactors may necessitate additional automated screenshot diffs and regression fixtures.

## Governance Updates
- **Rendering baselines.** Golden screenshot packs accompany each imagery change and include GPU telemetry (frame time, VRAM, shader stats). Deviations >5% in runtime budgets or cache hit rates raise blocking alerts.
- **Telemetry collectors.** Replay CI ingests GPU counters from supported hardware and posts dashboards correlating imagery changes with performance impacts. Any regression triggers a mandatory review by the imagery working group.
- **Cache policy change control.** Modifying cache eviction or prefetch rules requires updating the baseline documentation and executing replay benchmarks with hot/cold cache scenarios.

## Validation
- Basemap cache manager must maintain ≥ 92% hit rate during offline replay while respecting ≤ 3 GB disk footprint.
- Imagery pipeline must sustain ≥ 55 FPS for ribbon/heatmap workloads on the reference GPU with ≤ 80% GPU utilization.
- Attribution overlay must pass automated screenshot diffs across five basemap providers to ensure licensing accuracy.

## References
- [Telemetry & health requirements](../6X_Core_Domain_Services/64_Telemetry_Health.md)
- [Frontend requirements](../9X_Frontends_Ops/91_UI_Shell_Layout.md)
- [ADR-029: Mapping plugin architecture](ADR-029-mapping-plugin-architecture.md)
- [ADR-034: Metadata-driven dashboards and inspector surfaces](ADR-034-metadata-driven-dashboards.md)

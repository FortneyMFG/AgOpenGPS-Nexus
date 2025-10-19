# Genetics Plugin

## Overview
The Genetics plugin manages variety, hybrid, and trait metadata. It fuses planting data with observed field performance to help agronomists select genetics tailored to their fields and management zones.

## Capabilities
- `analytics.genetics` shared capability for producing trait performance analytics.
- `layers.genetics` shared capability to render genetics-themed overlays on the map.
- `reporting.genetics` for contributing to agronomy reports.

## Core Integration
- `GeneticsLayerIngestPipeline.cs` ingests planting layers, trait datasets, and performance metrics, producing `GeneticsLayerFeatures`.
- `GeneticsAnalyticsSnapshot.cs` encapsulates key KPIs per variety/hybrid.
- `GeneticsExportPipeline.cs` packages data for downstream tools or advisors.
- Configuration is managed through `GeneticsLayerIngestOptions.cs`.
- Ensure regression tests cover ingest edge cases, especially when multiple varieties share the same zone.

## UI Integration
- Powers the genetics picker view-model (`Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/GeneticsPickerViewModel.cs`).
- Registers layer toggles, dashboards, and report sections through UI registries when packaged as a zip.
- Provides warning indicators if a hybrid underperforms relative to targets.

## Dependencies
- Relies on Crop and Mapping data for context and spatial positioning.
- Integrates with Variable Mapping to suggest rate adjustments per genetics.

## Packaging Notes
- Bundle reference catalogs (traits, seed guides) under `assets/catalog/` to support offline usage.
- Manifest should note optional dependencies on Crop and Variable Mapping for richer analytics.

## Related Resources
- Existing agronomy briefs under `docs/plugins/Genetics.md`.
- `docs/plugins/official/VariableMapping.md` for how genetics drive prescription changes.
- ADRs around data governance detail how sensitive genetics data is secured.


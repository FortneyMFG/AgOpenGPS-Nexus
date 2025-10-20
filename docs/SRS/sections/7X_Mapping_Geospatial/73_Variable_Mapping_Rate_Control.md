# 73 — Variable Mapping & Variable Rate Control (Status: drafting)

## Problem statement
Define how variable-rate prescriptions, live agronomy layers, and closed-loop rate controllers integrate with Core mapping and automation services without fragmenting existing section control workflows.【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L86-L156】【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L1-L46】

## Requirements (from contributors)
- R-VR-000 (MUST, layer metadata): Describe variable-rate layers with schema metadata (units, valid ranges, controller hints) so Core and plugins can normalize inputs/outputs across products.【F:docs/SRS/options/7X/O-DATA-5_MetadataDrivenLayers.md†L1-L64】【F:docs/SRS/sections/7X_Mapping_Geospatial/71_Mapping_Kernel_Contracts.md†L52-L132】
- R-VR-001 (MUST, controller architecture): Use per-section controllers that ingest agronomy layers, implement smoothing/lag compensation, and expose quality metrics before commanding hardware.【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L19-L46】【F:docs/SRS/sections/6X_Core_Domain_Services/61_Kinematics_Pose_Fusion.md†L86-L156】
- R-VR-002 (SHOULD, closed-loop telemetry): Record commanded vs. actual rate, error bands, and calibration adjustments in telemetry for audit and tuning.【F:docs/SRS/sections/6X_Core_Domain_Services/64_Telemetry_Health.md†L28-L126】【F:docs/SRS/options/6X/O-TELE-4_LayerDiagnostics.md†L1-L38】
- R-VR-003 (MUST, safety constraints): Respect headlands, keep-outs, and manual overrides when computing variable-rate setpoints; controllers must degrade to manual rates if automation confidence drops.【F:docs/SRS/sections/8X_Guidance/81_Guidance_Orchestrator.md†L94-L180】【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L120-L188】
- R-VR-004 (SHOULD, prescription import/export): Support ISOXML, Shapefile, and GeoJSON prescription formats with provenance metadata and conflict resolution when merging with live layers.【F:docs/SRS/options/7X/O-DATA-5_MetadataDrivenLayers.md†L32-L64】【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L94-L156】
- R-VR-005 (MUST, plug-in contracts): Expose SDK hooks so agronomy plugins can register algorithms (rate calculators, analytics) that operate on shared caches without bypassing Core safety checks.【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L17-L120】

## Current sentiment
A metadata-driven controller pipeline (O-BACKEND-4) paired with strong telemetry is viewed as the path forward; teams want to prove closed-loop reliability before promoting advanced prescriptions to production.【F:docs/SRS/options/6X/O-BACKEND-4_LayerControllers.md†L19-L46】【F:docs/SRS/options/6X/O-TELE-4_LayerDiagnostics.md†L1-L38】

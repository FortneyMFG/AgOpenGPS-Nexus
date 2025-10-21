# 75 — Tiling & Rendering Services (Status: drafting)

## Problem statement
Describe the services that manage tile storage, GPU uploads, and rendering pipelines so mapping layers remain performant across desktop and headless deployments while honoring resource budgets for Raspberry Pi/CM5 hardware.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L12-L156】【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L40-L128】

## Requirements (from contributors)
- R-TILE-000 (MUST, tile store): Maintain a vector/raster tile store with caching policies, compression, and eviction tuned for constrained storage; tiles must include provenance and schema version.【F:docs/SRS/sections/3X_Data_Storage/32_Persistence_Formats.md†L12-L156】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L92-L116】
- R-TILE-001 (MUST, render pipeline): Separate CPU update, GPU upload, and draw phases with explicit budgets so plugins cannot starve the renderer; provide instrumentation for frame timing.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L70-L126】
- R-TILE-002 (SHOULD, multi-resolution): Support multi-resolution tiles (LOD) for large farms and aggregated analytics, including stitching adjacent fields without seams.【F:docs/SRS/sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md†L12-L120】
- R-TILE-003 (MUST, headless rendering): Offer server-side rasterization/export paths (PNG/GeoTIFF) driven by the same rendering pipeline for report generation and offline sync dashboards.【F:docs/SRS/sections/2X_System_Architecture/21-O6%20-%20Linux%20Core%20service%20with%20remote%20frontends.md†L6-L44】【F:docs/SRS/sections/9X_Frontends_Ops/94_Extensibility_Packaging_Updates.md†L59-L120】
- R-TILE-004 (SHOULD, GPU constraints): Validate pipelines on Windows, Linux desktop, and Raspberry Pi/CM5 GPUs; avoid features beyond OpenGL ES3 and provide fallbacks for software rendering.【F:docs/SRS/sections/1X_Platform_Foundations/11_OS_Support.md†L14-L45】
- R-TILE-005 (MUST, deterministic replay): Ensure tile updates replay deterministically from journals for CI/regression use cases.【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L92-L100】

## Current sentiment
A shared tiling pipeline with deterministic replay is required before rolling out metadata-driven dashboards or headless map services; performance validation on constrained hardware remains the gating factor.【F:docs/SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md†L70-L126】【F:docs/SRS/sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md†L92-L116】

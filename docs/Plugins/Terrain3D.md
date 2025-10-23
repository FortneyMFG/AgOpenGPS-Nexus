# 3D Terrain & Drainage Planner Plugin (Planned)

The Terrain3D plugin helps operators evaluate elevation, water flow, and drainage scenarios using Nexus telemetry and public DEM sources.

## Planned capabilities

- LiDAR/RTK elevation ingest with normalization to Layer Registry entries plus reprojection to the field CRS.
- Generation of derived surfaces: slope, aspect, curvature, flow accumulation, depression fill, and wetness indices.
- Interactive 3D visualization with contour lines, hillshade, and adjustable exaggeration for desktop and companion clients.
- Drain tile planning tools with snapping, pipe sizing calculators, and outlet validation.
- Export pipelines for contractors (GeoJSON, shapefile, PDF maps) and integration with guidance paths for contour planting.

## Data & integration

- Elevation rasters stored as `terrain.elevation` tiles with metadata capturing source (RTK log, DEM), resolution, and processing steps.
- Drainage plans stored as vector layers (`drain.tilePlan`, `drain.outlet`) with installation status, pipe properties, and provenance linking to work orders and sessions.
- Hooks into the Automation Engine for erosion alerts or auto-flagging drainage review when yield/terrain conditions match configured rules.
- Shares recommendations with the AI Agronomic Advisor so agronomic prescriptions can account for drainage constraints.

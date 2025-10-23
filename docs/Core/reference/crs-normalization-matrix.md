# CRS Normalization Matrix

> **Scope.** This matrix operationalizes the coordinate-reference-system (CRS) policy
> captured in [ADR-022](../../development/SRS/sections/3X_Data_Storage/32-ADR-022 - CRS units and precision policy.md). It applies to every
> ingest, storage, and replay path that carries spatial data inside Nexus.

## Why publish a matrix?

Multiple pods are implementing import pipelines, PoseStream fusion, and mapping plugins
in parallel. Without a shared normalization table, each team risks picking different
projections, precision tiers, or audit metadata. The matrix below provides:

- Canonical storage and processing frames per artifact type.
- Distortion thresholds and fallback rules for switching out of WGS84.
- Precision and unit expectations for regression checks.
- Audit fields that must be emitted whenever a reprojection occurs.

## Selection & distortion checks

1. **Detect the source CRS.** Prefer explicit EPSG codes. When absent, inspect GeoTIFF
   tags, ISOXML task metadata, or shapefile `.prj` contents. Default to EPSG:4326 only if
   the file is silent.
2. **Compute the field footprint.** Build a convex hull for incoming vertices or the
   raster bounds, then evaluate geodesic distances across the diagonals.
3. **Measure distortion.** Project the hull into WGS84 (EPSG:4326) and candidate local
   projected systems (UTM, State Plane). Reject any option whose scale distortion exceeds
   **1 cm over 640 acres** or **5 ppm**, whichever is tighter.
4. **Pick a working CRS.**
   - ≤ 320 acres and distortion within the 1 cm target → remain in EPSG:4326.
   - Otherwise prefer the single UTM zone that covers ≥ 95% of the hull. If the field
     straddles zones, split at the registry boundary or fall back to a State Plane or
     custom transverse Mercator profile defined in the farm registry.
5. **Publish the decision.** Store both the detected source CRS and the normalized target
   CRS in provenance metadata so replay and exports can reconstruct the transform chain.

## Normalization matrix

| Pipeline asset | Acceptable source CRS | Normalized storage CRS | Processing frame | Precision & units | Mandatory audit fields |
| --- | --- | --- | --- | --- | --- |
| **PoseStream (live + replay)** | EPSG:4326, EPSG:4979 (ellipsoidal heights), RTCM-derived local ENU frames | EPSG:4326 (lat/long) with optional EPSG:4979 height | Derived ENU tangent plane anchored at session origin (EPSG:4978 math) | Latitude/longitude stored as `float64` degrees, heights as `float64` meters | `sourceCrs`, `normalizedCrs`, `enuOrigin`, `distortionPpm`, `heightDatum`, `transformHash` |
| **Vector layers (boundaries, guidance, zones)** | EPSG:4326, UTM zones, State Plane, legacy NAD83 foot-based grids | EPSG:4326 for registry records; per-field projected CRS (UTM/State Plane) for tile caches | Same as storage; processors may materialize ENU slices for analytics | Geometry vertices stored as `float64` meters in projected CRS, attributes remain SI units | `sourceCrs`, `normalizedCrs`, `areaSqmBefore/After`, `perimeterDelta`, `reprojectToolVersion`, `precisionTier` |
| **Raster layers (GeoTIFF/COG)** | EPSG:4326, EPSG:3857, local UTM/State Plane, raw image grids | Field CRS selected via distortion check (typically UTM) | Raster kernels operate in the same projected CRS; down-stream analytics may request ENU tiles | Pixel centers stored as projected meters; raster values keep registry units; geotransform coefficients rounded to 9 decimal places | `sourceCrs`, `normalizedCrs`, `pixelSizeBefore/After`, `resampleKernel`, `nodataPropagation`, `distortionPpm` |
| **TileStore persistence** | Already-normalized GeoJSON/COG tiles, Section masks (legacy) | Matches layer definition CRS; fallback to EPSG:4326 when registry lacks override | Tile engines expose ENU slices per tile for deterministic replay | Tile bounds stored as `float64` meters; tile payload hashed for provenance | `layerId`, `tileId`, `normalizedCrs`, `tileResolution`, `tileHash`, `precisionTier` |
| **Interop imports (ISOXML, Shapefile, CSV)** | ISOXML declared CRS, `.prj` contents, or lat/long columns | First normalize to WGS84, then apply selection rules above | Import wizard stages data in EPSG:4326 before projecting to field CRS | WGS84 staging uses `float64` degrees; final output matches layer registry | `sourceCrs`, `stagingCrs`, `normalizedCrs`, `conversionSequence`, `unitConversions`, `qualityGateResult` |
| **Telemetry & audit logs** | Inherits CRS from producing pipeline | Mirror the producer’s normalized CRS; no implicit reprojection | Analytics replay into ENU using stored transform metadata | Coordinates recorded as `float64`; summary aggregates round to 1 mm in projected CRS | `sourceCrs`, `normalizedCrs`, `distortionPpm`, `auditEventId`, `producerId`, `timestampUtc` |

## Precision tiers

- **Tier A (pose-critical):** `float64` spatial coordinates, ellipsoidal heights, and
  quaternions. Applies to PoseStream snapshots, control arbitration, and deterministic
  replay fixtures.
- **Tier B (mapping tiles):** `float64` projected coordinates and `float32` attribute
  payloads. Raster values may use `float32`, `int16`, or `uint16` depending on layer
  registry guidance, but geotransforms stay `float64`.
- **Tier C (summary telemetry):** `float32` spatial bins with metric units. Aggregations
  must round to at least 1 mm (projected) or 1e-7 degrees (geodetic) to remain reversible.

## Required provenance payload

Every normalization operation must emit a provenance record that includes:

- **`sourceCrs` and `normalizedCrs`** EPSG codes.
- **Transform parameters** (Helmert or grid shift identifiers) when non-trivial.
- **Distortion metrics:** average and maximum parts-per-million over the field hull.
- **Precision tier** consumed from the layer registry.
- **Conversion chain hash** so replay tooling can detect drift across library updates.
- **Operator or automation ID** responsible for the conversion when manually triggered.

## Validation checklist

- Regression fixtures covering at least **50 representative fields** must verify that the
  normalized output deviates by **≤ 1 cm** compared to the authoritative source.
- Raster imports must prove that cell-center geolocation error stays within **0.2%** of
  the original pixel size and that resampling preserves mean and standard deviation within
  **0.2%**.
- PoseStream reprojection must demonstrate **≤ 3 cm positional drift** after round-tripping
  through ENU, storage, and replay stages.
- Audit log consumers must fail CI if any normalization event omits `sourceCrs` or reports
  distortion above the published threshold without an accompanying override justification.

## Example configuration snippet

```jsonc
{
  "crs": {
    "default": "EPSG:4326",
    "maxDistortionPpm": 5,
    "fieldOverrides": {
      "field-8c1d": "EPSG:32614",
      "field-741a": {
        "crs": "EPSG:32137",
        "precisionTier": "TierB"
      }
    },
    "audit": {
      "enabled": true,
      "sink": "TelemetryProvenance",
      "fields": [
        "sourceCrs",
        "normalizedCrs",
        "distortionPpm",
        "transformHash"
      ]
    }
  }
}
```

This profile aligns the runtime configuration with ADR-022 while giving operations a
single reference for normalization behavior.

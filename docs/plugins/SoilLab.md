# Soil & Lab Manager Plugin (Planned)

The Soil & Lab Manager plugin captures the soil baseline needed by agronomy analytics, prescriptions, and compliance reports.

## Planned capabilities

- Soil sampling workflows with zone selection, sampling routes, chain-of-custody labels, and submission tracking tied to jobs/sessions.
- Lab result ingestion (CSV, PDF parsing, shapefile/grid uploads) with normalization to Layer Registry entries (e.g., soil organic matter, pH, nutrient levels) and provenance metadata (lab name, analyst, batch ID, sample depth).
- Batch tracking and lot traceability for regulatory reporting, including optional barcode scanning when receiving lab kits.
- Visualization overlays (`soil.*` layers) with temporal comparisons and per-field trends to identify variability hotspots.
- Recommendation engine hooks that feed Crop Reports, Profit analytics, and the AI Agronomic Advisor with calibrated baselines.

## Data contracts

- Defines `SoilSampleSubmission.v1` documents referencing `fieldId`, `zoneId`, sample points, chain-of-custody info, and shipping status.
- Normalizes lab results into `SoilAnalysis.v1` objects that map into `soil.*` raster layers with unit metadata, sample depth, and quality scores.
- Emits recommendation payloads (lime/fertilizer bands) that integrate with ADR-013 derivation flows and can be promoted into prescription layers after operator review.

## Integration notes

- Ties into TaskService so work orders can schedule sampling crews and mark submissions complete from mobile devices.
- Shares inventory ledger hooks for sample kits and lab fees, ensuring Profit analytics capture costs alongside agronomic insights.
- Provides report sections for regulatory exports (e.g., nutrient management plans) and Print Studio templates for landlord updates.

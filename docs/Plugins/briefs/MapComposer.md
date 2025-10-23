# Map Composer & Print Studio Plugin (Planned)

The Map Composer plugin delivers professional PDF map books and compliance packets sourced from Nexus layers.

## Planned capabilities

- Offline map layout tooling with template-driven PDF/GeoPackage exports. Templates capture page size, orientation, scale bars, legends, logos, and metadata fields.
- Integration with Report Builder templates for crop, profit, soil, and regulatory sections so operators can assemble full-season binders.
- High-resolution tile stitching, labeling, and annotation workflows for custom map books, including overview + inset layouts.
- Layer selection UI that enforces provenance (planned vs. actual) and color ramp standards defined by contributing plugins.
- Batch export pipelines for landlord packets, regulatory submissions, and contractor packets, including automated naming conventions.

## UX considerations

- "Print View" mode mirrors the active map but overlays print extents, scale bars, north arrow, and legend preview. Operators can adjust extent, scale, and layer visibility before export.
- Template gallery surfaces first-party templates (Landlord, Compliance, Profit Summary) and allows organizations to store custom templates under version control.
- Exports provide PDF (vector overlays + raster tiles) and optional GeoPackage/GeoTIFF artifacts for GIS partners.

## Integration

- Powered by Report Builder (ADR-051) for section assembly and dependency tracking.
- Consumes soil, yield, profit, risk, and advisor recommendation layers to populate legends and callouts.
- Hooks into regulatory exports so compliance packets embed signed logs alongside maps.

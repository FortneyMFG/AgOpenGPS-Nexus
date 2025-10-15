# Field Health Plugin Ingestion Pipeline

The field health plugin tracks agronomic risk observations (flooding, compaction,
weed pressure, etc.) that operators draw with the zone editing tools. The
`FieldHealthIngestPipeline` normalizes these observations, deduplicates updates,
and produces layer metadata that satisfies the `FieldHealthRiskLayer.v1` schema
from ADR-052.

Key responsibilities:

- Validate incoming `FieldHealthObservation` payloads and enforce schema
  patterns before storing them.
- Maintain a canonical observation list keyed by feature identifier so the most
  recent scouting details win.
- Derive roll-up statistics (total affected hectares, severity counts, last
  surveyed timestamp) that populate the schema's `metadata.stats` block.
- Allow plugins to update layer level context (notes, tags, schema reference)
  while keeping published metadata snapshots deterministic.

Use the pipeline to ingest observations as they arrive from the UI, imports, or
mobile scouts. Whenever the pipeline state changes, request a metadata snapshot
and serialize it into the zone layer document for persistence or publishing.

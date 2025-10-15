# Legacy Data Ingest Notes

Tasks NX-054 through NX-058 land the first wave of tooling that bridges AgOpenGPS V6 field
artifacts into the Nexus runtime. This document summarises the new import pipeline,
validation harnesses, and auto-tuning helpers.

## Legacy Field Importer (NX-054)

- `LegacyFieldImporter` reads the V6 `TrackLines.txt`, `Boundary.txt`, and `Headland.txt`
  files from a field directory and converts them into strongly typed Nexus data models.
- The importer preserves AB track metadata (name, heading, nudge, curve points) and lifts
  boundary/headland vertices into planar geometry primitives so the UI wizard can preview
  imported geometry before persisting it.
- The implementation mirrors the tolerant readers used by V6 and accepts duplicate
  drive-through flags or optional headers to keep recovery workflows reliable.

## Coverage Analytics Parity Harness (NX-055)

- `CoverageAnalyticsParityHarness` compares legacy CSV exports with Nexus coverage
  analytics summaries and reports per-metric deltas.
- The harness powers regression tests and will back the CLI tooling that alerts when
  algorithm changes breach configured tolerances.
- Metrics are matched case-insensitively and the harness surfaces the absolute delta for
  dashboards or developer review.
- `CoverageExportVerifier` inspects zipped shapefile exports from V6 and Nexus,
  comparing polygon areas, centroid positions, and symmetric difference area to ensure
  geometry parity before the analytics CSVs are diffed.

## Machine Profile Translator (NX-056)

- `V6MachineProfileTranslator` converts the legacy `Properties.Settings.Default` snapshot
  into the new `MachineProfile` aggregate used by Nexus for hydraulics, implement geometry,
  and section layout.
- The translator normalises section offsets, hitch lengths, and hydraulic timings so both
  the CLI importer and the UI wizard can emit ready-to-use Nexus configuration payloads.

## Rate Control Parity Validation (NX-057)

- Section control scenarios captured from V6 are replayed against the Nexus
  `SectionMaskCalculator` to confirm parity across speed gating, look-ahead activation, and
  manual suppression logic.
- Scenario definitions live in `tests/Aog.Plugins.Tests/Sections/Data/LegacyRateControlScenarios.csv`
  and can be extended with new regression captures as coverage grows.

## Guidance Auto-Tuning (NX-058)

- `AutoSteerLiteTuningCalculator` derives an `AutoSteerLiteTuningProfile` from a translated
  `MachineProfile`, aligning look-ahead multipliers and startup ramps with the V6 heuristics.
- Wider implements bias the hold multiplier upward, while shorter wheelbases tighten the
  minimum look-ahead distance. The calculator validates its output before returning it so
  callers can rely on sane defaults.

## Legacy Metadata + History Import (NX-106–NX-111)

- `LegacyFieldImporter` now reads `Field.txt`, `Flags.txt`, `Contour.txt`, `RecPath.txt`,
  `Tram.txt`, and `Sections.txt` alongside geometry files so the importer surfaces operator
  metadata, scouting flags, contour resume buffers, recorded path logs, tram templates, and
  worked-area cells in a single `LegacyFieldData` bundle.
- New domain models (`LegacyFieldOverview`, `LegacyFlag`, `LegacyContourResume`,
  `LegacyRecordedPath`, `LegacyTramTemplate`, `LegacyWorkedAreaHistory`) encapsulate the
  imported assets, making it trivial for UI and service layers to reason about provenance and
  keep audit trails intact.
- The regression fixtures under `tests/Aog.Core.Tests/Legacy/Data/SampleField` now include
  representative `Field.txt`, `Flags.txt`, `Contour.txt`, `RecPath.txt`, `Tram.txt`, and
  `Sections.txt` exports so future importer changes have a parity harness to lean on.


# V6 Field Functionality Inventory & Gap Analysis

## Purpose

Task NX-102 requires a clear view of the legacy (V6) field artefacts that still need parity
work in Nexus. This note summarises the files and responsibilities bundled into a V6 field
directory and highlights which pieces are, and are not yet, represented in the current
Nexus import pipeline.

## Legacy Field Asset Inventory

V6 encapsulates a field as a collection of optional aspects (background imagery, boundary
geometry, flags, etc.) that are loaded into the `Field` aggregate and maintained through a
single `FieldStreamer` facade.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Models/Field/Field.cs†L7-L27】【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/FieldStreamer.cs†L7-L106】

| Asset | Stored Files | V6 Responsibilities |
| --- | --- | --- |
| Background imagery | `BackPic.txt`, `BackPic.png` | Persists Bing map bounding boxes plus PNG tiles so the OpenGL map can render satellite or hybrid imagery behind coverage layers.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/BingMapStreamer.cs†L11-L124】 |
| Field overview metadata | `Field.txt` | Records operator name, convergence, and the WGS84 origin captured when the field was created, enabling reprojection and audit workflows.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/OverviewStreamer.cs†L9-L75】 |
| Boundaries & headlands | `Boundary.txt`, `Headland.txt` | Stores exterior/drive-through polygons plus optional inner rings that drive headland lift cues and section suppression.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/BoundaryStreamer.cs†L9-L101】 |
| Contour coverage strips | `Contour.txt` | Captures recorded contour passes and the unsaved buffer so operators can resume recording guidance for sectional control.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/ContourStreamer.cs†L8-L62】 |
| Flags & annotations | `Flags.txt` | Keeps geo-referenced flag markers with colour, heading, ID, and notes for scouting or workflow reminders.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/FlagListStreamer.cs†L10-L135】 |
| Recorded path logs | `RecPath.txt` | Writes playback-ready vehicle traces (position, heading, speed, autosteer state) used for teaching or reference runs.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/RecordedPathStreamer.cs†L9-L90】 |
| Tram line templates | `Tram.txt` | Serialises inner/outer tram polygons plus individual paths for multi-pass tramline automation.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/TramLinesStreamer.cs†L9-L85】 |
| Worked area patches | `Sections.txt` | Streams triangle strips representing painted coverage and pending unsaved work so section control history survives restarts.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Streamers/Field/WorkAreaStreamer.cs†L8-L79】 |

## Nexus Coverage Snapshot

The Nexus importer currently focuses on a subset of the V6 assets:

- `LegacyFieldImporter` loads `TrackLines.txt`, `Boundary.txt`, and `Headland.txt`,
  returning a `LegacyFieldData` bundle that only includes AB/curve tracks plus boundary
  geometry.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldImporter.cs†L13-L161】
- The resulting `LegacyFieldData` exposes only `Tracks` and `Boundaries`, leaving no slots
  for imagery, contour strips, flags, or section history.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L128】
- Existing documentation for the import tooling reiterates the same limited scope and does
  not mention additional field-aspect readers yet.【F:docs/porting/LegacyDataIngest.md†L7-L15】

### Gap Analysis

| Asset | Nexus Status | Notes |
| --- | --- | --- |
| AB/curve tracks | ✅ Imported via `LegacyFieldImporter` and surfaced on `LegacyFieldData.Tracks`.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldImporter.cs†L33-L90】【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L21-L29】 |
| Boundaries & headlands | ✅ Imported via `LegacyFieldImporter`/`LegacyFieldData.Boundaries` and already referenced by UI importers.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldImporter.cs†L92-L200】【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L21-L29】 |
| Background imagery | ❌ Not represented in `LegacyFieldData`; no Nexus service persists `BackPic` artefacts yet.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L29】 |
| Field overview metadata | ❌ No importer output for `Field.txt`, so creator/origin data is unavailable for Nexus workflows.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L29】 |
| Flags & annotations | ❌ Flag collections are not parsed today; UI lacks legacy scouting markers until readers are added.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L29】 |
| Contour coverage strips | ❌ Contour files are not consumed, preventing resuming legacy contour control runs.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L29】 |
| Recorded path logs | ❌ `RecPath.txt` is ignored, so replay/teach-in data is absent in Nexus imports.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L29】 |
| Tram line templates | ❌ Tram automation artefacts are not yet mapped into Nexus structures.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L29】 |
| Worked area patches | ❌ Section history (`Sections.txt`) is not surfaced, limiting continuity metrics after migration.【F:Nexus SourceCode/src/Aog.Core/Legacy/LegacyFieldData.cs†L11-L29】 |

## Recommended Follow-Ups

1. **Extend `LegacyFieldImporter`** with optional readers for imagery, flags, tram lines,
   contours, and worked area so the importer mirrors the V6 `FieldStreamer` responsibilities.
2. **Design new domain models** (or reuse existing Nexus primitives) for imagery metadata,
   flag annotations, and section history to avoid bolting raw text files directly into the
   runtime.
3. **Update the UI migration wizard** to surface these additional assets, ensuring operators
   can preview and selectively import overlays, markers, and tram patterns.
4. **Add regression fixtures** using representative field directories that exercise each
   asset, continuing the parity approach used for NX-055/NX-057 coverage validation.

Capturing these follow-ups will close the biggest gaps between V6 field workflows and the
current Nexus experience.

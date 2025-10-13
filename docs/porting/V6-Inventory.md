# V6 Algorithm Inventory

This note captures the legacy (V6) math assets that need assessment during the Nexus port. It groups the major routines by domain and records their source, responsibilities, and key dependencies so that follow-up porting tasks can reference concrete entry points.

## Paths & Guidance

- **CABLine** — `Legacy SourceCode -V6/GPS/Classes/CABLine.cs`
  - Builds the active AB line state for the current track, applying tool width/overlap offsets and determining lane selection for the pivot axle.【F:Legacy SourceCode -V6/GPS/Classes/CABLine.cs†L63-L149】
  - Computes steering targets via Stanley or pure pursuit control, including integral and derivative terms for pivot error correction and Stanley distance smoothing.【F:Legacy SourceCode -V6/GPS/Classes/CABLine.cs†L152-L220】
- **CABCurve** — `Legacy SourceCode -V6/GPS/Classes/CABCurve.cs`
  - Locates the closest curve segment to the guidance look-ahead point, derives offsets for parallel passes, and lazily rebuilds offset polylines in the background when the tool lane changes.【F:Legacy SourceCode -V6/GPS/Classes/CABCurve.cs†L75-L214】
  - Supports guide-line generation for visualization and curve-following, reusing the same offset builder used for active guidance passes.【F:Legacy SourceCode -V6/GPS/Classes/CABCurve.cs†L198-L214】
- **CYouTurn** — `Legacy SourceCode -V6/GPS/Classes/CYouTurn.cs`
  - Selects turn templates (wide, omega, K-style) based on tool geometry and skip width, then generates Dubins-based point lists for AB-line reversals while respecting headland boundaries.【F:Legacy SourceCode -V6/GPS/Classes/CYouTurn.cs†L183-L347】
- **DubinsPath hierarchy** — `Legacy SourceCode -V6/AgOpenGPS.Core/Models/Guidance/DubinsPath.cs`
  - Encapsulates Dubins path primitives (outer, inner, curved) used by YouTurn, computing tangents and arc/straight segment lengths lazily from shared turn constraints.【F:Legacy SourceCode -V6/AgOpenGPS.Core/Models/Guidance/DubinsPath.cs†L8-L200】
- **CGuidance** — `Legacy SourceCode -V6/GPS/Classes/CGuidance.cs`
  - Implements Stanley steering corrections with heading and cross-track damping, integral wind-up protection, and slope compensation, feeding steering angles back to the vehicle model and section control indicators.【F:Legacy SourceCode -V6/GPS/Classes/CGuidance.cs†L40-L194】
  - Nexus port status: dynamic look-ahead scheduling and startup ramping now live in `AutoSteerLiteTuningProfile`/`AutoSteerLiteController` (see `AutoSteerLite-Tuning.md`).

## Coverage & Boundaries

- **CSection / CPatches** — `Legacy SourceCode -V6/GPS/Classes/CSection.cs`, `CPatches.cs`
  - Section state holds mapping bounds, timers, and positional metadata for each boom segment used by coverage control.【F:Legacy SourceCode -V6/GPS/Classes/CSection.cs†L9-L56】
  - Patch management streams triangle strips while sections are active, accumulating painted area and persisting patch runs for job summaries.【F:Legacy SourceCode -V6/GPS/Classes/CPatches.cs†L17-L110】
- **CFieldData** — `Legacy SourceCode -V6/GPS/Classes/CFieldData.cs`
  - Aggregates total/actual worked area, overlap, and distance metrics from coverage patches and boundary geometry, providing formatted summaries and remaining-work estimates.【F:Legacy SourceCode -V6/GPS/Classes/CFieldData.cs†L7-L159】
- **BoundaryBuilder & CBoundary** — `Legacy SourceCode -V6/GPS/Classes/BoundaryBuilder.cs`, `CBoundary.cs`, `CHead.cs`
  - BoundaryBuilder extends track polylines, computes self-intersections, and emits finalized polygon fences for field boundaries/headlands.【F:Legacy SourceCode -V6/GPS/Classes/BoundaryBuilder.cs†L11-L214】
  - CBoundary stores the resulting boundary lists and headland control switches, while `CHead` evaluates tool corners/look-ahead points against the polygons to drive hydraulic lift cues and audio alerts.【F:Legacy SourceCode -V6/GPS/Classes/CBoundary.cs†L6-L23】【F:Legacy SourceCode -V6/GPS/Classes/CHead.cs†L1-L108】【F:Legacy SourceCode -V6/GPS/Classes/CHead.cs†L109-L204】
  - See `LegacyDataIngest.md` for the Nexus importer that consumes `TrackLines.txt`, `Boundary.txt`, and `Headland.txt` to materialise these structures.
- **glm helpers** — `Legacy SourceCode -V6/GPS/Classes/CGLM.cs`
  - Provides shared computational geometry utilities (point-in-polygon, spline interpolation, unit conversions) used by coverage, boundary, and path modules.【F:Legacy SourceCode -V6/GPS/Classes/CGLM.cs†L10-L200】

## Filters, Sensor Fusion & Vehicle Dynamics

- **CAHRS** — `Legacy SourceCode -V6/GPS/Classes/CAHRS.cs`
  - Hosts IMU heading/roll state with configurable roll filters, fusion weights, and dual-antenna switching parameters loaded from user settings.【F:Legacy SourceCode -V6/GPS/Classes/CAHRS.cs†L3-L36】
- **CNMEA** — `Legacy SourceCode -V6/GPS/Classes/CNMEA.cs`
  - Maintains GNSS fix data and provides local-plane conversion plus low-pass speed averaging used by both real GPS and the simulator.【F:Legacy SourceCode -V6/GPS/Classes/CNMEA.cs†L9-L53】
- **CVehicle** — `Legacy SourceCode -V6/GPS/Classes/CVehicle.cs`
  - Centralizes vehicle control parameters (Stanley gains, look-ahead tuning, hydraulic lead distances) and exposes goal-point distance updates tied to current cross-track error and speed.【F:Legacy SourceCode -V6/GPS/Classes/CVehicle.cs†L11-L139】
- **CGuidance (see above)** — Supplies filtered steering commands and integral management that couple the sensors and vehicle model.【F:Legacy SourceCode -V6/GPS/Classes/CGuidance.cs†L40-L194】

## Simulation & Replay

- **CSim** — `Legacy SourceCode -V6/GPS/Classes/CSim.cs`
  - Advances the built-in simulator by filtering steering commands, integrating heading with step distance, projecting new WGS84 positions, and synthesizing IMU/GNSS observables before handing them back through the shared app model.【F:Legacy SourceCode -V6/GPS/Classes/CSim.cs†L7-L84】
  - Handles acceleration inputs, simulated speed smoothing, and deterministic pseudo-altitude for testing map overlays.【F:Legacy SourceCode -V6/GPS/Classes/CSim.cs†L52-L103】

## Next Steps

- Validate each module’s dependencies and configuration inputs when porting to .NET 8 to ensure settings survive the migration.
- Capture representative datasets (field boundaries, coverage patches, Dubins turn traces) to use as regression vectors alongside the upcoming ports (NX-051 through NX-053).

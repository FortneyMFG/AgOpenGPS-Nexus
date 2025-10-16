---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "2024-05-13"
---

# Line Families and Creation

AgOpenGPS v6 supports several guidance line families backed by the shared `CTrk` track store. Each family defines how points are captured, offset, and regenerated at run time.

## Track data model

- `CTrk` persists either straight or curved references: `ptA`/`ptB` for straight AB, `curvePts` for polylines, plus metadata such as `heading`, `mode`, visibility flags, and operator-applied `nudgeDistance` shifts.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTrack.cs†L12-L124】
- Active AB lines cache extended endpoints (`endPtA/endPtB`) and expose `currentLinePtA/B` for the working pass; curves maintain `curList` (offset polyline) and optional guideline bundles for side-display.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L13-L154】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L60-L154】

## Line family reference

| Family | Inputs & capture | Storage & refresh | Math & smoothing | Notes |
| --- | --- | --- | --- | --- |
| **AB (straight)** | Operator records A/B points (`track.ptA/ptB`). `BuildCurrentABLineList` recomputes extended endpoints and determines current pass index (`howManyPathsAway`). | `CTrk.ptA/ptB`, `heading`, `nudgeDistance`. Current pass stored in `CABLine.currentLinePtA/B` with headings attached.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L78-L154】 | Offset distance `distAway = (width-overlap) * path + offset ± nudge`; line shifted by rotating that distance perpendicular to heading. | Supports `snapDistance`, `nudge` controls, and overlap aware of `tool.overlap` and `tool.offset` (3‑pt hitches).【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L118-L150】 |
| **A+ (derived bearing)** | UI stores single point + heading; runtime still uses AB pipeline once `track.heading` and `ptA/ptB` are populated. | Same as AB; heading never recomputed from live A/B vector. | Identical to AB after initialisation. | No additional smoothing logic; inherits AB nudge/offset behaviour. |
| **AB Curve** | `track.curvePts` recorded while driving. `BuildCurveCurrentList` finds nearest reference points and spawns `curList` by offsetting each node by pass distance. | Curves reuse `CTrk.curvePts`; `curList` holds offset polyline; `guideArr` caches parallel guide lines if enabled.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L75-L206】 | Offset uses normal vector (`sin(heading+π/2) * distAway`) per node. After filtering duplicates, Catmull-Rom interpolation (`glm.Catmull`) densifies long segments.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L220-L360】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGLM.cs†L120-L150】 | Async recomputation throttled; cancellation tokens abort previous builds when the operator changes passes.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L132-L216】 |
| **Contour (follow logged swaths)** | `CContour.stripList` collects logged contour strips (`ctList`). Operators can lock to a strip (`isLocked`) and cycle priority sides. | `stripList` retains base strips; `ctList` holds the current contour path with headings per point. | Similar offset logic to curves; contour solver searches locally around the vehicle to avoid snapping to distant strips and supports manual lock toggling.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CContour.cs†L7-L338】 | Contour mode respects `isHeadingSameWay` and can flip headings when running opposite directions.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CContour.cs†L320-L399】 |
| **Recorded / Adaptive (shuttle, dubins)** | Recording lists (`recList`, `shuttleDubinsList`) capture pivot/tool states during manual driving. Playback reuses pure pursuit to chase the recorded polyline.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CRecordedPath.cs†L430-L520】 | Lists persist full poses; runtime keeps indices (`A/B`) to find nearest segment and goal points. | Pure pursuit identical to AB/curve version; integrates optional integral term and clamps steering to `maxSteerAngle`. | Dubins helper generates turn-in geometries when reacquiring a recorded swath.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CRecordedPath.cs†L462-L520】 |
| **Headland passes** | Headland tool builds offset passes from boundary polygons (`hdLine`) and stores sequences in `CHeadLine.tracksArr`. | `CHeadLine` contains `tracksArr` of `CHeadPath.trackPts` (polylines) with `moveDistance` metadata. | Generation leverages boundary offsets (see headland chapter) to create concentric rings; runtime treats them as curve tracks. | Headland passes feed the same curve guidance stack once selected.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHeadLine.cs†L6-L35】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTrack.cs†L78-L126】 |

## Point capture & re-capture behaviours

- **A/B recapture** – Pressing record overwrites `CTrk.ptA/ptB`. `BuildCurrentABLineList` checks every ~0.66 s when the machine is out of autosteer or the track is invalid, ensuring the working pass resets when headings drift or nudges change.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L78-L154】
- **Curve re-lock** – `BuildCurveCurrentList` toggles between global nearest search (when first engaging) and local neighbourhood search (`findNearestLocalCurvePoint`) to prevent snapping to adjacent strips once locked.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L619-L706】
- **Contour lock** – Operators can lock the active contour strip; the toggle simply flips `isLocked`, preventing automatic reselection until released.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CContour.cs†L58-L68】
- **Headland offsets** – Headland track generation encodes `moveDistance` per ring so that selected passes can be shifted in or out without recomputing the boundary geometry.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHeadLine.cs†L11-L35】

## Research Notes

- Code pointers: `CTrack.cs`, `CABLine.cs`, `CABCurve.cs`, `CContour.cs`, `CRecordedPath.cs`, `CHeadLine.cs`, `CGLM.cs`.
- Open questions: UI workflows for A+, headland creation wizards, and tramline templating sit in WinForms code not covered here.
- Constants: default Catmull spacing clamps to 1–4 m (`step` in `BuildNewOffsetList`); pass spacing uses `tool.width - tool.overlap` across all families.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L220-L268】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L130-L150】
- Dev gap: Dev branch artefacts (e.g., adaptive improvements) were inaccessible.

---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "2024-05-13"
---

# Line Selection and Reacquisition

AgOpenGPS v6 keeps the “current driving line” in sync with vehicle position through a blend of auto-tracking, proximity searches, and operator overrides.

## Auto-track entry points

- When auto-track is enabled and autosteer is off, the system periodically re-evaluates the closest reference track around the steer axle, invalidating cached AB/curve state if the index changes.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L862-L890】
- `CTrack.FindClosestRefTrack` filters visible tracks, requires similar heading (within ≈±57°), and chooses the minimum squared distance either to the infinite AB line or to individual curve points.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTrack.cs†L32-L126】

## Pass selection within a track

- **AB lines** – `BuildCurrentABLineList` projects the guidance look-ahead position onto the stored reference line, applies overlap/offset corrections, and derives the integer pass number `howManyPathsAway`. Heading alignment is checked every 0.66 s so reversing direction flips the pass orientation automatically.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L78-L154】
- **Curves** – `BuildCurveCurrentList` finds the closest reference samples by first scanning coarse indices, then examining a small neighbourhood. The algorithm switches between “global” search (when not locked) and “local” search around the previous index to prevent jumps to distant strips.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L75-L154】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L619-L706】
- **Contours** – Contour mode stores the current strip index and allows manual lock/unlock. When unlocked it mirrors the curve logic to pick the nearest strip based on priority (right/left).【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CContour.cs†L7-L161】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CContour.cs†L258-L338】

## Reacquisition and loss handling

- Straight and curved solvers guard against degenerate geometry—zero-length segments force an early return rather than emitting stale steering targets.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L185-L194】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L316-L379】
- When no valid point is found (e.g., curve list empty), the guidance distance is set to 32000 mm (special “invalid” sentinel) so the autosteer module can disengage gracefully.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L398-L407】
- Recorded-path playback uses the same nearest-segment search as curves and will stop autosteer if the goal point falls beyond the recorded extent (e.g., past the end of a curve swath).【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CRecordedPath.cs†L430-L459】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L820-L843】

## Operator controls

- **Nudge controls** – `CTrack.NudgeTrack` and `NudgeRefTrack` let operators shift either the working pass or the reference spine. Shifts respect travel direction (sign flip when running opposite heading) and mark the line/curve invalid so it is rebuilt on the next loop.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTrack.cs†L129-L190】
- **Snap-to-pivot** – `SnapToPivot` applies the measured pivot cross-track error as an instantaneous nudge, effectively snapping the pass centreline back under the vehicle.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTrack.cs†L167-L173】
- **Contour lock** – Tapping the contour lock toggles `isLocked`, freezing the current contour strip even if a closer strip appears.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CContour.cs†L58-L68】

## Headland and boundary influence

- Track selection itself ignores headland priority; headland passes are stored as separate curve-like tracks that can be selected manually. Boundary influence instead manifests in section control (preventing sections from turning on outside the fence) and hydraulics (headland raise/lower), described in later chapters.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHeadLine.cs†L6-L35】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L880-L1108】

## Research Notes

- Code pointers: `Position.designer.cs`, `CTrack.cs`, `CABLine.cs`, `CABCurve.cs`, `CContour.cs`, `CRecordedPath.cs`.
- Open questions: UI level cues for auto-track state changes; whether Dev adds hysteresis or UI prompts when snap events occur.
- Constants: the auto-track proximity filter uses 2000 m line extensions for distance projection; search windows around the closest curve point span ±7–12 samples depending on travel direction.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTrack.cs†L83-L122】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABCurve.cs†L90-L206】

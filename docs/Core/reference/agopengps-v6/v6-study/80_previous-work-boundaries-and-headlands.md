---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "-"
---

# Previous Work, Boundaries, and Headlands

AgOpenGPS v6 maintains several spatial layers—coverage “patches,” field boundaries, and headland rings—that influence section control, hydraulics, and YouTurn automation.

## Previous work (coverage) storage

- Each section owns a `CPatches` instance. When a section turns on, `TurnMappingOn` seeds a new triangle strip whose first vertices align with the current section edges. Every fix, `AddMappingPoint` appends two more vertices (left/right edge) and accumulates area.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CPatches.cs†L8-L161】
- Triangle strips are batched into `patchList` for rendering and persisted via `patchSaveList` when the job is saved. Coverage area totals (`workedAreaTotal`) update while mapping is active.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CPatches.cs†L48-L161】
- `triStrip` (list of `CPatches`) is attached to the main form and rendered in OpenGL so the section controller can sample coverage pixels for automatic control.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/FormGPS.cs†L130-L138】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L923-L1108】

## Boundary management

- Boundaries are stored as `CBoundaryList` entries, each containing outer `fenceLine` and optional `hdLine`/`turnLine` polygons. The boundary manager (`CBoundary`) maintains the list and exposes helpers such as `IsPointInsideFenceArea` and `IsPointInsideHeadArea`.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CBoundaryList.cs†L5-L19】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHead.cs†L5-L112】
- During pose updates, the code checks whether the tool pivot sits inside the outer fence; if not, `mc.isOutOfBounds` is raised and YouTurn logic may be inhibited.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L1055-L1079】
- Section control consults `section[j].isInBoundary` prior to firing coverage heuristics; out-of-bounds sections request OFF immediately and reset their timers.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L897-L1108】

## Headlands

- Headland polygons (`hdLine`) define keep-out/keep-in zones near field edges. `WhereAreToolCorners` flags whether left/right section edges lie inside the headland so the system can determine if the entire toolbar is in headland space.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHead.cs†L37-L62】
- `WhereAreToolLookOnPoints` projects section edges forward by the on-lookahead distance to determine whether upcoming ground lies in the headland. This prevents sections from re-engaging just before entering the headland.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHead.cs†L64-L93】
- Hydraulics use the same state: `SetHydPosition` toggles the hydraulic lift PGN (0xEF) between raise/lower when the tool is in a headland and autosteer conditions permit.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHead.cs†L12-L34】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/PGN.Designer.cs†L143-L167】
- Headland passes themselves are stored separately in `CHeadLine.tracksArr`, enabling operators to drive concentric headland rings using the same curve guidance stack.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CHeadLine.cs†L6-L35】

## Interaction with automation

- **YouTurn** – Before generating a turn, `CYouTurn` checks that the machine remains inside the turn boundary and resets turns if cross-track exceeds 1.3 m, leveraging boundary/headland awareness.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L1055-L1079】
- **Tram control** – Tram byte generation considers headland state to disable tram markers inside headlands and to drive hydraulic outputs accordingly.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L967-L1014】

## Research Notes

- Code pointers: `CPatches.cs`, `CBoundary*.cs`, `CHead.cs`, `CHeadLine.cs`, `OpenGL.Designer.cs`, `Position.designer.cs`.
- Open questions: Dev branch may add boundary-based auto-track selection or dynamic headland widths; not visible in v6.
- Constants: Headland detection relies on pixel lookahead (same as section control) and fixed jack-knife thresholds for hydraulic timing. Boundary spacing and headland widths come from UI wizards outside this extract.
- Dev gap: No access to Dev-specific boundary smoothing or shapefile importers.

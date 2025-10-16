---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "2024-05-13"
---

# Toolbar Coverage and Effective Width

Section geometry, coverage sensing, and section-state decisions determine where the virtual toolbar “should be” relative to logged work and field boundaries.

## Section geometry setup

- Section edges originate from vehicle settings (`setSection_position*`, `setVehicle_toolOffset`). `SectionSetPosition` and `SectionCalcWidths` compute each section’s left/right metre offsets and derive the total tool width from the extreme sections.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L381-L459】
- The same routines populate read-pixel indices (`rpSectionPosition`, `rpSectionWidth`) so OpenGL can sample coverage pixels aligned with each section.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L441-L459】
- During runtime `CalculateSectionLookAhead` transforms those offsets into world coordinates, tracks left/right speeds, and updates `tool.farLeftSpeed / farRightSpeed` for downstream lookahead calculations.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L1391-L1487】

## Effective working width & lookahead

- Lookahead distances for section on/off and hydraulic raise are proportional to edge speeds and configurable time constants (`lookAheadOnSetting`, `lookAheadOffSetting`, `hydLiftLookAheadTime`). Values are clamped (on ≤200 px, off ≤160 px) to cap the preview distance.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L880-L895】
- The “effective” active swath for section control is defined by the read-pixel trapezoid between `lookAheadDistanceOff` and `lookAheadDistanceOn`. The slope `mOn/mOff = (right-left)/width` skews the read window when the outer edges move at different speeds (turning).【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L961-L1073】
- Because each section retains its absolute left/right offsets, partial-section operation (manual Off or boundary clipping) does **not** shift the centreline; instead, individual sections will stay Off while others engage, leaving the pass reference unchanged.

## Coverage sampling and state machine

- On every frame OpenGL reads a strip of the coverage FBO (`GL.ReadPixels`) spanning the tool width (`tool.rpWidth`) and the tallest of section, tram, and hydraulic lookahead heights.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L923-L951】
- For each section the controller counts “untouched” pixels (value 0) inside the on-window. If `(untouched/total) > (100 - minCoverage)` the section requests ON; otherwise it requests OFF, subject to headland and boundary overrides.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L1051-L1108】
- Boundary clipping toggles `section[j].isInBoundary` ahead of time, and headland look-ahead determines whether the upcoming area is inside a headland ring. Sections forced out by boundary/headland rules clear timers and immediately request OFF.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L897-L1108】
- Manual button states override automation: `sectionBtnState == On` forces `sectionOnRequest`, `Off` forces `sectionOffRequest`, while `Auto` obeys coverage heuristics.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L1000-L1055】

## Encoding section states

- Section bitfields (1–16) are encoded into PGN 0xFE and mirrored into machine PGNs (0xE5/0xEF). Each bit represents the logical “On” decision, combining auto logic and manual overrides.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L497-L556】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/PGN.Designer.cs†L35-L170】
- Edge speeds (left/right) are also forwarded (PGN 0xE5) using the `toolLSpeed/toolRSpeed` slots, enabling downstream rate or hydraulic controllers to anticipate twist across the boom.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L521-L556】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/PGN.Designer.cs†L168-L192】

## Research Notes

- Code pointers: `Sections.Designer.cs`, `Position.designer.cs`, `OpenGL.Designer.cs`, `PGN.Designer.cs`.
- Open questions: Dev branch may add dynamic effective-width calculations; v6 keeps the guidance centreline fixed regardless of which sections are active.
- Constants: pixel clamping thresholds (200 px on, 160 px off) correspond to 20 m and 16 m at 10 cm per pixel. Minimum coverage defaults to 100 % (`setVehicle_minCoverage`).【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L880-L904】【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L165-L220】
- Dev gap: No access to Dev-specific toolbar width heuristics.

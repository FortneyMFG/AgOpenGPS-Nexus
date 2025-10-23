---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "-"
---

# Glossary and Symbols

This appendix collects the recurring symbols, frames, and configuration keys used by the v6 guidance stack. Values are paraphrased for clarity; see linked source for exact implementations.

## Symbol table

| Symbol | Definition | Frame / Units | Source |
| --- | --- | --- | --- |
| `pn.fix` | Current GNSS fix stored as local easting/northing (metres) after WGS84→local conversion; `fixQuality`, `age`, and `headingTrue` accompany it.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CNMEA.cs†L9-L90】 | Local tangent plane (`latStart`, `lonStart` origin), metres. | `CNMEA.fix` consumed by `FormGPS` |
| `pivotAxlePos` | Vehicle reference point at antenna pivot offset; holds easting, northing, heading in radians.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L41-L1263】 | Local ENU, metres & radians. | `FormGPS.CalculatePositionHeading()` |
| `steerAxlePos` | Steer axle pose derived from `pivotAxlePos` plus wheelbase along heading.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L41-L1263】 | Local ENU, metres & radians. | `FormGPS.CalculatePositionHeading()` |
| `guidanceLookPos` | Preview point ahead of pivot, distance `max(tool.width/2, speed·lookAheadTime)`.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L1265-L1270】 | Local ENU, metres. | `FormGPS.CalculatePositionHeading()` |
| `guidanceLineDistanceOff` | Cross-track error published to autosteer; stored as millimetres (converted to ±127 lightbar steps via `*0.05`).【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L918-L959】 | Vehicle lateral frame, millimetres. | `FormGPS` autosteer loop |
| `guidanceLineSteerAngle` | Commanded steer angle sent to PGN 0x254, stored in centidegrees (×100).【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L998-L1017】 | Degrees ×100. | `FormGPS` autosteer loop |
| `tool.width`, `tool.overlap`, `tool.offset` | Implement geometry: swath width, overlap subtraction, lateral shift for 3‑pt hitches.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTool.cs†L11-L93】 | Metres. | `CTool` constructor |
| `tool.hitchLength`, `trailingHitchLength` | Distances from vehicle pivot to implement pivot(s). Used to position toolbar and preview points.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTool.cs†L18-L143】 | Metres. | `CTool` constructor & draw routine |
| `tram.controlByte` | Bitfield describing left/right tram alignment (`0x01` right, `0x02` left).【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTram.cs†L27-L38】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L969-L982】 | Bitmask. | `CTram`, OpenGL detector |
| `goalPointLookAheadHold`, `goalPointLookAheadMult`, `goalPointAcquireFactor` | Parameters tuning lookahead distance vs cross-track for Stanley/Pure Pursuit controllers.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CVehicle.cs†L18-L74】 | Dimensionless scalars. | `CVehicle` constructor |
| `rowSkipsWidth`, `rowSkipsWidth2`, `turnSkips` | Tram/row skip counters used by YouTurn to select the next pass offset.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CYouTurn.cs†L23-L2337】 | Integer multiples of `(tool.width - overlap)`. | `CYouTurn` |
| `p_254`, `p_239`, `p_229` | PGN buffers for autosteer (0xFEF2), section (0x0EF), and machine messages. Fields include `status`, `lineDistance`, `tram`, `toolLSpeed`.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L918-L1039】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L520-L561】 | CAN PGN payloads (bytes). | `FormGPS` autosteer loop, `Sections` module |
| `vec2`, `vec3` | Lightweight position structs storing easting/northing (+heading for `vec3`).【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/vec3.cs†L12-L115】 | Local ENU, metres & radians. | `vec2`, `vec3` definitions |

## Configuration key highlights

| Setting key | Default | Influence |
| --- | --- | --- |
| `setTram_tramWidth`, `setTram_passes`, `setTram_alpha` | 24 m, 1 pass, α = 0.8 | Tram generation width, number of passes, overlay opacity.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L52-L55】【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L271-L276】 |
| `setVehicle_goalPointLookAheadHold`, `setVehicle_goalPointLookAheadMult` | User-tunable doubles | Governs lookahead scaling in `CVehicle.UpdateGoalPointDistance`.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CVehicle.cs†L18-L74】 |
| `setAS_minSteerSpeed`, `setAS_maxSteerSpeed`, `setAS_isSteerInReverse` | Vehicle min/max steering speeds, reverse permission flag | Autosteer gate logic and reverse-disable guard.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CVehicle.cs†L18-L80】【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L95-L103】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L939-L996】 |
| `setAutoSwitchDualFixOn`, `setAutoSwitchDualFixSpeed` | false, 2 km/h | Automatic dual-antenna switching thresholds.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L277-L278】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L153-L160】 |
| `setTool_isSectionOffWhenOut`, `setVehicle_minCoverage` | Booleans/percentages | Section control gating when outside boundary or below coverage threshold.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CTool.cs†L21-L105】 |
| `setGPS_isRTK_KillAutoSteer`, `setGPS_ageAlarm` | false, 20 s | RTK alarm behaviour and stale-fix threshold.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L69-L103】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CNMEA.cs†L30-L44】 |

## Naming bridge to Nexus

| AgOpenGPS v6 name | Suggested Nexus naming direction | Notes |
| --- | --- | --- |
| `pivotAxlePos` | `vehicle.pose.pivot` | Keep vector struct with (east, north, heading). |
| `steerAxlePos` | `vehicle.pose.steer_axle` | Required for curvature and preview math. |
| `guidanceLookPos` | `guidance.preview_point` | Used by Stanley/Pure Pursuit preview; expose as ENU vector. |
| `guidanceLineDistanceOff` | `guidance.error.cross_track_mm` | Maintain millimetre storage for PGN compatibility. |
| `guidanceLineSteerAngle` | `guidance.command.steer_angle_cdeg` | Stored as centidegrees for CAN transport. |
| `tram.controlByte` | `guidance.tram.flags` | Preserve bit semantics to interoperate with section control. |
| `tool.width` | `implement.swath.width_m` | Pair with `overlap` and `offset` for coverage math. |
| `setAutoSwitchDualFixSpeed` | `config.gnss.dual_switch_speed_kph` | Mirror the km/h threshold control. |
| `rowSkipsWidth` | `guidance.rows.skip_passes` | Map to integer skip count for automatic pass selection. |
| `p_254` payload | `bus.autosteer.pgn_0xF0FE` | Keep field ordering for downstream ECUs. |

## Research Notes

- Code pointers: `CNMEA.cs`, `Position.designer.cs`, `CTool.cs`, `CTram.cs`, `CVehicle.cs`, `CYouTurn.cs`, `Sections.Designer.cs`, `vec3.cs`.
- Open questions: Some symbols (e.g., `pn.fixOffset`) are written during external corrections; behaviour not analysed here.
- Constants: Cross-track scaling uses 0.05 to compress millimetres to lightbar bytes; lookahead defaults hinge on tool width vs speed.
- Edge cases: `guidanceLineDistanceOff = 32020` denotes “no target”; ensure Nexus interprets this sentinel when bridging messages.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L918-L938】

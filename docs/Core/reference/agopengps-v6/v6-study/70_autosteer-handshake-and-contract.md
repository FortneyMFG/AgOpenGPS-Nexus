---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "-"
---

# Autosteer Handshake and Contract

AgOpenGPS v6 communicates with the autosteer ECU using proprietary PGNs on each fix update. The contract covers desired steering angle, cross-track, status flags, speed, and section bits.

## Update frequency and timing

- `SendPgnToLoop(p_254.pgn)` is invoked every fix cycle after guidance calculations, so the ECU receives fresh commands at ~10 Hz (the `gpsHz` cadence). Recorded-path and free-drive modes reuse the same frame with adjusted status bits.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L899-L1043】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/FormGPS.cs†L83-L168】

## PGN 0xFE (Autosteer Command)

| Field | Bytes | Units / scale | Source | Notes |
| --- | --- | --- | --- | --- |
| Speed | 5–6 | 0.1 km/h (unsigned) | `abs(avgSpeed)` | Sign is discarded; ECU infers direction separately.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L903-L904】 |
| Status | 7 | 0=disengaged, 1=engaged | Autosteer state machine | Set to 0 when autosteer button off, speed outside `[minSteerSpeed, maxSteerSpeed]`, reverse gear (unless `setAS_isSteerInReverse`), dead-zone conditions, or sensors not ready.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L910-L1016】 |
| Steer angle | 8–9 | 0.01° signed | `guidanceLineSteerAngle` | Centi-degree command derived from controller output; suppressed when dead-zone active.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L1000-L1016】 |
| Line distance | 10 | 0.02 m unsigned (0–255) | `guidanceLineDistanceOff` | 32000/32020 sentinel values map to 255 (no data). Otherwise `distance_mm * 0.05 + 127` encodes ±2.54 m.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L923-L937】 |
| Section bits 1–8 | 11 | bitmask | Section controller | Combined logical state for sections 1–8.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L497-L521】 |
| Section bits 9–16 | 12 | bitmask | Section controller | Combined logical state for sections 9–16.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L497-L556】 |

Field indices are defined in `CPGN_FE` (`speedLo`, `speedHi`, `status`, etc.) and initialised with the PGN header `{0x80,0x81,0x7f,0xFE,8,...}`.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/PGN.Designer.cs†L35-L54】

## Additional PGNs

- **0xFD (feedback)** – Receives actual steering angle, heading, roll, and switch status. Guidance references `mc.actualSteerAngleDegrees` to compute dead-zone gating but otherwise treats feedback as telemetry.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/PGN.Designer.cs†L56-L74】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L1000-L1017】
- **0xEF (machine status)** – Publishes u-turn, speed, hydraulic lift command, tram control, geofence status, and section bits (mirrors 0xFE). Hydraulics are triggered by headland logic; tram bytes encode outer/inner tram states.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/PGN.Designer.cs†L143-L167】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/OpenGL.Designer.cs†L967-L1108】
- **0xE5 (section telemetry)** – Extends section bits to 64 positions and reports left/right tool speeds for rate controllers.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/PGN.Designer.cs†L168-L192】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Sections.Designer.cs†L521-L556】
- **0x64 (corrected position)** – Optional GNSS+heading broadcast for external consumers (24-byte payload containing lat, lon, heading).【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L840-L848】

## State machine nuances

- Status toggles to 0 (“disengaged”) when autosteer button is off, the speed is outside allowed bounds, direction changes to reverse, or the dead-zone logic detects small error for too long (`deadZoneDelay`).【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L910-L1016】
- Free-drive mode (`vehicle.isInFreeDriveMode`) still sends PGN 0xFE but overrides speed to 8.0 km/h and steer angle to the UI slider, keeping the ECU active while the operator drives manually.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L1019-L1036】
- Recorded-path playback forces status=1 even if autosteer button is off, ensuring the ECU follows the scripted path during drive commands.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L918-L919】

## Sign conventions

- Steering angles positive right / negative left; the ECU expects centi-degrees in two’s complement, matching the sign used for actual steering feedback (`mc.actualSteerAngleDegrees`).【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L903-L1016】
- Cross-track distance is sent as magnitude+sign in `guidanceLineDistanceOff` (mm). The lightbar byte uses an offset encoding (0–255) so physical zero corresponds to 127.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L923-L937】

## Research Notes

- Code pointers: `Position.designer.cs`, `PGN.Designer.cs`, `Sections.Designer.cs`, `OpenGL.Designer.cs`.
- Open questions: Dev branch may add heartbeat/timeout handling; v6 relies on sending every fix without explicit ACKs.
- Constants: Max lightbar span ±2.54 m (5 cm units); dead-zone thresholds set via `setAS_deadZoneHeading` / `setAS_deadZoneDelay` settings.【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L923-L1016】【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L264-L269】
- Dev gap: No Dev data on CAN framing or additional PGNs (e.g., IMU streaming) beyond what v6 exposes.

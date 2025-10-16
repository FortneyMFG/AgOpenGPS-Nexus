---
intent: "AOG v6/Dev guidance extraction"
status: "reference-notes"
source_branch: "Legacy SourceCode -V6@cf5eafe25e7bc29b0023e81b14424a04645aca0e"
last_audit: "2024-05-13"
---

# Error Terms and Controllers

AgOpenGPS v6 implements two primary lateral controllers—Stanley and Pure Pursuit—sharing common error definitions while applying different blends of heading and cross-track information.

## Core error definitions

- **Pivot cross-track (`e_y`)** – Signed perpendicular distance from the pivot axle to the current pass. Computed via 2‑D cross product:  
  \( e_y = \frac{(dy\,x_p - dx\,y_p + x_B y_A - y_B x_A)}{\sqrt{dx^2 + dy^2}} \) where `(dx, dy)` span the pass segment.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L185-L193】
- **Steer cross-track** – Same projection applied to the steer axle after shifting the pass by the integral term `inty` to represent implement draft.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGuidance.cs†L120-L169】
- **Heading error (`e_ψ`)** – Difference between vehicle (pivot or steer) heading and pass heading, wrapped to ±90° so the controller does not flip when the machine reverses direction.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L315-L325】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGuidance.cs†L178-L189】
- **Preview/goal point** – Generated at distance `L = goalPointDistance` along the pass ahead of the pivot, where `goalPointDistance` adapts to cross-track magnitude and vehicle speed (see below).【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L241-L262】【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CVehicle.cs†L83-L142】

## Goal distance adaptation

`CVehicle.UpdateGoalPointDistance` scales lookahead between `goalPointLookAheadHold` and `goalPointLookAheadHold * goalPointAcquireFactor` based on |`e_y`| (in `modeActualXTE`). The result is clamped to ≥2 m, providing longer lookahead when close to the line and shorter lookahead when far away.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CVehicle.cs†L83-L142】

## Stanley controller

- **Equation** – After computing steer cross-track `XTEc = atan(k_e * e_y / speed)` and heading error (scaled by `stanleyHeadingErrorGain`), the controller outputs  
  \( u = - (\text{xTrackSteerCorrection} + e_ψ) \) in radians, converted to degrees for the autosteer ECU. Lateral derivative (`derivativeDistError`) and integral (`inty`) damp oscillations and compensate for implement drift.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGuidance.cs†L42-L108】
- **Smoothing** – Cross-track correction is low-pass filtered (`0.5` weighting) and derivative is updated every six cycles to reduce noise. Integral accumulation is gated by speed, autosteer state, and error thresholds, with fast decay (×0.7) when inactive.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGuidance.cs†L54-L95】
- **Side-hill compensation** – Roll from `CAHRS` multiplies `sideHillCompFactor`, letting the operator bias the steering angle against slopes.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGuidance.cs†L97-L101】

## Pure Pursuit controller

- **Curvature** – Using the lookahead point, curvature is \(\kappa = \frac{2 \cdot \Delta x}{L^2}\) in vehicle coordinates (`Δx` is the lateral component). Steering command becomes `atan(κ * wheelbase)` converted to degrees and limited to `±maxSteerAngle`.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L264-L289】
- **Draft compensation** – Pivot cross-track feeds an integral term that laterally offsets the target line (`inty`) before recomputing steer geometry, similar to Stanley. Integrator clamps to ±0.2 m and decays when the machine slows or leaves autosteer.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L195-L238】
- **Derivative damping** – Pure Pursuit tracks a pivot derivative (`pivotDerivative`) to detect rapid error changes; although the derivative is not added directly to the command, it gates the integral accumulation to avoid wind-up during sharp manoeuvres.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L198-L237】

## Controller outputs

Both controllers ultimately populate `guidanceLineDistanceOff` (millimetres) and `guidanceLineSteerAngle` (centi-degrees). Autosteer PGNs use those values alongside actual steering feedback to enforce dead-zone logic and report cross-track performance.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CABLine.cs†L329-L331】【F:Legacy SourceCode -V6/SourceCode/GPS/Forms/Position.designer.cs†L903-L1043】

## Tunable parameters

| Parameter | Setting key | Default | Notes |
| --- | --- | --- | --- |
| Stanley distance gain (`k_e`) | `stanleyDistanceErrorGain` | 1.0 | Multiplies cross-track term in Stanley controller.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L176-L177】 |
| Stanley heading gain | `stanleyHeadingErrorGain` | 1.0 | Scales heading error inside Stanley.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L176-L177】 |
| Stanley integral gain | `stanleyIntegralGainAB` | 0.0 | Governs draft correction when `pivotDistanceError` small.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L198-L203】 |
| Pure Pursuit integral | `purePursuitIntegralGainAB` | 0.0 | Enables integral draft compensation in PP controller.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L152-L166】 |
| Goal lookahead hold | `setVehicle_goalPointLookAheadHold` | 3 m | Nominal lookahead multiplier when on-line.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L165-L220】 |
| Goal acquire factor | `setVehicle_goalPointAcquireFactor` | 0.9 | Shrinks lookahead when far from the line.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L272-L279】 |
| Side-hill gain | `setAS_sideHillComp` | 0.0 | User scaling for roll compensation.【F:Legacy SourceCode -V6/SourceCode/GPS/Properties/Settings.cs†L95-L104】 |

## Worked example (single timestep)

1. Pivot cross-track `e_y` = 0.15 m to the left; heading error `e_ψ` = 2°.  
2. At 6 km/h, Stanley computes `speedTerm = 1 + 0.277*(speed−1) ≈ 2.388` and `XTEc = atan(0.15 / 2.388) ≈ 3.6°`.  
3. Low-pass filtering halves last cycle’s correction; suppose previous `xTrackSteerCorrection = 4°`, new value ≈ `(4 + 3.6)/2 = 3.8°`.  
4. Command `u = −(3.8° + 2°) ≈ −5.8°`, bounded by `maxSteerAngle` and adjusted for slope/integral before writing to PGN.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGuidance.cs†L48-L108】

## Research Notes

- Code pointers: `CGuidance.cs`, `CABLine.cs`, `CABCurve.cs`, `CVehicle.cs`, `Settings.cs`.
- Open questions: Dev branch may experiment with curvature feed-forward; no evidence in v6.
- Constants: Stanley derivative update every sixth cycle, integral trigger limits (pivot error <0.25 m, derivative <1) are hard-coded.【F:Legacy SourceCode -V6/SourceCode/GPS/Classes/CGuidance.cs†L71-L99】
- Dev gap: Without Dev sources we cannot confirm alternate controllers (e.g., pure pursuit for curves vs. Stanley toggle behaviour beyond what’s in v6).

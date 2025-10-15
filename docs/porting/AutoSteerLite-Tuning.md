# AutoSteer-Lite Tuning Notes

NX-053 ports the legacy AutoSteer-Lite look-ahead scheduling and startup ramp behaviour into the Nexus plugin stack. The new
`AutoSteerLiteTuningProfile` mirrors the V6 parameters that governed goal point distance, cross-track filtering, and the initial
engagement ramp so AutoSteer starts smoothly without overshoot. The profile feeds a stateful `AutoSteerLiteTuningState` that the
controller consults each cycle to blend the hold/acquire multipliers with current speed and cross-track error.

## Constraint-aware look-ahead (NX-316)

Dynamic look-ahead now honours ADR-027 constraint gating cues. The tuning profile exposes:

- `HeadlandSlowdownMultiplier` and `ConstraintSlowdownMultiplier` to bias the computed distance when the pose mask indicates a
  headland or blocking keep-out zone.
- `ConstraintDistanceMarginMeters` to clamp goal points against the detected clearance so steering targets stay inside the
  permitted area.

The `AutoSteerLiteTuningState.Update` method accepts an optional `ConstraintLookAheadContext` describing the active mask. When
present, the controller applies the multipliers, enforces the clearance margin, and smooths the filtered result so look-ahead
shrinks predictably as the vehicle approaches a constraint.【F:Nexus SourceCode/src/Aog.Plugins/AutoSteer/AutoSteerLiteTuning.cs†L13-L151】

Key behaviours:

- Look-ahead distance now adapts dynamically with speed and cross-track error, matching the V6 `CVehicle.UpdateGoalPointDistance`
  formula.
- Startup ramp keeps steering gentle for the first few metres after engagement before blending to steady-state gains.
- Cross-track and look-ahead outputs are exponentially filtered to avoid abrupt changes between controller cycles.

See `AutoSteerLiteController` and `AutoSteerLiteTuningProfile` for the ported implementation details.

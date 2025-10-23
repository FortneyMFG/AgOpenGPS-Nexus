# Guidance Orchestrator — Live Field Builder

The live field builder turns the operator's first lap into a deterministic boundary with optional keep-outs.

## Responsibilities
- Ingest fused ENU pose samples from PoseStream at ≥50 Hz while Recording.
- Detect closed loops using spatial hashing + segment intersection checks at 2 Hz.
- Polygonize candidate loops, enforce CCW/CW winding, and simplify with configurable tolerance (default 0.1 m).
- Validate polygons (`area ≥ 0.25 ha`, `vertices ≤ 50k`, `holes ≤ 32`, buffer(0) repair) before surfacing prompts.
- Assign monotonically increasing `field.rev` and `hole.rev` values on acceptance.

## Algorithms
1. **Loop detection**
   - Maintain sliding window of track vertices with R-tree index.
   - Identify self-intersections; extract simple loop segments.
   - Reject loops failing quality heuristics (area threshold, min vertex spacing ≥ 0.05 m).
2. **Polygonization**
   - Order vertices along the driven path direction; ensure outer loops are CCW.
   - Apply `buffer(0)` repair if GEOS reports invalid geometry; surface UI prompt on failure.
3. **Revision management**
   - Each accepted outer loop increments `field.rev` and stores `origin_llh` + `enu_epoch`.
   - Each keep-out increments its own `hole.rev` for plan key hashing.

## Events
- `FieldChanged(field.rev)` after 500 ms debounce on accepted boundary edits.
- `KeepOutChanged(hole.rev)` after 250 ms debounce per hole.

## Telemetry
- Record polygon area (ha), perimeter (m), simplification tolerance, repair outcome.
- Emit warnings when polygon density > 2 pts/m and recommend simplification.

## Acceptance checks
- [ ] Simulated loop detection recovers boundaries within ±0.2 m of ground truth for σ ≤ 0.15 m noise.
- [ ] Keep-out creation prompts show area readout and correctly update `hole.rev`.
- [ ] Invalid geometry attempts log repair attempts and surface actionable UI prompts.
- [ ] Revision IDs increment monotonically and survive process restarts.

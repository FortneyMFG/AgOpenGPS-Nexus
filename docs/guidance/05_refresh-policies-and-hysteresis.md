# Guidance Orchestrator — Refresh Policies & Hysteresis

Explains refresh triggers, debounces, and hysteresis thresholds that prevent planner thrash.

## Hysteresis thresholds
- Width change banner at ±10% (`or ±0.3 m`), auto-replan at ±20%.
- Pass-selection epsilon `ε = 0.05` normalized (≈0.10 m lateral) required before switching swaths.
- Planner debounce windows: boundary 500 ms, keep-out 250 ms.

## Refresh triggers
- Immediate: new boundary/keep-out accepted, headland config change, orientation mode toggle, Quick Refresh button, planner timeout fallback.
- Deferred: minor width change (<10%), row bias updates, coverage-only progress.
- Optional auto-refresh after headland N completes (per job configuration).

## Partial vs. full replan
- Partial when edits >30 m from active path AND family order unchanged AND active path equivalence holds.
- Full replan when connectors change, edits near active path, or plan equivalence fails.

## Operator experience
- Quick Refresh button surfaces only when inputs changed beyond hysteresis thresholds.
- UI banner prompts for width changes and fallback states (AB vs F2C).
- Manual overrides freeze plan, suppressing auto-refresh until resume.

## Acceptance checks
- [ ] Banner-only refresh cases never trigger planner call.
- [ ] Quick Refresh command acknowledges within 250 ms and returns new plan ≤1.5 s.
- [ ] Partial replan preserves active path when equivalence satisfied; otherwise defers swap.
- [ ] Override freeze blocks auto-refresh until operator resumes plan.

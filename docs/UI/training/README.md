# Simulation Scenario Library

The scenario library bundles curated presets that match the default simulation providers
shipped with Nexus. Each scenario targets a common workflow so operators can load a
realistic environment without building a configuration from scratch. Use it alongside the
[composite simulation fabric checklist](composite-simulation-fabric.md) to keep presets,
seed fixtures, and topic coverage aligned with ADR-004.

## Files
- `library.json` — full simulation configuration that declares the shared providers,
  default routes, and three scenario presets.
- [`legacy-auto-run/`](../../Core/reference/agopengps-v6/scenarios/legacy-auto-run/README.md) — legacy UDP scenario pack with soak verification logs and soak reports.

## Scenario Presets
### `baseline-guidance`
- Straight AB-line guidance with deterministic IMU noise.
- Use when validating steering loops, plugin wiring, or CI smoke tests.

### `headland-training`
- Switches the guidance route to the headland planner and slows the clock playback.
- Helpful for operators practicing headland turns and manual overrides.

### `replay-overlay`
- Uses a recorded guidance stream while the vehicle dynamics continue to run live.
- Ideal for debriefs where telemetry is available but sensor fusion still runs in real time.
- Pair with the [cross-track replay harness slice](../../Core/howto/cross-track-replay-harness.md) to
  validate replay parity before loading field captures.

## Loading the Library
1. Launch the UI and open **Simulation → Edit scenarios...**.
2. Choose **Import**, select `docs/UI/training/library.json`, and pick the desired scenario.
3. Press **Apply scenario** to push the preset into the simulation bar.

For headless runs, pass the file to the tooling scripts, e.g. `nexus sim --config
./docs/UI/training/library.json --scenario headland-training`.

## Performance Budgets
The `performance-matrix.json` catalog is exercised by the
`SimulationPerformanceHarness` integration tests. Each run now wraps the
simulation bus with the `InstrumentedSimBus` and aggregates publish timings via
`SimulationPerformanceBudgetRecorder`. The resulting budget snapshot enforces
CPU-oriented thresholds (messages per second, max publish duration) so ADR-026
performance budgets stay measurable in CI. Update both the scenario presets and the
[performance budget dashboards](../Core/performance-budget-telemetry-dashboards.md) when
topic coverage or provider mixes change.

# Simulation Scenario Library

The scenario library bundles curated presets that match the default simulation providers
shipped with Nexus. Each scenario targets a common workflow so operators can load a
realistic environment without building a configuration from scratch.

## Files
- `library.json` — full simulation configuration that declares the shared providers,
  default routes, and three scenario presets.

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

## Loading the Library
1. Launch the UI and open **Simulation → Edit scenarios...**.
2. Choose **Import**, select `docs/scenarios/library.json`, and pick the desired scenario.
3. Press **Apply scenario** to push the preset into the simulation bar.

For headless runs, pass the file to the tooling scripts, e.g. `nexus sim --config
./docs/scenarios/library.json --scenario headland-training`.

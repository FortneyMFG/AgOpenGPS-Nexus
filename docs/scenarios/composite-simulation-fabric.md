# Composite Simulation Fabric GA Checklist

[ADR-004 — Composite simulation fabric](../ADR/ADR-004-composite-simulation.md) defines the composite simulation fabric that stitches SimClock, SimBus topics, and deterministic fixtures together. Use this checklist to confirm the GA cut meets expectations and remains reproducible in CI. Cross-reference the [simulation library](README.md) and targeted replay coverage in the [cross-track harness guide](../howto/cross-track-replay-harness.md) when validating updates.

## Fabric Components

- **SimClock.** Provides deterministic time progression with pause/step controls. All providers subscribe to the shared clock.
- **SimBus topics.** Canonical topics cover pose, guidance, section states, implement IO, GNSS, IMU, and telemetry logs. Providers publish/subscribe using strongly typed contracts documented in the [capability registry](../reference/capability-registry.md).
- **Provider bundles.** Core ships ground truth, GNSS/IMU noise, implement physics, and environment/weather providers with seeded random generators.
- **Replay bridge.** Converts captured telemetry logs into SimBus events for regression comparison, complementing the [legacy auto-run scenario](legacy-auto-run/README.md) for UDP regression coverage.

## GA Criteria

1. **Topic catalog.** `docs/scenarios` lists every SimBus topic with schema references and producer/consumer ownership.
2. **Seeded fixtures.** CI hosts golden seeds for smoke, guidance, and coverage scenarios. Each seed records expected outputs for Core, Plugins, and telemetry sinks.
3. **Extensibility hooks.** Provider registry supports configuration via JSON and injection of custom providers for partner sims.
4. **Performance budget.** Fabric runs within the CPU/memory budgets defined in ADR-026 for headless and UI-attached runs.

## Validation Steps

- **Determinism sweep.** Run the nightly determinism job to compare telemetry hashes across Windows x64 and Linux ARM64 agents.
- **Plugin integration.** Execute the plugin regression suite (`dotnet test --filter Category=SimulationFabric`) to confirm plugin providers bind to the expected topics and surface metadata described in the [metadata-driven UI style guide](../reference/metadata-driven-ui-style-guide.md).
- **Replay parity.** Feed recorded field data through the replay bridge and compare outputs to live sim runs using the provided diff tooling. Use the [cross-track replay harness](../howto/cross-track-replay-harness.md) as a smoke slice before running full suites.
- **Documentation review.** Ensure scenario guides reference current topic names and include any new providers introduced in the release, updating the [scenario library index](README.md) when presets change.

Adhering to this checklist keeps the composite simulation fabric aligned with ADR-004 while giving QA and partner teams clear expectations for validation.

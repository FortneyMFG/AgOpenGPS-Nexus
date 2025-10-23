# Guidance Orchestrator Plugin

The Guidance Orchestrator turns live driving into Fields2Cover plans and deterministic `SteerTargets`.

## Responsibilities
- Record first headland laps, build field boundaries and keep-outs.
- Maintain implement lens (`EffectiveWidth`) with hysteresis and revision tracking.
- Call the F2C sidecar via gRPC, manage caching/fallback/retention.
- Stream `SteerTargets` at 25 Hz with speed caps and row-bias hooks.
- Surface Quick Refresh, plan provenance, and override states in the UI.

## Directory layout
- `src/` — plugin implementation (pending).
- `tests/` — unit + integration tests (pending).
- `appsettings.example.json` — configuration defaults.

Refer to [`docs/guidance`](../../docs/Core/guidance/01_live-field-builder.md) for design notes.

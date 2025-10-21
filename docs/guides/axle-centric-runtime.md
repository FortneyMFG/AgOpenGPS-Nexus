# Axle-centric runtime playbook

Tasks **NX-452** through **NX-455** deliver the axle-centric runtime introduced by
[ADR-067](../SRS/sections/6X_Core_Domain_Services/61-ADR-067 - Equipment configuration and axle-centric kinematics runtime.md). This guide captures the
operator and engineering workflows required to take a rig from configuration export to
field-ready automation.

## Profile ingestion (NX-452)
- Profiles are exported as JSON using schema version `1.0`. The runtime now ships with
  `AxleCentricProfileLoader`, which performs deterministic canonicalisation, computes the
  content hash, enforces compatibility guards, and emits `KIN-###` error codes.
- Health telemetry is exposed via `AxleCentricTelemetry`. It reports axle counts, per-mode
  curvature limits, the maximum slip budget, and the profile hash so support can confirm the
  active configuration without retrieving files manually.
- Deterministic seeds come from the payload when present, otherwise they are derived from the
  profile hash or can be overridden when running fixtures. This keeps regression simulations
  stable across ingest/export cycles.

## Automation integration (NX-453)
- `AxleAutomationIntegrator` feeds the planner and autosteer controllers with the advertised
  curvature and slip limits for each mode. The integrator publishes a `AutomationModeSnapshot`
  through `IAutomationModeSink` so existing automation modules can latch onto a single
  integration point.
- Turn radius is computed from the curvature limit using `1/κ`, which means the planners do not
  need to reimplement the Ackermann maths. Drive direction policies (Forward, Reverse, or
  Bidirectional) are pushed to the controllers alongside the deterministic seed and content hash
  for deterministic logging.

## Calibration workflows (NX-454)
- `CalibrationWorkflows` hosts the Ackermann wizard, hitch zeroing helper, slip sanity checks,
  and transport lock verification. Results are returned as strongly typed records so UI and CLI
  tooling can present consistent messaging.
- Ackermann validation targets ≤0.2° RMS residual, mirroring the acceptance gates documented in
  the blueprint. Hitch zeroing enforces ±0.2° tolerance by default, while slip checks compute
  the average slip ratio and a derate value if limits are exceeded.

## Documentation and presets (NX-455)
- Preset bundles live under `artifacts/presets/axle-centric/`. Each JSON file includes the
  canonical hash, deterministic seed, and mode limits ready for ingestion.
- Support teams should reference this guide when onboarding axle-centric rigs. The telemetry
  topics `/machine/health`, `/planner/limits`, and `/calibration/status` now map directly to the
  structures described here.

## Regression checklist
- ✅ Profile hash matches between configurator export and runtime ingestion.
- ✅ Automation planners receive the same curvature limits advertised in the profile.
- ✅ Calibration workflows pass with Ackermann residual ≤0.2° RMS and hitch zero offsets within
  the configured tolerance.
- ✅ Slip sanity checks cover both low-speed and headland manoeuvres, with derate values logged
  when exceeding the per-mode slip budget.

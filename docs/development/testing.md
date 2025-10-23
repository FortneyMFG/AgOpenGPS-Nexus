# Testing Guidelines

Nexus validation spans unit, integration, deterministic simulation, and
hardware-in-the-loop coverage. Use this page as a launchpad into the
validation resources maintained under `docs/development/qa/` and the
training library.

## Quick Links

- [QA dashboard runbook](qa/qa-dashboard.md) — aggregating metrics and
  release readiness evidence.
- [Plugin QA handshake](qa/plugin-qa-handshake.md) — required steps
  before shipping new or updated plugins.
- [Fault injection harness](qa/fault-injection-harness.md) — reproduce
  mesh and subsystem failures deterministically.
- [HIL automation rig](qa/hil-automation-rig.md) — bench orchestration
  with telemetry capture and regression logging.
- [Training scenarios](training/README.md) — composite replay drills and
  scenario fixtures for onboarding and smoke validation.

Pair these resources with the [developer guide](INDEX.md) and the
[simulation harness how-to](howto/cross-track-replay-harness.md) when
planning validation for a change. Record executed checks in your PR and
update `tasks.md` with any additional manual verification that occurs.

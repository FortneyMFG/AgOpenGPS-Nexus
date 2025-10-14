# NX-095 Dealer support escalation process

The dealer escalation playbook provides a predictable path from first-line intake to
engineering follow-up. It aligns field telemetry (NX-094) with human response times so
operators receive timely updates even when complex reproductions are required.

## Intake and triage

1. **Initial capture (0–4 hours)** — Regional support staff log requests in the ticketing
   queue, attach any immediate screenshots/logs, and link the most recent field feedback
   snapshot for the affected machine/build.
2. **Triage huddle (same business day)** — The support coordinator reviews new tickets,
   tags impacted subsystems (Guidance, Sections, Telemetry, etc.), and assigns severity based on
   safety impact and downtime risk. Critical tickets trigger an on-call ping immediately.
3. **Acknowledgement (within 8 hours)** — Dealers receive a templated acknowledgement email
   summarising severity, any required log uploads, and the next update window.

## Escalation ladders

- **Level 1 — Support engineering (≤ 24 hours)**: Validate reproduction steps, collect
diagnostics, and apply known workarounds. Publish interim updates to the ticket every business
 day.
- **Level 2 — Feature owner (≤ 48 hours)**: When Level 1 exhausts workarounds or telemetry
  shows regressions, hand off to the owning engineer. They document hypothesis, debug plan, and
  required instrumentation directly in the ticket.
- **Level 3 — Cross-team task force (≤ 72 hours)**: For blocking fleet issues, the release
  manager convenes core/AGiO/plugin leads. They decide on hotfix scope, rollback, or feature flag
  adjustments, recording actions and ETA.

## Status reporting

- **Daily digest** — Support coordinator circulates a digest summarising open escalations,
  severity, owners, and ETA using the latest aggregated telemetry counts for context.
- **Dealer updates** — Dealers receive updates at least every 24 hours (critical) or every
  48 hours (warning) even if the root cause is still under investigation.
- **Closure review** — When resolved, attach mitigation steps, firmware/app builds, and
  confirm with the dealer before marking closed. Feed findings back into the community preview
  program (NX-096) to validate the fix with opt-in fleets before broad release.

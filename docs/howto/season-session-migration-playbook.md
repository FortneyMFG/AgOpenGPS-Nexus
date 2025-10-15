# Season/session migration playbook (NX-320)

Season organisers (ADR-040) and job sessions (ADR-041) move Nexus away from
implicit "run" folders toward an auditable hierarchy: **Season → Job →
Session**. This playbook packages the rollout plan for field deployments so
operators, dealers, and plugin teams can stage the new lifecycle with minimal
downtime.

## Audience & prerequisites

- **Audience:** Deployment leads, dealer technicians, fleet QA, and plugin
  maintainers responsible for Nexus upgrades.
- **Prerequisites:**
  - Season aggregator & sync orchestration from NX-223 is deployed in staging
    (core daemon + CLI wrappers).
  - Session start/stop UX refresh from NX-295 (or newer) is in the build
    targeted for rollout.
  - Plugin teams have acknowledged ADR-040/041 lifecycle hooks in their
    manifests or have a mitigation plan documented in their tickets.
  - Staging workspace with representative farms/jobs that mirrors the production
    folder layout.

## Migration phases

### Phase 0 — Readiness inventory

1. **Catalogue active farms/jobs.** Export the current farm/job roster from the
   discovery service or the `farms/` directory and tag jobs that must launch in
   the first season-enabled window.
2. **Map plugin dependencies.** Record which plugins react to `onJobLoaded`
   and confirm their session-aware upgrades are scheduled. Plugins that cannot
   handle session events must be flagged for sandbox-only usage.
3. **Align terminology.** Update runbooks, UI training material, and dispatch
   macros to refer to *sessions* instead of *runs*. Sync with dealer training so
   operator messaging is consistent the day features launch.

### Phase 1 — Toolchain validation

1. **Season aggregator smoke test.** Using staging data, run the NX-223 season
   aggregator to import at least two seasons that span multiple farms. Confirm
   deduplication, authoring metadata, and optimizer payload propagation match
   ADR-040 expectations.
2. **Session lifecycle rehearsal.** In the upgraded UI, rehearse
   `Start → Pause → Resume → Complete` flows with telemetry logging enabled.
   Verify the session timeline persists after application restart and that audit
   logs record state transitions.
3. **Automation safety net.** Validate that automation and scripting relying on
   implicit runs fails safe: warnings must be logged, and no destructive writes
   occur if the new session APIs are unavailable.

### Phase 2 — Data migration & seeding

1. **Create season definitions.** With the season aggregator CLI, seed seasons
   for the upcoming crop year. Capture change logs for audit and share IDs with
   analytics/reporting teams so filters can be updated ahead of rollout.
2. **Prepare job manifests.** For each job, record its target `seasonId` and the
   operators that should appear in the first migrated session. Store the mapping
   in source control or the deployment tracker to keep the rollout reproducible.
3. **Stage session migration.** Use the session migration utility (delivered in
   NX-334) or equivalent scripts to convert `job.json` to the ADR-041 schema in
   staging. Confirm `sessions[]` references the seeded season IDs and that
   coverage/log artefacts link to the right session documents.

### Phase 3 — Validation gates

1. **Regression replay.** Run the deterministic replay harness across migrated
   jobs to ensure coverage, telemetry, and analytics overlays remain identical to
   pre-migration baselines (± tolerance captured in ADR-020).
2. **Sync audit.** Execute a round-trip sync (offline → online) to ensure season
   metadata and session notes survive merges. Investigate any duplicate
   `jobIds` or dropped `extensions` payloads before promoting the build.
3. **Plugin sign-off.** Require each plugin owner to certify their lifecycle
   hooks using staging fixtures. Capture the results in the migration tracker and
   gate go/no-go on outstanding blockers.

### Phase 4 — Production rollout

1. **Pilot cohort.** Select a limited fleet and perform the migration during a
   low-risk window. Provide on-call coverage from Core, UI, and plugin owners.
2. **Field validation.** Complete at least one real session end-to-end and
   verify seasons appear in navigation, analytics filters, and exports. Capture
   screenshots/logs to use as regression references.
3. **General availability.** After the pilot, roll the migration out fleet-wide
   using the staged manifests. Enforce change freezes on season schemas until
   all clients confirm successful sync.

### Phase 5 — Post-migration stewardship

1. **Monitor telemetry.** Track session creation/closure latency, plugin
   acknowledgement times, and autosave error rates during the first two weeks.
   Regression thresholds should match ADR-041 rollout criteria.
2. **Feedback loop.** Collect operator and dealer feedback; file follow-up tasks
   for UX adjustments or plugin polish. Update this playbook with lessons
   learned.
3. **Archival policy review.** Ensure retention tooling is updated to archive at
   least three seasons offline as outlined in the SRS.

## Rollback & contingency plan

- **Session rollback:** If critical regressions appear, restore the last
  pre-migration backup of `job.json` and session artefacts, then disable the
  session-aware plugins in manifests. Document the incident and rerun Phase 1
  validation before attempting another rollout.
- **Season rollback:** In case seasons need to be withdrawn, remove season
  documents via the aggregator, clear cached season contexts on clients, and
  revert navigation toggles that surface season-first workflows.
- **Operator communication:** Keep templated status updates ready (pre-launch,
  active incident, resolved). Avoid ad-hoc messaging so the dealer network and
  support center share a single narrative.

## Checklist snapshot

| Milestone | Owner | Status notes |
| --- | --- | --- |
| Season aggregator deployed (NX-223) | Core lifecycle | |
| Session UX shipped (NX-295) | UI | |
| Plugin readiness confirmed | Plugin leads | |
| Seasons seeded in staging | Deployment lead | |
| Jobs migrated to session schema | Lifecycle tooling | |
| Replay + sync regression passed | QA | |
| Pilot cohort complete | Fleet ops | |
| Fleet-wide rollout complete | Deployment lead | |
| Post-migration review logged | Architecture guild | |

Keep the filled-in checklist with the deployment ticket and attach telemetry and
validation artefacts to satisfy ADR-040/041 audit requirements.

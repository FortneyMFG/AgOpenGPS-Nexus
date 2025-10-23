# NX-097 1.0 launch readiness review and sign-off checklist

The 1.0 launch gate combines packaging, validation, support, and communication sign-offs
into a single review so the release manager can issue a confident go/no-go decision. The
checklist anchors on the release orchestration scripts, packaging deliverables, and
operational playbooks already in the repository, ensuring the team verifies artefacts and
runbooks rather than recreating them ad hoc.【F:tools/ci/release.ps1†L4-L131】【F:README.md†L88-L99】【F:docs/Core/howto/installer-update-channels.md†L11-L85】

## Checklist structure

| Gate | Objective | Required evidence |
| --- | --- | --- |
| Packaging & artefacts | Release bundles are reproducible, signed (when certificates are available), and hashed for verification. | Release manifest, Windows/ Pi bundles staged by the release script, and checksum verification logs. | 
| Validation & QA | Bench, sim, and field validation all show green with archived artefacts. | Latest QA dashboard export, HIL/fault schedules, validated field checklist, and generated post-run report. |
| Support & communications | Downstream teams are ready to absorb the launch and react to regressions. | Field feedback snapshot, escalation roster updates, and community preview status summary. |
| Launch review | Final ceremony records the decision, owners, and rollback plan. | Signed review notes plus links to manifests, QA outputs, and support artefacts. |

## Packaging & artefact readiness

- [ ] Run `tools/ci/release.ps1` for the target version and capture the console output showing the staged `artifacts/release/<channel>/<timestamp>` directory and generated `manifest.json`. Attach the log to the checklist so reviewers can trace any signing warnings back to the script.【F:tools/ci/release.ps1†L69-L131】
- [ ] Verify SHA-256 hashes from the manifest against the staged Windows and Pi artefacts before promoting the build. Store the verification transcript alongside the manifest to prove the media is intact.【F:docs/Core/howto/installer-update-channels.md†L22-L85】
- [ ] Confirm the Windows single-file executable, ZIP, and installer bundle plus the Raspberry Pi Debian package are present and match the packaging expectations documented for operators. Record download locations for each so support can guide late adopters.【F:docs/Core/howto/installer-update-channels.md†L30-L117】

## Validation & QA sign-offs

- [ ] Attach the latest QA dashboard aggregate showing all tracked scenarios passing; include the command output (`qa dashboard aggregate`) and JSON artefact to keep the evidence reproducible.【F:docs/development/qa/qa-dashboard.md†L3-L33】
- [ ] Provide HIL run results and resolved fault injection schedules for the release candidate so reviewers can confirm deterministic benches remain green. Capture both the rig configuration references and generated JSON summaries.【F:docs/development/qa/hil-automation-rig.md†L3-L33】【F:docs/development/qa/fault-injection-harness.md†L3-L33】
- [ ] Include a validated field safety checklist covering the final release smoke run together with the QA console validation log, demonstrating operator sign-offs were recorded.【F:docs/development/qa/field-safety-checklist.md†L3-L41】
- [ ] Generate and archive the post-run Markdown report that stitches the checklist, metrics, and fault schedule together; link it in the review record for auditors.【F:docs/development/qa/post-run-report.md†L3-L33】

## Support & communication readiness

- [ ] Publish the latest field feedback telemetry snapshot so the review can confirm no outstanding critical regressions are trending upward. Summaries from the support CLI must be attached to the ticket.【F:docs/Core/support/field-feedback-telemetry.md†L3-L37】
- [ ] Document the current escalation roster, outstanding tickets, and update cadence from the dealer escalation playbook so leadership understands how issues will be triaged after launch.【F:docs/Core/support/dealer-escalation-process.md†L3-L38】
- [ ] Capture community preview metrics, survey sentiment, and exit-criteria status to prove the candidate has soaked without critical regressions before graduation.【F:docs/Core/support/community-preview-program.md†L17-L33】

## Launch review ceremony

- [ ] Schedule the cross-team review with core, AGiO, plugins, UI, QA, and support leads. Record attendees, decision, and any conditions (e.g., staged rollback builds) directly in the checklist package so the outcome is auditable.【F:README.md†L88-L99】【F:docs/Core/support/dealer-escalation-process.md†L26-L38】
- [ ] Archive the signed checklist, manifest, QA artefacts, and support summaries in the release folder (`artifacts/release/<channel>/<timestamp>`) to preserve a single source of truth for the launch decision and future audits.【F:tools/ci/release.ps1†L69-L131】【F:docs/Core/howto/installer-update-channels.md†L22-L85】

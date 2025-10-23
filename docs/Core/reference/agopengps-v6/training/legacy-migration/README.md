# Legacy migration training set (NX-086)

This package equips instructors with classroom and hands-on material for
teaching the legacy-to-Nexus migration workflow. Pair it with the
[legacy migration guide](../../legacy-migration-guide.md) when planning a
field rollout.

## Contents

| File | Purpose |
| --- | --- |
| [`agenda.md`](agenda.md) | Timetable for the classroom and lab sessions. |
| [`instructor-script.md`](instructor-script.md) | Talking points that reinforce the slides and demos. |
| [`hands-on-checklist.md`](hands-on-checklist.md) | Step-by-step operator exercise using sample data. |
| [`knowledge-check.md`](knowledge-check.md) | Quiz used to confirm readiness before deployment. |

Print or export to PDF before travelling; on-site networks are often unreliable.

## Required resources

- Laptop with the latest Nexus build and `legacy-tool` CLI installed.
- Bench harness or simulator capable of replaying the `legacy-auto-run`
  scenario pack.
- Translated sample assets produced by `legacy-tool translate`.
- Projector or large display for group walkthroughs.

## Delivery tips

- Keep lab groups to three people or fewer so each operator can drive a portion
  of the simulation run.
- Capture soak reports and quiz results per attendee and upload them to the
  regional deployment tracker after class.
- Encourage operators to bring their own legacy configuration snapshots; it
  makes the exercises more relevant and uncovers edge cases early.

# Instructor script

## Opening (0–10 min)

- Introduce the rollout objective: retire AgOpenGPS V6 on targeted machines by
  the end of the season.
- Emphasize safety expectations: always perform migrations with a second person
  present, use the bench harness first, and log every soak run.
- Preview the agenda and point participants to the printed checklist and quiz.

## Tooling overview (10–30 min)

- Demo `legacy-tool translate --help`, highlighting required parameters and the
  warnings emitted for incomplete legacy profiles.
- Show the `legacy-tool soak` report structure. Connect the metrics to the
  90 Hz UDP rate and explain how frame mismatches affect steering stability.
- In Nexus, open the legacy import wizard and demonstrate how the preview pane
  surfaces geometry issues before import.

## Migration workflow deep dive (30–55 min)

- Map each legacy artifact (machine XML, AB lines, coverage exports) to its
  Nexus counterpart using the diagram in the guide.
- Stress the importance of archiving translation outputs for traceability.
- Call out common pitfalls:
  - Legacy sections missing width metadata → fix in V6 before translation.
  - Old firmware revisions → update before running the soak harness.
  - Operators skipping the simulator run → require sign-off with the checklist.

## Hands-on lab (65–140 min)

- Assign roles (driver, scribe, spotter) for each group of attendees.
- Walk through the `hands-on-checklist.md`, pausing after each major milestone
  (translation, soak validation, import) to answer questions.
- Encourage teams to repeat steps with their own legacy data if time allows.
- Capture screenshots of successful imports and soak reports for the final wrap-
  up.

## Wrap-up (140–150 min)

- Distribute the `knowledge-check.md` quiz and collect responses.
- Review next steps: schedule on-farm migrations, share local support contacts,
  and remind attendees to upload their artifacts to the deployment tracker.

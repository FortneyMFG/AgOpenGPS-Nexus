# Legacy migration guide & training set (NX-086)

This guide documents the recommended path for moving machines and operators from
AgOpenGPS V6 to Nexus. It stitches together the tooling delivered in NX-083
through NX-090 and packages a field-ready training set so teams can rehearse the
migration before touching production hardware.

## Audience and prerequisites

- **Audience:** Field operators, dealer technicians, and advanced growers who
  currently rely on AgOpenGPS V6 and are preparing to deploy Nexus.
- **Required tooling:**
  - `legacy-tool` from the Nexus tooling bundle (NX-084).
  - `nexus` desktop build with the legacy import wizard enabled (NX-083).
  - Bench harness or simulator capable of replaying captured UDP traffic.
- **Input data:** Copy the legacy machine XML, `*.txt` field geometry exports,
  and at least one recorded UDP soak captured from the in-cab controller.

## Migration workflow

1. **Baseline the legacy install**
   - Capture screenshots of the V6 steering, sections, and machine dialogs.
   - Export the latest AB lines, boundaries, and headlands from the V6 field
     directory. Ensure the directory snapshot is read-only before continuing.
   - Record current firmware versions and confirm the bench harness matches the
     in-field wiring.
2. **Translate configuration with `legacy-tool translate`**
   - Run `legacy-tool translate --machine ./Machine.xml --fields ./Fields` to
     produce a `machine-profile.json` and `field-assets/` folder.
   - Archive the CLI output alongside the original XML/TXT files. The JSON
     bundle is the source of truth for Nexus after migration.
   - Review warnings emitted by the translator. Address missing offsets or
     duplicate sections inside the legacy configuration before retrying.
3. **Validate IO timing with `legacy-tool soak`**
   - Replay a 30-second UDP capture on the bench and run
     `legacy-tool soak --seconds 30 --report soak.json`.
   - Confirm pose, steering command/state, and section frames report matched
     totals. Investigate discrepancies before moving to the live machine.
   - File the soak report with the migration ticket so QA can audit the run.
4. **Import geometry via the Nexus wizard**
   - Launch Nexus and open **File → Import legacy assets…**.
   - Select the translated `field-assets/` folder. Preview each boundary and
     AB line to verify headings, nudge offsets, and curvature against the
     original V6 screenshots.
   - Save the imported fields to a staging workspace and run a short simulated
     path using the `legacy-auto-run` scenario pack (NX-090).
5. **Load the machine profile**
   - In **Settings → Machines**, choose **Import → Legacy translation** and pick
     the `machine-profile.json` emitted earlier.
   - Verify hitch lengths, implement dimensions, and section timing match the
     legacy screenshots. The tuning wizard pre-populates steering gains using
     NX-058 so only minor adjustments should be required.
6. **Field validation and sign-off**
   - Perform a guided drive in manual mode to confirm GNSS, sections, and
     hydraulic controls respond correctly.
   - Enable auto-steer, complete a short AB pass, and compare coverage with the
     translated headland/boundary layers.
   - Collect a fresh soak log and coverage export for post-run validation.

## Training set contents

The `docs/training/legacy-migration` folder packages a turnkey curriculum for
teaching the workflow above:

- **Agenda:** 90-minute classroom plus 60-minute hands-on lab covering tooling,
  translation, and validation steps.
- **Instructor script:** Talking points that align with the slides and live
  demos. Use it to keep parallel sessions consistent across regions.
- **Hands-on checklist:** Step-by-step exercise that walks trainees through the
  CLI translation, soak validation, and Nexus import workflow using bundled
  sample data.
- **Knowledge check:** Five-question quiz operators complete before receiving
  production credentials.

Clone or print the materials before onsite training. Update the agenda with
local break schedules, and record attendance in the regional deployment log.

## Supporting resources

- Dealer deployment toolkit (NX-087) for staging USB media.
- Legacy auto-run scenario pack (NX-090) for validation inside the simulator.
- Coverage analytics parity harness (NX-055) to verify post-run exports.

These resources keep migrations predictable, auditable, and aligned with the
field-readiness criteria captured in the SRS.

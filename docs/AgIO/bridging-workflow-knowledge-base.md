# NX-093 Bridging workflow knowledge base

Bridging keeps legacy AgOpenGPS (V6/Teensy) installs productive while operators adopt Nexus. This
knowledge base distils the scripts, checklists, and troubleshooting patterns that support teams use
when coaching dealers and growers through mixed deployments. Firmware and transport requirements
remain governed by [SRS §53 AOG-Link Compatibility](../SRS/sections/5X_Hardware_IO_Device_Layer/53_AOG_Link_Compatibility.md)
and the legacy PGN catalog in [the AgIO reference appendix](../SRS/references/AgIO_PGN_Baseline.md).

## Audience and prerequisites

- **Primary users** — Dealer support teams, regional agronomy specialists, and Nexus success
  engineers responding to mixed-fleet questions.
- **Prerequisites** — Familiarity with the legacy AB guidance model, UDP/serial wiring for Teensy
  bridges, and access to Nexus tooling (`nexus run core`, `nexus run agio`, `nexus sim`).
- **Related docs** — Review the legacy migration guide (NX-086), multi-machine sync workflow (NX-088),
  and legacy auto-run scenarios (NX-090) for background and reproducible fixtures.

## Core bridging workflows

### 1. Stand up a Nexus-to-Teensy bridge

1. **Validate firmware** — Confirm the Teensy reports the supported firmware hash from the UDP
   discovery (NX-080). Outdated firmware must be reflashed before proceeding.
2. **Provision bridge profile** — Translate existing machine profiles with the legacy configuration
   CLI (NX-084). Inspect the generated Nexus profile for steer, section, and rate controller
   parity.
3. **Launch bridge services** — Start `nexus run core` and `nexus run agio --profile <machine>` on
   a diagnostic laptop. Ensure the PGN bridge plugin is enabled and advertising the correct UDP
   endpoints.
4. **Handshake verification** — Watch the capabilities exchange logs for ACKs on steer, section,
   and timing (NX-005/NX-028). Capture screenshots for the support ticket before handing control
   to the operator.

### 2. Synchronise guidance and coverage data

1. **Import guidance lines** — Use the legacy AB import wizard (NX-083) or the coverage analytics
   CLI (NX-055) to seed Nexus with historic AB lines, curves, and headlands.
2. **Backfill coverage** — Replay legacy logs through the replay plugin (NX-035) so Nexus can render
   coverage parity for the current field. Verify headland cutoffs visually in the UI map panel.
3. **Schedule sync jobs** — Document the nightly sync cadence (NX-088) and confirm cloud relays or
   NAS targets are reachable from the field office.

### 3. Run mixed-mode operations (legacy steer, Nexus sections)

1. **Split responsibilities** — Configure the route binder so the legacy steer loop remains active
   while Nexus sections drive rate control. Flag this configuration in the machine profile notes.
2. **Latency budget** — Monitor timing capsules (NX-066) to verify <30 ms round-trip latency for
   section commands. If exceeded, downgrade map detail or remove non-essential plugins.
3. **Operator briefing** — Provide the mixed-mode quick reference card (template in
   `docs/templates/operator-briefing.md`) and record the acknowledgement in the deployment
   ticket.

## Troubleshooting playbooks

| Symptom | Probable cause | Resolution |
| --- | --- | --- |
| UDP discovery succeeds but steer does not engage | PGN bridge plugin disabled or ports blocked | Validate `plugins.json` on the bridge host and confirm firewall openings on 8888/8889. |
| Sections lag behind machine motion | Telemetry sampling at 10 Hz cap | Increase sampling to 20 Hz in the Nexus profile and retune the rate controller gains (NX-053). |
| Teensy reboots when Nexus connects | Firmware mismatch or 5 V brownout | Reflash to the supported hash and confirm a dedicated 5 V regulator supplying ≥1 A. |
| Coverage sync shows gaps | Legacy logs missing GNSS fix quality | Inspect the replay harness report; request replacement logs or mark the coverage as estimated in the ticket. |
| Mixed-mode operator confused about control transfer | Briefing skipped | Schedule a follow-up call, deliver the operator briefing, and update the deployment checklist with sign-off. |

## Logging and escalation

1. **Capture artefacts** — Collect the Nexus diagnostic bundle (`nexus support bundle --bridge`)
   alongside Teensy serial captures. Store bundles under the ticket ID in the shared support drive.
2. **Timeline updates** — Post progress summaries every 24 hours for critical issues and every
   48 hours for warnings, aligning with the dealer escalation process (NX-095).
3. **Hand-off criteria** — Escalate to the feature owner when PGN parity diffs persist after replay
   validation or when latency exceeds the safe operating envelope for more than 10 minutes.

## Continuous improvement

- Record new troubleshooting patterns in the shared FAQ sheet and backfill this knowledge base
  monthly.
- Link resolved tickets to telemetry signatures so automated detectors can flag regressions early
  (NX-094).
- Coordinate with the community preview program (NX-096) to pilot improved bridge defaults before
  distributing to all dealers.

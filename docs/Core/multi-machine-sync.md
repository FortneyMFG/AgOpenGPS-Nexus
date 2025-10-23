# Multi-machine synchronization workflow (NX-088)

> **Audience:** Dealer technicians and fleet operators who stage more than one Nexus
> machine at a time.
> **Outcome:** Every machine leaves the shop with a consistent identity and a repeatable
> process for exchanging work data even when rigs stay offline for weeks.

Nexus is distributed under a permissive open-source license. There is no activation or
seat enforcement layer to worry about; the "fleet manifest" files described below exist
only to keep operator notes, machine IDs, and configuration metadata in sync across a
fleet. Treat them like configuration rather than DRM. Pair this workflow with the
[offline update channel guide](installer-update-channels.md) so staged media and nightly
sync archives stay aligned with the channel assignments you deploy. Normative policies
for collaborative sessions and lifecycle events live in the
[SRS UI layout section](../SRS/sections/9X_Frontends_Ops/91_UI_Shell_Layout.md) and
[Job Sessions lifecycle ADR](../SRS/sections/6X_Core_Domain_Services/62-ADR-041%20-%20Job%20Sessions%20Lifecycle.md).

## Workflow roles and artifacts

| Role | Responsibilities | Key artifacts |
| --- | --- | --- |
| Dealer staging tech | Builds the portable media, preloads fleet identities, and seeds the sync anchor before trucks roll out. | Dealer toolkit bundle, fleet manifest (`fleet/manifest.json`), sync seed (`sync/seed.json`). |
| Lead machine ("anchor") | First machine configured at the customer site. Owns the authoritative fleet manifest and publishes nightly sync snapshots. | `%PROGRAMDATA%/AgOpenGPS/Nexus/fleet/manifest.json`, `%PROGRAMDATA%/AgOpenGPS/Nexus/sync/outbox/`. |
| Satellite machines | Consume sync snapshots and contribute their own coverage and guidance updates back to the anchor. | `%PROGRAMDATA%/AgOpenGPS/Nexus/fleet/machine-<id>.json`, `%PROGRAMDATA%/AgOpenGPS/Nexus/sync/inbox/`. |

The dealer toolkit from NX-087 already creates the staging folder that travels with the
installer.【F:docs/howto/dealer-deployment-toolkit.md†L1-L54】 Extend that bundle with a
`fleet/` directory that holds one machine manifest per cab and a `sync/` directory
containing the initial field profiles you want preloaded. The
[season/session migration playbook](season-session-migration-playbook.md) offers scripts
for translating legacy machine profiles before you drop them into the sync seed.

## Pre-flight checklist

Complete these items before leaving the staging bench. Doing so keeps the
[legacy migration guide](../reference/agopengps-v6/legacy-migration-guide.md) workflow aligned with the synced
machine identities when you upgrade existing fleets:

1. Generate the machine manifest files from the toolkit helper (`dealer-toolkit fleet export`).
   Name them `machine-<serial>.json` so they line up with the stickers on each cab
   controller.
2. Drop the manifest files into `fleet/` alongside the Nexus installers in the dealer
   toolkit output.【F:docs/howto/dealer-deployment-toolkit.md†L13-L54】 The same tool writes
   `fleet/manifest.json`, which simply lists every machine ID and which one should start as
   the anchor.
3. Export translated machine profiles (`legacy-tool translate`) and any baseline guidance
   lines into `sync/seed/` so every machine starts with identical data.【F:docs/howto/dealer-deployment-toolkit.md†L38-L54】 Use
   the [Pi simulation helper](pi-sim.md) when you need to verify legacy AB lines before
   travelling.
4. Print the delivery checklist from the toolkit bundle and annotate which manifest goes
   with which physical machine to avoid swapping identities in the field.【F:docs/howto/dealer-deployment-toolkit.md†L30-L54】

## Step 1 — Configure the lead machine

1. Pick the machine that will act as the sync anchor (usually the first unit you power on
   in a fleet). Copy the entire `fleet/` and `sync/` folders from the dealer media to a
   temporary folder on the machine.
2. Place the anchor's `machine-<serial>.json` under
   `%PROGRAMDATA%/AgOpenGPS/Nexus/fleet/machine-anchor.json` and copy the remaining machine
   manifests into the same directory for later distribution.
3. Launch Nexus Core. During startup it reads every `fleet/machine-*.json` file, validates
   that the anchor ID matches `fleet/manifest.json`, and writes
   `fleet/manifest.cache.json` summarising the configured fleet. Confirm the log shows the
   anchor machine as `FleetAnchorConfirmed` before proceeding. Capture a snapshot for the
   [dealer escalation playbook](../support/dealer-escalation-process.md) so support teams can
   quickly audit fleet assignments if issues arise.
4. Seed the sync area by copying the contents of `sync/seed/` into
   `%PROGRAMDATA%/AgOpenGPS/Nexus/sync/inbox/`. The Core service will ingest the inbox and
   mirror it into `sync/outbox/anchor-<date>.zip` so satellites can pull the same baseline
   dataset.

## Step 2 — Distribute machine identities

1. On each remaining machine, copy **only** its assigned `machine-<serial>.json` file into
   `%PROGRAMDATA%/AgOpenGPS/Nexus/fleet/`. Leave the anchor's manifest untouched.
2. Start Nexus Core and watch for the `FleetMachineRegistered` entry. The log also records
   the serial so you can double-check the right file landed on the right machine. Record the
   timestamp alongside your [field feedback telemetry snapshot](../support/field-feedback-telemetry.md)
   so weekly rollups show which rigs received fresh identities.
3. Delete the copied manifest file from the portable media after registration to avoid
   accidentally reusing it on another fleet.

## Step 3 — Exchange nightly sync snapshots

Even fully offline fleets can stay aligned by rotating the dealer media (or any rugged USB
stick) between machines.

1. **From the anchor:** After each shift, run `sync-export.ps1` (Windows) or
   `sync-export.sh` (Linux) from the dealer toolkit bundle. The script copies the latest
   `sync/outbox/*.zip` into `sync/drop/` on the removable drive and refreshes the manifest.
2. **At each satellite:** Plug in the drive, run `sync-import.ps1`/`sync-import.sh`, and
   confirm it reports `In sync with anchor <timestamp>`. The script unpacks the snapshot
   into `sync/inbox/` so Core ingests new AB lines, coverage logs, and machine notes the
   next time it starts.
3. **Return path:** When the satellite finishes work, the import script also packages its
 local changes into `sync/outbox/<machine-id>-<timestamp>.zip`. Drop the drive back at the
  anchor and run `sync-collect` so the anchor manifest pulls in the updates and merges them
  into the next nightly snapshot.

Preview fleets participating in the
[community preview program](../support/community-preview-program.md) should attach the
sync drop timestamps to their weekly survey so telemetry comparisons account for when rigs
received updated guidance or coverage archives.

## Step 4 — Auditing and recovery

- The anchor's `fleet/manifest.cache.json` is the authoritative record. Back it up weekly so
  you can restore identities if a cab PC fails.
- If a machine is replaced, retire its identity by deleting the corresponding
  `fleet/machine-*.json` file and running `sync-export` once more. The next import removes
  the retired machine from other cabs.
- Keep at least two historical sync archives on the removable drive. If a sync merge goes
  wrong, delete the latest inbox contents and re-import the prior archive to roll back.

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| `Fleet manifest mismatch` in the log | Wrong machine serial embedded in the manifest. | Regenerate the manifest with the correct serial or rename the file to match. |
| Sync scripts fail with `manifest missing` | Dealer toolkit bundle was copied without the `sync/` folder. | Re-run the toolkit packaging (NX-087) and ensure the sync seed is present before traveling. |
| Satellite shows stale AB lines | Import was skipped or failed silently. | Re-run `sync-import`, confirm the timestamp matches the anchor's, and review the Core log for `SyncApplied` entries. |
| Anchor manifest lost after disk swap | `%PROGRAMDATA%` was wiped during OS reinstall. | Restore the latest `fleet/manifest.cache.json` and the most recent sync archive from the removable drive before restarting Nexus. |

This workflow keeps machine identities tightly scoped per cab while preserving an offline,
repeatable sync loop that satisfies the legacy compatibility goals in SRS §4.3.

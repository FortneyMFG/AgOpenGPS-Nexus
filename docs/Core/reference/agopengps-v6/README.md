# AgOpenGPS V6 Reference Packet

This packet consolidates the research, parity studies, and rollout aids that
bridge the legacy AgOpenGPS V6 suite into Nexus. Use it when planning migrations,
verifying parity, or studying the WinForms-era guidance stack.

## Migration Playbooks

- [Legacy migration guide](legacy-migration-guide.md) — end-to-end field upgrade
  workflow for retiring V6 installations.
- [Training curriculum](training/legacy-migration/README.md) — instructor agenda,
  hands-on checklist, and knowledge check for operator training.
- [Legacy auto-run scenario pack](scenarios/legacy-auto-run/README.md) — simulator
  preset and soak reports that mirror the UDP validation harness.

## Porting and Parity Notes

- [AutoSteer-Lite tuning reference](porting/AutoSteerLite-Tuning.md) — heuristics
  used when translating steering gains from V6 profiles.
- [Legacy data ingest workbook](porting/LegacyDataIngest.md) — CLI import flow and
  validation harness notes.
- [V6 functionality gap analysis](porting/V6-Functionality-Gap-Analysis.md) —
  outstanding capability differences between V6 and Nexus.
- [V6 inventory](porting/V6-Inventory.md) — catalog of WinForms dialogs,
  configuration files, and transport flows.
- [Ported math verification](porting/PortedMathVerification.md) — coverage,
  section control, and steering parity results.

## Guidance Study

- [V6 guidance deep dive](v6-study/00_index.md) — indexed study capturing the
  toolbar, planner, and autosteer behaviour in the legacy stack.

Keep this index synchronized as additional V6 research or migration aids are
added to the packet.

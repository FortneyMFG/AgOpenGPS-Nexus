# AgOpenGPS V6 Reference Packet

This packet concentrates everything harvested from the legacy V6 code line so
porting and parity teams can track what still needs to be replicated inside
Nexus.

## Porting & Translator Notes

- [AutoSteer Lite tuning](porting/AutoSteerLite-Tuning.md) — Mirrors the V6
  heuristic profile so the Nexus plugin matches look-ahead and ramp behaviour.
- [Legacy data ingest](porting/LegacyDataIngest.md) — Documents how field
  folders, logs, and settings are translated into Nexus workspace assets.
- [Ported math verification](porting/PortedMathVerification.md) — Captures the
  numerical checks used when moving guidance and coverage algorithms across.
- [V6 functionality gap analysis](porting/V6-Functionality-Gap-Analysis.md) —
  Summarises the remaining UX and systems gaps versus Nexus plans.
- [V6 inventory](porting/V6-Inventory.md) — Enumerates the dialogs, telemetry,
  and data exports extracted from the WinForms host.

## Guidance Study Excerpts

The [V6 guidance study](v6-study/00_index.md) breaks down the legacy guidance
stack so planners can stage regression harnesses and operator documentation:

- Field creation, line families, and toolbar interactions.
- Coverage accounting, autosteer handshake, and controller error terms.
- Boundary/headland semantics, edge-case handling, and glossary references.

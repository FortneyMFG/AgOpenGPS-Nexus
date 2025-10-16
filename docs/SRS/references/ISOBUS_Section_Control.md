# ISOBUS-inspired section control PGNs

## Context
Community discussions (Feb 2024) highlighted the need to harmonize AgOpenGPS machine control PGNs with widely used ISOBUS/J1939
patterns so future integrations (e.g., PR 607) can reuse documented DDI semantics instead of bespoke message layouts.

## Key proposals
- **Desired section states** should follow ISOBUS condensed work state setpoints: use PGN 290 for sections 1-16 and cascading
  PGNs (291+) for additional sections. Each PGN packs two bits per section, mirroring the ISO DDI 290 definition so third-party
  controllers can decode boom commands consistently.
- **Actual feedback states** should be broadcast by the machine using PGN 161 (and 162+ for rigs with more than 16 sections). If
  no update arrives within a timeout window, AgOpenGPS should revert to local paint heuristics just as it does today, ensuring a
  safe fallback.
- **Operator overrides and master work switch** should use the aligned condensed state PGNs: PGN 367 for per-section overrides,
  PGN 289 for master setpoint, and PGN 141 for master actual feedback. These mirror the DDEntity catalog so button panels and
  rate controllers can interoperate.
- **DDI-based payload structure**: reuse the ISOBUS byte layout with command, element number, and DDI identifiers to keep units
  and scaling consistent across vendors. The UDP/serial framing (0x80, 0x81 … CRC) can remain, but the payload bytes adopt the
  ISOBUS semantics for clarity and tooling compatibility.

## Rationale
- Reduces duplicated documentation by pointing to the ISO 11783 (ISOBUS) Data Dictionary (over 600 DDI entries).
- Gives firmware authors a consistent template when adding features like turn-on/off delays (DDI 205/206) or section offsets
  (DDI 134) without inventing new PGNs.
- Keeps legacy PGNs alive during migration while documenting the preferred replacements so implementers can plan staged
  rollouts.

## Outstanding questions
- What timeout/window should AgOpenGPS enforce before treating feedback as stale when a condensed work state PGN is missed?
- Can the microcontroller toolchain efficiently parse the command/element/DDI tuple without blowing RAM/flash budgets?
- Should the AgIO CRC be retained even though UDP already carries a checksum, to preserve serial compatibility?

## Related ADRs

- [ADR-006 — AgIO Link MCU Communications](../../ADR/ADR-006-aog-link-mcu-communications.md)
- [ADR-015 — Section Control Grouping Semantics](../../ADR/ADR-015-section-control-grouping-semantics.md)
- [ADR-048 — RadioBridge](../../ADR/ADR-048_RadioBridge.md)

# Metadata-driven UI style guide (NX-326)

ADR-034 shifts Nexus dashboards, inspectors, and legends to a metadata-driven
model so the same widgets render consistently across desktop and companion
clients. NX-326 codifies the styling guidance for those widgets so plugin teams
can ship new layouts without breaking parity or accessibility guarantees. The
normative component requirements and token catalog now live in the SRS — review
[ADR-034](../development/SRS/sections/9X_Frontends_Ops/91-ADR-034%20-%20Metadata-driven%20dashboards%20and%20inspector%20surfaces.md)
and the companion samples under `docs/development/SRS/appendices/samples/plugins` before shipping
changes. Treat this document as an operational checklist for applying those
specs.

## Design principles

1. **Metadata first.** All visual decisions flow from metadata provided by the
   layer registry, controllers, or presets. Hard-coded colours, ranges, or
   display strings are prohibited unless explicitly negotiated in the metadata
   schema.
2. **Composable cards.** Dashboards and inspectors assemble reusable card
   components that scale from 320&nbsp;px-wide mobile panes to 1440&nbsp;px desktop
   canvases by adjusting padding and typography tokens.
3. **Remote parity.** Every layout must render identically on Avalonia desktop
   and remote companions. The new `CompanionMetadataSnapshot` type backs this
   promise by sharing legend, inspector, dashboard, and replay metadata with
   mobile shells.
4. **Contrast & motion discipline.** Widgets obey the shared theme palette and
   avoid gratuitous animation. Motion is limited to data changes (sparkline
   updates, section reveals) and respects reduced-motion settings.

## Layout tokens

- Source all spacing, radius, and typography from
  [`artifacts/ui-theme-tokens.json`](../../artifacts/ui-theme-tokens.json). Keep
  PRs limited to that artifact (and the SRS appendices) when adjusting values so
  consumers track diffs centrally.
- Mirror companion overrides by importing the `Companion` token set documented
  in the SRS appendix instead of hard-coding alternate values in views.
- Metadata that supplies colours (e.g., legend gradients) must provide
  `#AARRGGBB` values so the same appearance can be reproduced on the web or
  native shells.

## Legend & inspector styling

- Legend entries show gradients with a 4 px radius swatch and two-line layout:
  primary label and range display on the first row, mode badge and optional
  description on the second.
- Inspector cards reserve the top row for the primary value and target. Secondary
  rows follow the order: quality, weight, world location, timestamp, source, and
  rate availability.
- Transport and payload metadata lists use 12 px fonts and inherit the
  low-contrast foreground brush; they collapse into accordion-style sections on
  narrow screens.

## Dashboard styling

- Sparklines use 2.5 px strokes on desktop and 2.0 px on mobile, with 8 px top
  and bottom padding to avoid clipping.
- PID tuning sliders surface labels (`P`, `I`, `D`) left-aligned with value
  readouts on the right. Slider handles expand to 32 px hit targets on touch
  devices.
- Status banners (e.g., "Cross-track error 0.04 m") adopt the accent colour when
  the reported metric exceeds ADR-034 guardrails; otherwise they remain in the
  medium-contrast foreground brush.

## Accessibility checklist

- Minimum colour contrast ratio of 4.5:1 for body text and 3:1 for large text or
  icon-only buttons.
- Focus order mirrors the logical reading order and remains identical between
  desktop and companion builds.
- Every chart or gradient conveys the same information through text (range
  display, numeric values) so colour-blind operators are not disadvantaged.
- Motion respects OS-level reduced-motion settings by disabling sparkline
  transition animations.

## Review workflow

1. **Design review.** Attach screenshots (desktop + companion) and the emitted
   `CompanionMetadataSnapshot` JSON to the PR description. Design leads confirm
   parity and accessibility.
2. **QA checklist.** Run `dotnet test` for `Aog.UI.Avalonia.Tests` and capture
   golden renders using the dashboard automation harness from NX-301.
3. **Documentation update.** When introducing new metadata fields, extend the
   relevant ADR or schema docs and refresh this style guide if tokens change.
4. **Release note.** Summarise user-facing changes in the seasonal release note
   and link to updated screenshots in the golden pack.

Following this guide keeps ADR-034's metadata-driven UI strategy aligned across
platforms while giving plugin teams a predictable framework for new widgets.

# Zone Policy Operator Guide

This guide explains how Nexus surfaces spatial constraint policies to operators and how those
policies influence automation behaviour in the field. It complements ADR-027 and the UI
policy toggles implemented in the Avalonia shell so crews know when automation is gated and
what steps are required to resume work.

## Constraint states surfaced to the operator

Nexus evaluates each pose sample against boundary, headland, keep-out, and work-disabled
zones. The resulting constraint gate snapshot exposes whether autosteer and product control
are currently allowed, alongside the active zone identifiers.【F:Nexus SourceCode/src/Aog.Core/Safety/ControlArbiter.cs†L174-L213】
Operators should watch for the following annunciators:

- **Boundary awareness:** Shows when the implement footprint is inside the field boundary
  or headland buffers. Guidance line trimming and cost maps use this to smooth headland
  transitions.【F:Nexus SourceCode/src/Aog.Core/Safety/ControlArbiter.cs†L200-L209】
- **Keep-out gate:** Triggered when a keep-out zone intersects the footprint. Autosteer is
  disabled and section outputs are forced off until the machine leaves the zone or an
  override is authorised.【F:Nexus SourceCode/src/Aog.Core/Safety/ControlArbiter.cs†L186-L199】
- **Work-disabled gate:** Allows driving but forces product control and variable-rate
  outputs off while logging the affected zones for audit purposes.【F:Nexus SourceCode/src/Aog.Core/Safety/ControlArbiter.cs†L186-L205】

The Avalonia shell mirrors these states via the Zone Constraint Policy panel so operators can
acknowledge overrides and review the history of manual actions.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/ZoneConstraintPolicyViewModel.cs†L132-L205】

## Responding to gate events

1. **Stay clear of keep-out regions.** Automation remains disengaged while the gate is active.
   If the zone was entered intentionally (e.g., to inspect an obstacle), re-engage autosteer
   only after backing out of the zone and confirming the annunciator has cleared.
2. **Work-disabled areas:** Product control remains off but steering can continue. Plan
a return path that respects the regulatory buffer before re-arming sections.
3. **Manual overrides:** When policy allows, the operator can apply a temporary override via
the Zone Constraint Policy panel. Overrides are timestamped and recorded in the history log
for later review.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/ZoneConstraintPolicyViewModel.cs†L72-L108】

Always note the rationale for an override in the job log so agronomy and compliance teams
understand why automation was bypassed.

## Importing and validating zones

Use the Zone Import & Export panel to bring in shapefiles, GeoPackages, or ISOXML bundles.
Each workflow runs validation passes that check geometry integrity, buffer configuration, and
policy compatibility before applying the catalog update.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/ZoneImportExportPanelViewModel.cs†L18-L99】
When the import completes, review the activity log and ensure the newly imported zones appear
with the expected labels and priorities.

## Field-readiness checklist

Before entering the field:

- Confirm the boundary, headland, keep-out, and work-disabled zones are present and enabled
  for the job.
- Review the constraint policy toggles and ensure no overrides are left active from prior
  sessions.【F:Nexus SourceCode/src/Aog.UI.Avalonia/ViewModels/ZoneConstraintToggleViewModel.cs†L45-L120】
- Perform a short simulated pass to trigger the constraint gate and verify the annunciators
  clear when exiting a protected zone.

Following this checklist keeps automation trustworthy while preserving an auditable record of
policy decisions for each session.

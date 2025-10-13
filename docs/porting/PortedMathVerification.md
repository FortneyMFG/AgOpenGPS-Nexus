# Ported Math Verification Report

Task NX-059 documents the parity checks that confirm Nexus reproduces the legacy AgOpenGPS V6 math that was ported in NX-051 through NX-058. This report captures the regression datasets, tolerances, and observed deltas so future changes can track parity drift.

## Coverage Analytics Parity

The `CoverageAnalyticsParityHarness` compares CSV exports generated from the legacy coverage pipeline and the Nexus implementation. The data set exercises painted area aggregation, user-selected area, coverage percentage computation, and patch counting.

| Metric | Legacy | Nexus | Absolute Δ | Relative Δ | Harness Tolerance |
| --- | --- | --- | --- | --- | --- |
| TotalAreaSquareMeters | 248.00 | 248.05 | +0.05 | +0.020% | 0.10 |
| UserAreaSquareMeters | 200.00 | 199.98 | −0.02 | −0.010% | 0.10 |
| CoveragePercent | 62.00 | 62.05 | +0.05 | +0.081% | 0.10 |
| PatchCount | 3 | 3 | 0 | 0.000% | 0.10 |

- With a tolerance of ±0.10, all metrics pass; tightening the tolerance to ±0.01 intentionally flags the area metrics to prove the harness alerts on drift.【F:Nexus SourceCode/tests/Aog.Core.Tests/Coverage/CoverageAnalyticsParityHarnessTests.cs†L15-L42】
- Metric names must match exactly between CSVs; mismatches fail fast so new metrics cannot silently drop from reports.【F:Nexus SourceCode/tests/Aog.Core.Tests/Coverage/CoverageAnalyticsParityHarnessTests.cs†L44-L66】
- Regression datasets live alongside the tests for quick refreshes when new parity captures are added.【F:Nexus SourceCode/tests/Aog.Core.Tests/Coverage/Data/CoverageParityLegacy.csv†L1-L6】【F:Nexus SourceCode/tests/Aog.Core.Tests/Coverage/Data/CoverageParityNexus.csv†L1-L6】

## Section Control Rate Mask Validation

`SectionMaskCalculator` drives section enablement, honoring speed gating, look-ahead timing, and manual suppression. The parity scenarios replay recorded legacy states to confirm bitmask outputs match.

| Scenario | Behaviour Exercised | Expected Mask |
| --- | --- | --- |
| `basic_pass` | Nominal forward pass with outer sections active | `0b00101` (5) |
| `speed_gate` | Speed below minimum so all sections disabled | `0b00000` (0) |
| `lookahead` | Pending look-ahead activation toggles centre section | `0b00001` (1) |
| `suppressed` | Manual suppression blocks the trailing section | `0b00010` (2) |

All scenarios resolve to the same masks as the V6 recorder, demonstrating parity across the different gating paths.【F:Nexus SourceCode/tests/Aog.Plugins.Tests/Sections/RateControlParityValidationTests.cs†L13-L84】【F:Nexus SourceCode/tests/Aog.Plugins.Tests/Sections/Data/LegacyRateControlScenarios.csv†L1-L5】 Additional scenarios can be appended to the CSV without code changes to expand coverage.

## AutoSteer-Lite Tuning Regression

The `AutoSteerLiteTuningCalculator` rebuilds V6’s heuristic look-ahead and steering gains from a translated `MachineProfile`.

- Default tractor and toolbar dimensions round-trip through the translator and tuning calculator with look-ahead hold, speed multiplier, acquire factor, and minimum look-ahead distances staying within the legacy bands (±0.2 for multipliers).【F:Nexus SourceCode/tests/Aog.Plugins.Tests/AutoSteer/AutoSteerLiteTuningCalculatorTests.cs†L11-L38】
- Increasing implement width raises the hold multiplier above 3.5 and keeps look-ahead distances greater than 2.0 metres, mirroring V6’s bias for wider toolbars.【F:Nexus SourceCode/tests/Aog.Plugins.Tests/AutoSteer/AutoSteerLiteTuningCalculatorTests.cs†L40-L55】

The tuning tests, combined with the coverage and section control regressions, provide baseline verification for the ported math stack. Future algorithm changes should update these datasets and adjust tolerances thoughtfully to maintain confidence in parity.

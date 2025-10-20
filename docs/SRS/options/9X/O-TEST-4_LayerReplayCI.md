# O-TEST-4: Replay-driven CI and rollout for layers

## Summary
Builds a deterministic replay and benchmarking suite that validates aggregation math, rendering parity, and performance targets for variable-rate layers before field rollout, paired with staged feature flags.

## Details
- Implementation roadmap begins with core types/tests covering area-weighted overlap math, percent numerator/denominator handling, and min/max updates, followed by rendering refactors and aggregation buffering benchmarks for 48–64 row rigs at 10 Hz.
- UI enhancements, IO/interop wiring (PGNs `0xE4`/`0xE3`, handshake `0xE2`, feedback blocks `0xE1`/`0xE0`, aggregates `0xDF`), and persistence changes each enter behind targeted feature flags (default on for UDP, optional for CAN, legacy-only fallback available).
- Golden replay suite captures AgDiag recordings for planter, sprayer, and combine scenarios to generate deterministic PNGs, summary tables, and `badSample` counts validated in CI.
- Performance acceptance criteria: ≤3% CPU and ≤150 MB RAM after a one-hour session while sustaining 30–60 FPS rendering.
- Regression protection compares aggregator outputs (sum/avg/min/max) across releases using the replay suite, failing CI on deviations.
- Rollout documentation covers inspector workflow, rate-limit guidance, troubleshooting steps, and feature-flag toggles for field ops.

## Readiness & dependencies
- Anchored by R-CI-010/R-CI-011 requirements plus artifact guarantees in R-CI-012; failure to meet them blocks variable-layer ADRs.
- Relies on data-model schema hashing (R-DATA-012/R-DATA-014) and transport diagnostics (R-COMM-011/R-TH-010) to produce comparable replay outputs.
- Hardware validation requires coordination with R-HW-013 safety interlocks and documented fixture versions (R-CI-013).

## Pros
- Prevents regression of critical agronomic metrics before they reach the field.
- Replay-driven validation gives contributors confidence when refactoring aggregation or rendering code.
- Feature flags allow gradual enablement while supporting legacy-only deployments.

## Cons
- Requires curated replay datasets and infrastructure to store comparison artifacts.
- CI runtime increases due to rendering and aggregation benchmarks.
- Field teams must manage feature flags during rollout until legacy paths are retired.

## Risks & mitigations
- **Risk:** Replay suite drifts from real-world scenarios. **Mitigation:** Periodically refresh recordings from representative rigs and include scripts for contributors to contribute new traces.
- **Risk:** Feature-flag complexity confuses operators. **Mitigation:** Provide defaults and documentation that explain when to enable/disable new layers per transport.
- **Risk:** Benchmark noise triggers false positives. **Mitigation:** Use deterministic simulations and tolerance thresholds tuned to floating-point expectations.

## Borrowables
- Existing AgDiag record/replay tooling forms the backbone of deterministic tests.
- Current build pipelines can publish comparison artifacts (PNGs, CSV summaries) for review.
- Feature flag infrastructure in AgOpenGPS can be extended to guard new layer functionality.

## Rough effort
M — Significant investment in replay assets, CI scripting, and documentation but amortizes risk for future layer additions.

## References
<<<<<<< HEAD
- [Section 72 — Mapping Layers Plugin](../sections/7X_Mapping_Geospatial/72_Mapping_Layers_Plugin.md)
- [Section 96 — Quality Engineering & Release](../sections/9X_Frontends_Ops/96_Quality_Engineering_Release.md)
=======
- [Section 72 — Mapping Layers Plugin](../sections/7X/72_Mapping_Layers_Plugin.md)
- [Section 96 — Quality Engineering & Release](../sections/9X/96_Quality_Engineering_Release.md)
>>>>>>> origin/develop

## Related ADRs
- [ADR-004 — Composite Simulation](../../ADR/ADR-004-composite-simulation.md)
- [ADR-020 — Determinism & Replay CI](../../ADR/ADR-020-determinism-replay-ci.md)
- [ADR-068 — Layer controllers & aggregation runtime](../../ADR/ADR-068-layer-controllers-runtime.md)


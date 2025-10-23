# Global guardrail regression checks (NX-616)

ADR-025 and ADR-026 require Nexus to enforce data retention windows, stay within
published performance budgets, and keep deterministic replay plus crash recovery
fixtures wired into CI. This guide bundles those guardrails so contributors and
automation can run the same checks before merging changes.【F:docs/development/SRS/sections/3X_Data_Storage/34-ADR-025 - Data lifecycle and retention policy.md†L17-L38】【F:docs/development/SRS/sections/9X_Frontends_Ops/96-ADR-026 - Performance budgets and instrumentation.md†L12-L37】【F:docs/development/SRS/sections/2X_System_Architecture/21-ADR-900 - PoseStream, Layer, and Control Program Roadmap.md†L571-L600】

## Guardrail coverage

| Guardrail | What it verifies | Test anchor |
| --- | --- | --- |
| Retention windows | Mesh retention keeps only items inside the 30/90/365-day policies captured in ADR-025, pruning older entries when new publications arrive.【F:Nexus SourceCode/tests/Aog.Core.Tests/Mesh/MeshRetentionStoreTests.cs†L17-L68】 | `MeshRetentionStoreTests.Record_RespectsRetentionWindows` |
| Deterministic replay | The 10-second deterministic scenario reproduces the golden CSV so PoseStream and TileStore hashing stay stable across runs.【F:Nexus SourceCode/tests/Aog.Core.Tests/Simulation/DeterministicSimulationRegressionTests.cs†L21-L70】 | `DeterministicSimulationRegressionTests.TenSecondRun_MatchesGoldenCsvAsync` |
| Performance budgets | Each simulation scenario meets the CPU-oriented publish thresholds tracked by the performance harness and ADR-026 budget catalog.【F:Nexus SourceCode/tests/Aog.Core.Tests/Simulation/SimulationPerformanceHarnessTests.cs†L19-L70】 | `SimulationPerformanceHarnessTests.Scenario_CompletesWithinBudgetAsync` |
| Crash recovery | Session autosave pipelines recover journal entries without duplication after an unexpected shutdown, keeping acceptance fixtures green.【F:Nexus SourceCode/tests/Aog.Core.Tests/Jobs/SessionCrashRecoveryRegressionTests.cs†L15-L78】 | `SessionCrashRecoveryRegressionTests.CrashRecovery_ReplaysLostJournalEntryAndPersistsResumeAsync` |

Add `[Trait("Category", "Guardrail")]` to new regression tests when they should
join the bundle; the guardrail command below automatically discovers every test
with that trait.【F:Nexus SourceCode/tests/Aog.Core.Tests/Mesh/MeshRetentionStoreTests.cs†L12-L68】

## Running the guardrails

```bash
./tools/scripts/nexus.sh guardrails
```

On PowerShell-enabled hosts use:

```powershell
./tools/scripts/nexus.ps1 guardrails
```

Both entry points call `dotnet test` against `Nexus SourceCode/Nexus.sln` with a
filter that executes only guardrail-tagged tests, keeping runtime short while
exercising the global acceptance matrix.【F:tools/scripts/nexus.sh†L12-L64】【F:tools/scripts/nexus.ps1†L12-L86】

Pass additional `dotnet test` options after `--`; for example,
`./tools/scripts/nexus.sh guardrails -- --logger trx` records results for CI.

## CI integration checklist

1. Add `nexus guardrails` to the relevant workflow after build/test steps so
   regression buckets run on every PR.
2. Fail the job on any non-zero exit code; the guardrail bundle returns 1 when
   a test fails and 0 on success.
3. Publish the `TestResults.xml` or TRX artefacts if you request additional
   loggers; they contain per-guardrail timing data useful for audits.
4. When new guardrails land, ensure they include documentation updates and
   deterministic fixtures so this bundle stays fast and actionable.

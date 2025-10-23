# Stanley Controller Parity Harness

Task NX-313 adds a deterministic regression harness that compares the Nexus
Stanley controller port against a set of baseline pose samples. The goal is to
ensure steering commands match the legacy implementation before integrating
with the rest of the guidance stack.

## Baseline data

`tests/Aog.Plugins.Tests/Baselines/StanleyParityBaseline.json` stores:

- A straight lane centreline sampled at 10 m intervals.
- Five vehicle poses captured at different offsets, headings, and speeds.
- The expected steering command (radians) computed by the validated port.

The harness resets controller state for every sample to avoid cross-contamination
and verifies each command within 1e-9 radians.

## Running the test

Execute the parity harness with:

```bash
dotnet test "Nexus SourceCode/tests/Aog.Plugins.Tests/Aog.Plugins.Tests.csproj" \
  --filter StanleyControllerParityTests
```

The test reads the baseline JSON, instantiates the controller in Stanley mode,
and checks that every command equals the stored expectation. Failures mean the
controller diverged from the approved baseline and require investigation before
shipping.

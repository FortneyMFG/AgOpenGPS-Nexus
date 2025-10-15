# Firmware-in-Loop Stability Validation

Task NX-317 introduces a firmware-in-loop simulation slice that exercises the
Stanley controller with dynamic look-ahead while constraint contexts change
(headland entry and blocking keep-out zones). The harness verifies:

- Cross-track error settles below 5 cm after the acquisition phase.
- Heading error remains within 0.2 rad during the run.
- Dynamic look-ahead honours minimum bounds and reduces when entering headlands
  or approaching blocking constraints.
- Constraint-aware previews translate correctly into the new guidance lane
  protobuf contract.

## Running the slice

Run the stability test with:

```bash
dotnet test "Nexus SourceCode/tests/Aog.Plugins.Tests/Aog.Plugins.Tests.csproj" \
  --filter FirmwareInLoopStabilityTests
```

The test simulates 22 seconds of motion against a straight lane while the
constraint context transitions from open field → headland → blocking keep-out.
Assertions cover cross-track/heading envelopes, look-ahead trends, and
constraint clamping. A final preview is converted to `GuidanceLane` and checked
for consistency, ensuring the firmware-facing payload mirrors the simulated
state.

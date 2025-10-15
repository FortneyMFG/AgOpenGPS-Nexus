# Cross-track Replay Harness Slice

The cross-track slice exercises the replay plumbing between the PoseStream
recordings and the AutoSteer guidance preview so we can validate controller
behaviour without the full mapping stack. The integration test added for
NX-235 writes a deterministic set of pose samples that simulate a machine
acquiring the guidance line from a 1.5 m offset and then replays the samples
through the `TelemetryReplayController`.

## What the harness validates

- **Pose ingest:** The replay controller streams Parquet pose frames onto the
  in-memory event bus without dropping sequence numbers.
- **Cross-track trend:** The harness projects each pose into a local east/north
  frame and verifies the lateral error decreases monotonically while settling
  below 10 cm.
- **Metrics surface:** A summary structure captures max, final, RMS error, and
  replay duration so future slices (section arbiter, guidance preview) can plug
  into the same assertions.

## Running the test

Run the targeted unit test to regenerate the replay parquet fixtures and
execute the slice:

```bash
dotnet test "Nexus SourceCode/tests/Aog.Core.Tests/Aog.Core.Tests.csproj" \
  --filter CrossTrackReplayHarnessTests
```

The test creates its own temporary working directory, so it is safe to run in
parallel with other replay scenarios or within CI.

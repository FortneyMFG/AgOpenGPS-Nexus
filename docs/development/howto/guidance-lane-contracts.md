# Guidance Lane Publishing Contracts

ADR-033 standardises the geometry and preview data published by the guidance
planner. The `guidance.proto` contract formalises that payload so Core, plugins,
and firmware can exchange a deterministic lane model with constraint context.

## Message schema

The protobuf file `Nexus SourceCode/proto/guidance.proto` introduces:

- `LaneMetadata` — stable identifiers and provenance fields (lane id, template,
  field/job/session ids).
- `LanePoint` / `LanePass` — sampled planar geometry for each pass.
- `LanePreview` — dynamic preview state (cross-track, heading error, look-ahead,
  controller output) plus `LaneConstraintState` describing the active zone mask.
- `GuidanceLane` — the full lane definition with preview and planner metadata.
- `GuidanceLaneFrame` — bundling multiple lanes into a single publish frame.

All guidance payloads embed the existing `aog.core.v1.Header` so telemetry
retains deterministic sequencing and provenance.

## .NET helpers

`Aog.Core.Guidance` contains immutable domain models (`GuidanceLaneMetadata`,
`GuidanceLane`, `GuidanceLanePreview`, etc.) alongside extension methods that
convert to and from the generated protobuf types. Example usage:

```csharp
var metadata = new GuidanceLaneMetadata("lane-01", GuidanceLaneTemplate.Straight, "AB North");
var pass = new GuidanceLanePass(0, passPoints, headingRadians: 0, signedDistanceMeters: 0);
var preview = new GuidanceLanePreview(crossTrackError, headingError, lookAhead, targetPoint, controllerOutput, controllerEnabled);
var lane = new GuidanceLane(metadata, laneSpacing, implementWidth, overlap, nudge, extension, baseHeading, new[] { pass }, preview);
var publish = new GuidanceLanePublish(header, lane);
var proto = publish.ToProto();
```

Round-tripping back to the domain model is equally simple:

```csharp
var publishModel = proto.ToModel();
```

## Validation

`GuidanceLaneContractTests` exercises the conversion helpers and ensures the
schema captures preview data, constraint masks, and pass geometry without loss.
Run the test with:

```bash
dotnet test "Nexus SourceCode/tests/Aog.Core.Tests/Aog.Core.Tests.csproj" \
  --filter GuidanceLaneContractTests
```

using System;
using System.Collections.Generic;
using Aog.Core.Machines.Axle;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Machines.Axle;

public sealed class AxleCentricProfileLoaderTests
{
    private const string SampleProfile = """
    {
      "schemaVersion": "1.0",
      "profileId": "rig-articulated-tractor",
      "compatibility": {
        "minimumCoreVersion": "1.5.0",
        "schemaVersion": "1.0"
      },
      "axles": [
        { "id": "tractor-front", "role": "Steer", "curvatureLimit": 0.35, "slipLimit": 0.08 },
        { "id": "tractor-rear", "role": "Drive", "slipLimit": 0.05 },
        { "id": "implement", "role": "Implement", "slipLimit": 0.12 }
      ],
      "joints": [
        { "parent": "tractor-front", "child": "tractor-rear", "type": "Rigid" },
        { "parent": "tractor-rear", "child": "implement", "type": "Drawbar" }
      ],
      "modes": {
        "road": {
          "curvatureLimit": 0.18,
          "slipLimit": 0.04,
          "driveDirectionPolicy": "ForwardOnly"
        },
        "field": {
          "curvatureLimit": 0.32,
          "slipLimit": 0.12,
          "driveDirectionPolicy": "Bidirectional"
        }
      },
      "lateMeasurementPolicy": {
        "allowLateMeasurements": true,
        "maximumLatencyMilliseconds": 80
      },
      "deterministicSeed": 1337
    }
    """;

    [Fact]
    public void Load_ValidProfile_ProducesRuntimeProfile()
    {
        var loader = new AxleCentricProfileLoader();
        var result = loader.Load(SampleProfile, new AxleIngestionOptions(new Version(1, 5, 1), null));

        result.IsSuccess.Should().BeTrue();
        result.Profile.Should().NotBeNull();
        result.Profile!.ProfileId.Should().Be("rig-articulated-tractor");
        result.Profile.DeterministicSeed.Should().Be(1337);
        result.Profile.Modes.Should().ContainKey("field");
        result.Profile.GetMode("field").CurvatureLimit.Should().BeApproximately(0.32, 1e-6);
        result.Profile.GetMode("road").DriveDirectionPolicy.Should().Be(DriveDirectionPolicy.ForwardOnly);
        result.Telemetry.AxleCount.Should().Be(3);
        result.Telemetry.ModeCurvatureLimits["road"].Should().BeApproximately(0.18, 1e-6);
    }

    [Fact]
    public void Load_ReorderedJson_ProducesStableHash()
    {
        var loader = new AxleCentricProfileLoader();
        var first = loader.Load(SampleProfile);
        var reordered = loader.Load("""
        {
          "profileId": "rig-articulated-tractor",
          "schemaVersion": "1.0",
          "axles": [
            { "id": "tractor-rear", "role": "Drive", "slipLimit": 0.05 },
            { "id": "tractor-front", "role": "Steer", "curvatureLimit": 0.35, "slipLimit": 0.08 },
            { "id": "implement", "role": "Implement", "slipLimit": 0.12 }
          ],
          "joints": [
            { "parent": "tractor-front", "child": "tractor-rear", "type": "Rigid" },
            { "parent": "tractor-rear", "child": "implement", "type": "Drawbar" }
          ],
          "modes": {
            "field": {
              "curvatureLimit": 0.32,
              "slipLimit": 0.12,
              "driveDirectionPolicy": "Bidirectional"
            },
            "road": {
              "curvatureLimit": 0.18,
              "slipLimit": 0.04,
              "driveDirectionPolicy": "ForwardOnly"
            }
          },
          "compatibility": {
            "minimumCoreVersion": "1.5.0"
          },
          "lateMeasurementPolicy": {
            "maximumLatencyMilliseconds": 80,
            "allowLateMeasurements": true
          }
        }
        """);

        first.IsSuccess.Should().BeTrue();
        reordered.IsSuccess.Should().BeTrue();
        reordered.Profile!.ContentHash.Should().Be(first.Profile!.ContentHash);
    }

    [Fact]
    public void Load_IncompatibleSchema_ReturnsError()
    {
        var json = SampleProfile.Replace("\"schemaVersion\": \"1.0\"", "\"schemaVersion\": \"9.9\"");
        var loader = new AxleCentricProfileLoader();
        var result = loader.Load(json);

        result.IsSuccess.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Code == "KIN-001");
    }

    [Fact]
    public void Load_GuardViolation_ReturnsError()
    {
        var json = SampleProfile.Replace("\"minimumCoreVersion\": \"1.5.0\"", "\"minimumCoreVersion\": \"9.9.9\"");
        var loader = new AxleCentricProfileLoader();
        var result = loader.Load(json, new AxleIngestionOptions(new Version(1, 0), null));

        result.IsSuccess.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Code == "KIN-003");
    }

    [Fact]
    public void Load_CyclicGraph_ReturnsError()
    {
        var json = """
        {
          "schemaVersion": "1.0",
          "profileId": "cycle",
          "axles": [
            { "id": "a", "role": "Steer", "curvatureLimit": 0.3 },
            { "id": "b", "role": "Drive", "curvatureLimit": 0.3 }
          ],
          "joints": [
            { "parent": "a", "child": "b", "type": "Rigid" },
            { "parent": "b", "child": "a", "type": "Rigid" }
          ],
          "modes": {
            "field": { "curvatureLimit": 0.3, "slipLimit": 0.1 }
          }
        }
        """;

        var loader = new AxleCentricProfileLoader();
        var result = loader.Load(json);

        result.IsSuccess.Should().BeFalse();
        result.Messages.Should().Contain(m => m.Code == "KIN-062");
    }
}

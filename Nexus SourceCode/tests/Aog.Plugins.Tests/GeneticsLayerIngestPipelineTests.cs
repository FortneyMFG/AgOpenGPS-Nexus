using System;
using System.Collections.Generic;
using Aog.Core.Paths;
using Aog.Plugins.Genetics;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class GeneticsLayerIngestPipelineTests
{
    private static List<PlanarPoint> CreateSquare(double size) => new()
    {
        new PlanarPoint(0, 0),
        new PlanarPoint(size, 0),
        new PlanarPoint(size, size),
        new PlanarPoint(0, size)
    };

    [Fact]
    public void UpsertPlan_FirstFeatureCreatesEntry()
    {
        var now = new DateTimeOffset(2025, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var pipeline = new GeneticsLayerIngestPipeline(new GeneticsLayerIngestOptions
        {
            PlanLayerId = "layer:genetics.plan",
            VarietyLayerId = "layer:genetics.variety",
            DefaultActor = "plugin:genetics",
            DefaultSource = "plugin:genetics"
        }, () => now);

        var feature = pipeline.UpsertPlan(new GeneticsPlanIngestRequest
        {
            ZoneId = "Field #1",
            OuterBoundary = CreateSquare(10),
            Brand = "Pioneer",
            Product = "P1234",
            TraitStack = "VT2P",
            Lot = "LOT-1",
            Treatment = "Fungicide",
            Source = "manual",
            Notes = "North block",
            JobId = "job:123"
        });

        feature.ZoneId.Should().Be("Field #1");
        feature.FeatureId.Should().Be("geneticsPlan:field-1");
        feature.LayerId.Should().Be("layer:genetics.plan");
        feature.JobId.Should().Be("job:123");
        feature.CreatedAt.Should().Be(now);
        feature.LastModifiedAt.Should().BeNull();
        feature.Brand.Should().Be("Pioneer");
        feature.Product.Should().Be("P1234");
        feature.TraitStack.Should().Be("VT2P");
        feature.Source.Should().Be("manual");
        feature.Geometry.AreaSquareMeters.Should().BeApproximately(100, 1e-6);

        pipeline.TryGetPlan("Field #1", out var retrieved).Should().BeTrue();
        retrieved.Should().BeSameAs(feature);

        var plans = pipeline.GetPlanFeatures();
        plans.Should().ContainSingle().Which.FeatureId.Should().Be("geneticsPlan:field-1");
    }

    [Fact]
    public void UpsertPlan_UpdateRefreshesLastModified()
    {
        var now = new DateTimeOffset(2025, 1, 1, 8, 0, 0, TimeSpan.Zero);
        var pipeline = new GeneticsLayerIngestPipeline(clock: () => now);

        pipeline.UpsertPlan(new GeneticsPlanIngestRequest
        {
            ZoneId = "South Block",
            OuterBoundary = CreateSquare(12),
            Brand = "Dekalb",
            Product = "DKC50-80",
            Source = "import"
        });

        now = now.AddHours(1);

        var updated = pipeline.UpsertPlan(new GeneticsPlanIngestRequest
        {
            ZoneId = "South Block",
            OuterBoundary = CreateSquare(12),
            Brand = "Dekalb",
            Product = "DKC50-80",
            TraitStack = "SmartStax",
            Notes = "Adjusted lot",
            Source = "operator"
        });

        updated.FeatureId.Should().Be("geneticsPlan:south-block");
        updated.CreatedAt.Should().Be(new DateTimeOffset(2025, 1, 1, 8, 0, 0, TimeSpan.Zero));
        updated.LastModifiedAt.Should().Be(now);
        updated.TraitStack.Should().Be("SmartStax");
        updated.Source.Should().Be("operator");
        updated.Notes.Should().Be("Adjusted lot");
    }

    [Fact]
    public void UpsertPlan_InvalidGeometryThrows()
    {
        var pipeline = new GeneticsLayerIngestPipeline();

        var request = new GeneticsPlanIngestRequest
        {
            ZoneId = "Invalid",
            OuterBoundary = new List<PlanarPoint>
            {
                new(0, 0),
                new(10, 0)
            },
            Brand = "Brand",
            Product = "Product"
        };

        var action = () => pipeline.UpsertPlan(request);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpsertPlan_GeneratesStableIdentifierForSymbols()
    {
        var pipeline = new GeneticsLayerIngestPipeline(clock: () => new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero));

        var feature = pipeline.UpsertPlan(new GeneticsPlanIngestRequest
        {
            ZoneId = "***",
            OuterBoundary = CreateSquare(5),
            Brand = "Brand",
            Product = "Product"
        });

        feature.FeatureId.Should().MatchRegex("^geneticsPlan:[a-f0-9]{12}$");
    }

    [Fact]
    public void UpsertVariety_CreatesFeatureWithChangeLog()
    {
        var baseTime = new DateTimeOffset(2025, 4, 1, 7, 30, 0, TimeSpan.Zero);
        var now = baseTime;
        var pipeline = new GeneticsLayerIngestPipeline(clock: () => now);

        var changeLog = new List<GeneticsChangeLogEntry>
        {
            new(baseTime.AddMinutes(-10), "operator:1", "lot", "old", "LOT-1"),
            new(baseTime.AddMinutes(-5), "operator:1", "barcode", null, "ABC123")
        };

        var feature = pipeline.UpsertVariety(new GeneticsVarietyIngestRequest
        {
            ZoneId = "Field #1",
            SessionId = "session:abc",
            JobId = "job:789",
            OuterBoundary = CreateSquare(15),
            Brand = "Pioneer",
            Product = "P1234",
            TraitStack = "VT2P",
            Lot = "LOT-1",
            Barcode = "ABC123",
            ChangeLog = changeLog,
            Notes = "Applied with row cleaners"
        });

        feature.FeatureId.Should().Be("geneticsVariety:session-abc:field-1");
        feature.SessionId.Should().Be("session:abc");
        feature.JobId.Should().Be("job:789");
        feature.CreatedAt.Should().Be(baseTime);
        feature.AppliedAt.Should().Be(baseTime);
        feature.Barcode.Should().Be("ABC123");
        feature.ChangeLog.Should().HaveCount(2);
        feature.ChangeLog[0].Field.Should().Be("lot");
        feature.ChangeLog[1].Field.Should().Be("barcode");
    }

    [Fact]
    public void UpsertVariety_UpdateKeepsFeatureIdentity()
    {
        var now = new DateTimeOffset(2025, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var pipeline = new GeneticsLayerIngestPipeline(clock: () => now);

        var initial = pipeline.UpsertVariety(new GeneticsVarietyIngestRequest
        {
            ZoneId = "North",
            SessionId = "session:1",
            JobId = "job:1",
            OuterBoundary = CreateSquare(8),
            Brand = "Brand",
            Product = "Product",
            AppliedAt = now
        });

        now = now.AddMinutes(30);

        var updated = pipeline.UpsertVariety(new GeneticsVarietyIngestRequest
        {
            ZoneId = "North",
            SessionId = "session:1",
            JobId = "job:1",
            OuterBoundary = CreateSquare(8),
            Brand = "Brand",
            Product = "Product",
            Barcode = "NEWBAR"
        });

        updated.FeatureId.Should().Be(initial.FeatureId);
        updated.CreatedAt.Should().Be(initial.CreatedAt);
        updated.AppliedAt.Should().Be(initial.AppliedAt);
        updated.LastModifiedAt.Should().Be(now);
        updated.Barcode.Should().Be("NEWBAR");
    }

    [Fact]
    public void RemoveVariety_RemovesEntry()
    {
        var pipeline = new GeneticsLayerIngestPipeline(clock: () => new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.Zero));

        pipeline.UpsertVariety(new GeneticsVarietyIngestRequest
        {
            ZoneId = "Block A",
            SessionId = "session:2",
            JobId = "job:55",
            OuterBoundary = CreateSquare(6),
            Brand = "Brand",
            Product = "Product"
        });

        pipeline.RemoveVariety("Block A", "session:2").Should().BeTrue();
        pipeline.TryGetVariety("Block A", "session:2", out _).Should().BeFalse();
    }
}

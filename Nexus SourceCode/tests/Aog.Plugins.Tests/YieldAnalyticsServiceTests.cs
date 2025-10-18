using System;
using System.Collections.Generic;
using Aog.Plugins.CombineYield;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Plugins.Tests;

public sealed class YieldAnalyticsServiceTests
{
    [Fact]
    public void CreateSnapshot_UsesTimeProviderAndCopiesCollections()
    {
        var generatedAt = DateTimeOffset.Parse("2025-06-01T12:00:00Z");
        var time = new FakeTimeProvider(generatedAt);
        var service = new YieldAnalyticsService(time);

        var scope = new YieldAggregationScope("farm-1", seasonId: "2025");
        var summary = new YieldSummary(10, 9000, 8800, 450, 7000, 11000, 25000);
        var crops = new List<YieldCropAggregation>
        {
            new(scope, "Corn", summary)
        };
        var varieties = new List<YieldVarietyAggregation>
        {
            new(scope, "Corn", "Hybrid-1", summary)
        };

        var snapshot = service.CreateSnapshot(crops, varieties);

        Assert.Equal(generatedAt, snapshot.GeneratedAt);
        Assert.False(ReferenceEquals(crops, snapshot.Crops));
        Assert.False(ReferenceEquals(varieties, snapshot.Varieties));
        Assert.Single(snapshot.Crops);
        Assert.Single(snapshot.Varieties);

        crops.Clear();
        varieties.Clear();

        Assert.Single(snapshot.Crops);
        Assert.Single(snapshot.Varieties);
    }

    [Fact]
    public void CreateSnapshot_ThrowsWhenCollectionsContainNull()
    {
        var service = new YieldAnalyticsService(new FakeTimeProvider());
        var scope = new YieldAggregationScope("farm-1");
        var summary = new YieldSummary(5, 8500, 8300, 300, 7000, 9000, 12000);
        var valid = new YieldCropAggregation(scope, "Corn", summary);

        Assert.Throws<ArgumentException>(() => service.CreateSnapshot(new YieldCropAggregation?[] { valid, null }, Array.Empty<YieldVarietyAggregation>()));
        Assert.Throws<ArgumentException>(() => service.CreateSnapshot(Array.Empty<YieldCropAggregation>(), new YieldVarietyAggregation?[] { null! }));
    }

    [Fact]
    public void GetYieldByCrop_FiltersAndSortsResults()
    {
        var service = new YieldAnalyticsService(new FakeTimeProvider());

        var farmScope = new YieldAggregationScope("farm-1");
        var seasonScope = new YieldAggregationScope("farm-1", seasonId: "2025", fieldId: "field-1", jobId: "job-1");
        var otherJobScope = new YieldAggregationScope("farm-1", seasonId: "2025", fieldId: "field-1", jobId: "job-2");
        var otherFarmScope = new YieldAggregationScope("farm-2", seasonId: "2025");

        var snapshot = service.CreateSnapshot(
            new[]
            {
                new YieldCropAggregation(farmScope, "Corn", new YieldSummary(100, 9500, 9400, 400, 8200, 10300, 60000)),
                new YieldCropAggregation(seasonScope, "Corn", new YieldSummary(40, 9200, 9100, 350, 8000, 10000, 23000)),
                new YieldCropAggregation(otherJobScope, "Soy", new YieldSummary(35, 4200, 4100, 250, 3600, 4700, 15000)),
                new YieldCropAggregation(otherFarmScope, "Corn", new YieldSummary(50, 9800, 9700, 300, 9100, 10400, 32000))
            },
            Array.Empty<YieldVarietyAggregation>());

        var filter = new YieldScopeFilter { FarmId = "farm-1", SeasonId = "2025" };
        var results = service.GetYieldByCrop(snapshot, filter);

        Assert.Equal(2, results.Count);
        Assert.Equal("Corn", results[0].Crop);
        Assert.Equal("Soy", results[1].Crop);
        Assert.All(results, result =>
        {
            Assert.Equal("farm-1", result.Scope.FarmId);
            Assert.Equal("2025", result.Scope.SeasonId);
        });

        filter = new YieldScopeFilter { FarmId = "farm-1" };
        results = service.GetYieldByCrop(snapshot, filter);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, result => result.Scope.JobId is null);
    }

    [Fact]
    public void GetYieldByVariety_FiltersResults()
    {
        var service = new YieldAnalyticsService(new FakeTimeProvider());
        var scopeA = new YieldAggregationScope("farm-1", seasonId: "2025", jobId: "job-1", sessionId: "session-1");
        var scopeB = new YieldAggregationScope("farm-1", seasonId: "2025", jobId: "job-1", sessionId: "session-2");
        var scopeOther = new YieldAggregationScope("farm-2", seasonId: "2025");

        var snapshot = service.CreateSnapshot(
            Array.Empty<YieldCropAggregation>(),
            new[]
            {
                new YieldVarietyAggregation(scopeA, "Corn", "Hybrid-1", new YieldSummary(20, 9100, 9000, 320, 7800, 10200, 12000)),
                new YieldVarietyAggregation(scopeB, "Corn", "Hybrid-2", new YieldSummary(18, 8900, 8800, 310, 7600, 9800, 11000)),
                new YieldVarietyAggregation(scopeOther, "Corn", "Hybrid-1", new YieldSummary(25, 9300, 9200, 330, 8000, 10400, 14000))
            });

        var filter = new YieldScopeFilter { FarmId = "farm-1", JobId = "job-1" };
        var results = service.GetYieldByVariety(snapshot, filter);

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.Equal("farm-1", result.Scope.FarmId));
        Assert.All(results, result => Assert.Equal("job-1", result.Scope.JobId));
        Assert.Equal(new[] { "Hybrid-1", "Hybrid-2" }, new[] { results[0].Variety, results[1].Variety });
    }
}

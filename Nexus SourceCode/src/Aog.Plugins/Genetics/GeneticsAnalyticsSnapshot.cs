using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Aog.Plugins.Genetics;

/// <summary>
/// Delegate invoked when genetics analytics snapshots change.
/// </summary>
/// <param name="current">Latest analytics snapshot.</param>
/// <param name="previous">Previous analytics snapshot when available.</param>
public delegate void GeneticsAnalyticsCallback(GeneticsAnalyticsSnapshot current, GeneticsAnalyticsSnapshot? previous);

/// <summary>
/// Immutable snapshot describing the current genetics plan and variety analytics aggregates.
/// </summary>
public sealed class GeneticsAnalyticsSnapshot
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsAnalyticsSnapshot"/> class.
    /// </summary>
    public GeneticsAnalyticsSnapshot(
        DateTimeOffset generatedAt,
        IReadOnlyList<GeneticsPlanAnalytics> plans,
        IReadOnlyList<GeneticsVarietyAnalytics> varieties,
        IReadOnlyList<GeneticsLotStatistic> lots)
    {
        GeneratedAt = generatedAt;
        Plans = Wrap(plans);
        Varieties = Wrap(varieties);
        Lots = Wrap(lots);
    }

    /// <summary>Gets the timestamp the snapshot was generated.</summary>
    public DateTimeOffset GeneratedAt { get; }

    /// <summary>Gets aggregated plan analytics.</summary>
    public IReadOnlyList<GeneticsPlanAnalytics> Plans { get; }

    /// <summary>Gets aggregated as-applied analytics.</summary>
    public IReadOnlyList<GeneticsVarietyAnalytics> Varieties { get; }

    /// <summary>Gets aggregated lot level statistics across plan and variety records.</summary>
    public IReadOnlyList<GeneticsLotStatistic> Lots { get; }

    private static IReadOnlyList<T> Wrap<T>(IReadOnlyList<T> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items.Count == 0)
        {
            return Array.Empty<T>();
        }

        if (items is List<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }

        return new ReadOnlyCollection<T>(new List<T>(items));
    }
}

/// <summary>
/// Aggregated analytics describing planned genetics features.
/// </summary>
public sealed class GeneticsPlanAnalytics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsPlanAnalytics"/> class.
    /// </summary>
    public GeneticsPlanAnalytics(
        string? jobId,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        int featureCount,
        double totalAreaSquareMeters)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException("Brand is required.", nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            throw new ArgumentException("Product is required.", nameof(product));
        }

        if (featureCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(featureCount), featureCount, "Feature count must be non-negative.");
        }

        if (double.IsNaN(totalAreaSquareMeters) || double.IsInfinity(totalAreaSquareMeters) || totalAreaSquareMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAreaSquareMeters), totalAreaSquareMeters, "Area must be a non-negative finite value.");
        }

        JobId = string.IsNullOrWhiteSpace(jobId) ? null : jobId;
        Brand = brand;
        Product = product;
        TraitStack = string.IsNullOrWhiteSpace(traitStack) ? null : traitStack;
        Lot = string.IsNullOrWhiteSpace(lot) ? null : lot;
        Treatment = string.IsNullOrWhiteSpace(treatment) ? null : treatment;
        FeatureCount = featureCount;
        TotalAreaSquareMeters = totalAreaSquareMeters;
    }

    /// <summary>Gets the optional job identifier.</summary>
    public string? JobId { get; }

    /// <summary>Gets the seed brand.</summary>
    public string Brand { get; }

    /// <summary>Gets the seed product identifier.</summary>
    public string Product { get; }

    /// <summary>Gets the optional trait stack.</summary>
    public string? TraitStack { get; }

    /// <summary>Gets the optional lot identifier.</summary>
    public string? Lot { get; }

    /// <summary>Gets the optional treatment information.</summary>
    public string? Treatment { get; }

    /// <summary>Gets the number of plan features represented.</summary>
    public int FeatureCount { get; }

    /// <summary>Gets the total planned area in square metres.</summary>
    public double TotalAreaSquareMeters { get; }
}

/// <summary>
/// Aggregated analytics describing as-applied genetics features.
/// </summary>
public sealed class GeneticsVarietyAnalytics
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsVarietyAnalytics"/> class.
    /// </summary>
    public GeneticsVarietyAnalytics(
        string jobId,
        string brand,
        string product,
        string? traitStack,
        string? lot,
        string? treatment,
        string? barcode,
        int sessionCount,
        int featureCount,
        double totalAreaSquareMeters)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("JobId is required.", nameof(jobId));
        }

        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new ArgumentException("Brand is required.", nameof(brand));
        }

        if (string.IsNullOrWhiteSpace(product))
        {
            throw new ArgumentException("Product is required.", nameof(product));
        }

        if (sessionCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionCount), sessionCount, "Session count must be non-negative.");
        }

        if (featureCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(featureCount), featureCount, "Feature count must be non-negative.");
        }

        if (double.IsNaN(totalAreaSquareMeters) || double.IsInfinity(totalAreaSquareMeters) || totalAreaSquareMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAreaSquareMeters), totalAreaSquareMeters, "Area must be a non-negative finite value.");
        }

        JobId = jobId;
        Brand = brand;
        Product = product;
        TraitStack = string.IsNullOrWhiteSpace(traitStack) ? null : traitStack;
        Lot = string.IsNullOrWhiteSpace(lot) ? null : lot;
        Treatment = string.IsNullOrWhiteSpace(treatment) ? null : treatment;
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode;
        SessionCount = sessionCount;
        FeatureCount = featureCount;
        TotalAreaSquareMeters = totalAreaSquareMeters;
    }

    /// <summary>Gets the job identifier.</summary>
    public string JobId { get; }

    /// <summary>Gets the seed brand.</summary>
    public string Brand { get; }

    /// <summary>Gets the seed product.</summary>
    public string Product { get; }

    /// <summary>Gets the optional trait stack.</summary>
    public string? TraitStack { get; }

    /// <summary>Gets the optional lot identifier.</summary>
    public string? Lot { get; }

    /// <summary>Gets the optional treatment information.</summary>
    public string? Treatment { get; }

    /// <summary>Gets the optional barcode associated with the aggregate.</summary>
    public string? Barcode { get; }

    /// <summary>Gets the number of distinct sessions represented.</summary>
    public int SessionCount { get; }

    /// <summary>Gets the number of variety features represented.</summary>
    public int FeatureCount { get; }

    /// <summary>Gets the total applied area in square metres.</summary>
    public double TotalAreaSquareMeters { get; }
}

/// <summary>
/// Summarizes lot level coverage for planned and as-applied genetics features.
/// </summary>
public sealed class GeneticsLotStatistic
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneticsLotStatistic"/> class.
    /// </summary>
    public GeneticsLotStatistic(
        string lot,
        double plannedAreaSquareMeters,
        double appliedAreaSquareMeters,
        int plannedFeatureCount,
        int appliedFeatureCount)
    {
        if (string.IsNullOrWhiteSpace(lot))
        {
            throw new ArgumentException("Lot is required.", nameof(lot));
        }

        if (double.IsNaN(plannedAreaSquareMeters) || double.IsInfinity(plannedAreaSquareMeters) || plannedAreaSquareMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedAreaSquareMeters), plannedAreaSquareMeters, "Area must be a non-negative finite value.");
        }

        if (double.IsNaN(appliedAreaSquareMeters) || double.IsInfinity(appliedAreaSquareMeters) || appliedAreaSquareMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(appliedAreaSquareMeters), appliedAreaSquareMeters, "Area must be a non-negative finite value.");
        }

        if (plannedFeatureCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(plannedFeatureCount), plannedFeatureCount, "Feature count must be non-negative.");
        }

        if (appliedFeatureCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(appliedFeatureCount), appliedFeatureCount, "Feature count must be non-negative.");
        }

        Lot = lot;
        PlannedAreaSquareMeters = plannedAreaSquareMeters;
        AppliedAreaSquareMeters = appliedAreaSquareMeters;
        PlannedFeatureCount = plannedFeatureCount;
        AppliedFeatureCount = appliedFeatureCount;
    }

    /// <summary>Gets the lot identifier.</summary>
    public string Lot { get; }

    /// <summary>Gets the total planned area in square metres.</summary>
    public double PlannedAreaSquareMeters { get; }

    /// <summary>Gets the total applied area in square metres.</summary>
    public double AppliedAreaSquareMeters { get; }

    /// <summary>Gets the number of planned features contributing to the lot.</summary>
    public int PlannedFeatureCount { get; }

    /// <summary>Gets the number of applied features contributing to the lot.</summary>
    public int AppliedFeatureCount { get; }
}

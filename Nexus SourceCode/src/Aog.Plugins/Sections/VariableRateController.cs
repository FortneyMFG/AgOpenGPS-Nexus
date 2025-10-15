using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.Layers;
using Aog.Core.Paths;

namespace Aog.Plugins.Sections;

/// <summary>
/// Converts agronomic layer values into commanded section rates.
/// </summary>
public sealed class VariableRateController
{
    private readonly int _sectionCount;
    private readonly double _minimumRate;
    private readonly double _maximumRate;
    private readonly double _defaultRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="VariableRateController"/> class.
    /// </summary>
    /// <param name="sectionCount">Number of sections to command.</param>
    /// <param name="minimumRate">Minimum commanded rate.</param>
    /// <param name="maximumRate">Maximum commanded rate.</param>
    /// <param name="defaultRate">Fallback rate when no layer data is available.</param>
    public VariableRateController(int sectionCount, double minimumRate, double maximumRate, double defaultRate)
    {
        if (sectionCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sectionCount));
        }

        if (minimumRate < 0 || double.IsNaN(minimumRate) || double.IsInfinity(minimumRate))
        {
            throw new ArgumentOutOfRangeException(nameof(minimumRate));
        }

        if (maximumRate < minimumRate || double.IsNaN(maximumRate) || double.IsInfinity(maximumRate))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumRate));
        }

        if (defaultRate < minimumRate || defaultRate > maximumRate)
        {
            throw new ArgumentOutOfRangeException(nameof(defaultRate), "Default rate must lie within the min/max bounds.");
        }

        _sectionCount = sectionCount;
        _minimumRate = minimumRate;
        _maximumRate = maximumRate;
        _defaultRate = defaultRate;
    }

    /// <summary>
    /// Computes commanded rates for each section using the supplied layer.
    /// </summary>
    /// <param name="sections">Section placements in planar coordinates.</param>
    /// <param name="layer">Agronomic layer containing target rates.</param>
    public IReadOnlyList<double> ComputeRates(IReadOnlyList<SectionPlacement> sections, AgronomicLayerDocument layer)
    {
        if (sections is null)
        {
            throw new ArgumentNullException(nameof(sections));
        }

        if (layer is null)
        {
            throw new ArgumentNullException(nameof(layer));
        }

        if (sections.Count != _sectionCount)
        {
            throw new ArgumentException($"Exactly {_sectionCount} sections are required.", nameof(sections));
        }

        if (layer.Cells.Count == 0)
        {
            return Enumerable.Repeat(_defaultRate, _sectionCount).ToArray();
        }

        var rates = new double[_sectionCount];
        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            var cell = FindCell(section.Center, layer.Cells);
            var value = cell?.Value ?? _defaultRate;
            rates[index] = Math.Clamp(value, _minimumRate, _maximumRate);
        }

        return new ReadOnlyCollection<double>(rates);
    }

    private static AgronomicLayerCell? FindCell(PlanarPoint position, IReadOnlyList<AgronomicLayerCell> cells)
    {
        foreach (var cell in cells)
        {
            var half = cell.CellSizeMeters / 2.0;
            var minX = cell.Position.Easting - half;
            var maxX = cell.Position.Easting + half;
            var minY = cell.Position.Northing - half;
            var maxY = cell.Position.Northing + half;

            if (position.Easting >= minX && position.Easting <= maxX
                && position.Northing >= minY && position.Northing <= maxY)
            {
                return cell;
            }
        }

        return null;
    }
}

/// <summary>
/// Describes the planar placement of a boom section.
/// </summary>
/// <param name="Center">Centre position of the section in planar coordinates.</param>
public readonly record struct SectionPlacement(PlanarPoint Center);

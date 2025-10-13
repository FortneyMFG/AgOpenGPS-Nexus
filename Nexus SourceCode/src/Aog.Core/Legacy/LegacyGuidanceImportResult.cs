using System;
using System.Collections.Generic;
using Aog.Core.Paths;
using Aog.Core.Simulation.Configuration;

namespace Aog.Core.Legacy;

/// <summary>
/// Represents the output of the legacy guidance import workflow.
/// </summary>
public sealed class LegacyGuidanceImportResult
{
    public LegacyGuidanceImportResult(
        string fieldName,
        GeographicCoordinate origin,
        IReadOnlyList<LegacyAbLinePlanar> abLines,
        IReadOnlyList<PlanarPoint> boundary,
        SimulationScenarioConfiguration scenario)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ArgumentException("Field name must be provided.", nameof(fieldName));
        }

        FieldName = fieldName;
        Origin = origin;
        AbLines = abLines ?? throw new ArgumentNullException(nameof(abLines));
        Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
        Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
    }

    /// <summary>Gets the friendly field name associated with the import.</summary>
    public string FieldName { get; }

    /// <summary>Gets the geographic origin used for planar projection.</summary>
    public GeographicCoordinate Origin { get; }

    /// <summary>Gets the imported AB lines expressed in the planar frame.</summary>
    public IReadOnlyList<LegacyAbLinePlanar> AbLines { get; }

    /// <summary>Gets the imported boundary polygon expressed in the planar frame.</summary>
    public IReadOnlyList<PlanarPoint> Boundary { get; }

    /// <summary>Gets the scenario configuration that wires the legacy sources into Core routes.</summary>
    public SimulationScenarioConfiguration Scenario { get; }
}

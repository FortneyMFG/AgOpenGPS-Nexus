using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aog.Core.Paths;
using Aog.Core.Zones;
using CoverageAccumulator = Aog.Core.Coverage.CoverageAccumulator;

namespace Aog.Plugins.Mapping;

/// <summary>
/// Maintains per-field state for the mapping plugin including coverage statistics and AB line planners.
/// </summary>
public sealed class FieldStateStore
{
    private readonly Dictionary<string, FieldState> _fields = new(StringComparer.Ordinal);

    /// <summary>
    /// Mounts the supplied fields, replacing any existing state.
    /// </summary>
    /// <param name="fields">Field geometries to track.</param>
    public void MountFields(IEnumerable<FieldGeometry> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        _fields.Clear();

        foreach (var field in fields)
        {
            if (field is null)
            {
                throw new ArgumentException("Fields cannot contain null entries.", nameof(fields));
            }

            if (_fields.ContainsKey(field.FieldId))
            {
                throw new ArgumentException($"Duplicate field identifier '{field.FieldId}'.", nameof(fields));
            }

            _fields[field.FieldId] = new FieldState(field);
        }
    }

    /// <summary>
    /// Begins a new coverage patch for the specified field.
    /// </summary>
    public void BeginCoveragePatch(string fieldId, Aog.Core.Paths.PlanarPoint left, Aog.Core.Paths.PlanarPoint right)
    {
        GetField(fieldId).Coverage.BeginPatch(left.ToCoverage(), right.ToCoverage());
    }

    /// <summary>
    /// Adds a sample to the active coverage patch for the specified field.
    /// </summary>
    public double AddCoverageSample(string fieldId, Aog.Core.Paths.PlanarPoint left, Aog.Core.Paths.PlanarPoint right, bool includeInUserTotals = true)
    {
        return GetField(fieldId).Coverage.AddSample(left.ToCoverage(), right.ToCoverage(), includeInUserTotals);
    }

    /// <summary>
    /// Ends the active coverage patch for the specified field.
    /// </summary>
    public void EndCoveragePatch(string fieldId)
    {
        GetField(fieldId).Coverage.EndPatch();
    }

    /// <summary>
    /// Resets the coverage accumulator for the specified field.
    /// </summary>
    public void ResetCoverage(string fieldId)
    {
        var field = GetField(fieldId);
        _fields[fieldId] = new FieldState(field.Geometry);
    }

    /// <summary>
    /// Replaces the AB line definitions for the specified field.
    /// </summary>
    public void UpdateAbLines(string fieldId, IEnumerable<AbLineDefinition> abLines)
    {
        if (abLines is null)
        {
            throw new ArgumentNullException(nameof(abLines));
        }

        var field = GetField(fieldId);
        field.SetAbLines(abLines);
    }

    /// <summary>
    /// Replaces the zone overlays for the specified field.
    /// </summary>
    /// <param name="fieldId">Field identifier.</param>
    /// <param name="zones">Zone definitions to associate with the field.</param>
    public void UpdateZones(string fieldId, IEnumerable<ZoneDefinition> zones)
    {
        if (zones is null)
        {
            throw new ArgumentNullException(nameof(zones));
        }

        var field = GetField(fieldId);
        field.SetZones(zones);
    }

    /// <summary>
    /// Creates a snapshot of the specified field's state.
    /// </summary>
    public FieldStateSnapshot GetFieldSnapshot(string fieldId)
    {
        return GetField(fieldId).CreateSnapshot();
    }

    /// <summary>
    /// Returns snapshots for all mounted fields.
    /// </summary>
    public IReadOnlyList<FieldStateSnapshot> GetAllFieldSnapshots()
    {
        return _fields.Values
            .Select(state => state.CreateSnapshot())
            .OrderBy(snapshot => snapshot.Geometry.FieldId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Gets coverage metrics for the specified field.
    /// </summary>
    public FieldCoverageSnapshot GetCoverageSnapshot(string fieldId)
    {
        var field = GetField(fieldId);
        return field.CreateCoverageSnapshot();
    }

    /// <summary>
    /// Returns AB line metadata for the specified field.
    /// </summary>
    public IReadOnlyList<AbLineSnapshot> GetAbLineSnapshots(string fieldId)
    {
        var field = GetField(fieldId);
        return field.CreateAbLineSnapshots();
    }

    /// <summary>
    /// Returns zone overlay metadata for the specified field.
    /// </summary>
    /// <param name="fieldId">Field identifier.</param>
    /// <returns>Ordered zone overlay snapshots.</returns>
    public IReadOnlyList<FieldZoneSnapshot> GetZoneSnapshots(string fieldId)
    {
        var field = GetField(fieldId);
        return field.CreateZoneSnapshots();
    }

    /// <summary>
    /// Computes the AB line passes around the current position.
    /// </summary>
    /// <param name="fieldId">Field identifier.</param>
    /// <param name="abLineId">AB line identifier.</param>
    /// <param name="pivot">Current vehicle pivot point.</param>
    /// <param name="toolOffset">Implement lateral offset (positive shifts implement to the right).</param>
    /// <param name="headingSameWay">True when travelling along the stored AB heading.</param>
    /// <param name="neighborCount">Number of neighbouring passes to include on each side.</param>
    public IReadOnlyList<AbLinePassSnapshot> GetAbLinePasses(
        string fieldId,
        string abLineId,
        PlanarPoint pivot,
        double toolOffset,
        bool headingSameWay,
        int neighborCount = 0)
    {
        return GetField(fieldId).GetAbLinePasses(abLineId, pivot, toolOffset, headingSameWay, neighborCount);
    }

    private FieldState GetField(string fieldId)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new ArgumentException("Field identifier is required.", nameof(fieldId));
        }

        if (!_fields.TryGetValue(fieldId, out var field))
        {
            throw new KeyNotFoundException($"Field '{fieldId}' has not been mounted.");
        }

        return field;
    }

    private sealed class FieldState
    {
        private readonly Dictionary<string, AbLineState> _abLines = new(StringComparer.Ordinal);

        public FieldState(FieldGeometry geometry)
        {
            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));
            Coverage = new CoverageAccumulator
            {
                FieldAreaSquareMeters = geometry.AreaSquareMeters
            };
        }

        public FieldGeometry Geometry { get; }

        public CoverageAccumulator Coverage { get; }

        public void SetAbLines(IEnumerable<AbLineDefinition> definitions)
        {
            _abLines.Clear();

            foreach (var definition in definitions)
            {
                if (definition is null)
                {
                    throw new ArgumentException("AB line definitions cannot contain null entries.", nameof(definitions));
                }

                if (_abLines.ContainsKey(definition.Id))
                {
                    throw new ArgumentException($"Duplicate AB line identifier '{definition.Id}'.", nameof(definitions));
                }

                _abLines[definition.Id] = new AbLineState(definition);
            }
        }

        public FieldStateSnapshot CreateSnapshot()
        {
            return new FieldStateSnapshot(
                Geometry,
                CreateCoverageSnapshot(),
                CreateAbLineSnapshots(),
                CreateZoneSnapshots());
        }

        public FieldCoverageSnapshot CreateCoverageSnapshot()
        {
            return new FieldCoverageSnapshot(
                Geometry.FieldId,
                Coverage.TotalAreaSquareMeters,
                Coverage.UserAreaSquareMeters,
                Coverage.FieldAreaSquareMeters,
                Coverage.CoveragePercent,
                Coverage.PatchCount);
        }

        public IReadOnlyList<AbLineSnapshot> CreateAbLineSnapshots()
        {
            return _abLines.Values
                .Select(state => state.CreateSnapshot())
                .OrderBy(snapshot => snapshot.Id, StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<FieldZoneSnapshot> CreateZoneSnapshots()
        {
            if (_zones.Count == 0)
            {
                return Array.Empty<FieldZoneSnapshot>();
            }

            return _zones
                .Select(zone => zone.CreateSnapshot())
                .OrderByDescending(snapshot => snapshot.Priority)
                .ThenBy(snapshot => snapshot.ZoneId, StringComparer.Ordinal)
                .ToArray();
        }

        public IReadOnlyList<AbLinePassSnapshot> GetAbLinePasses(
            string abLineId,
            PlanarPoint pivot,
            double toolOffset,
            bool headingSameWay,
            int neighborCount)
        {
            if (!_abLines.TryGetValue(abLineId, out var abLine))
            {
                throw new KeyNotFoundException($"AB line '{abLineId}' is not registered for field '{Geometry.FieldId}'.");
            }

            if (neighborCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(neighborCount), neighborCount, "Neighbor count cannot be negative.");
            }

            var planner = abLine.Planner;
            var centreIndex = planner.GetPassIndex(pivot, toolOffset, headingSameWay);
            var passes = new List<AbLinePassSnapshot>((neighborCount * 2) + 1);

            for (var offset = -neighborCount; offset <= neighborCount; offset++)
            {
                var index = centreIndex + offset;
                passes.Add(abLine.CreatePassSnapshot(index, toolOffset, headingSameWay, pivot));
            }

            return passes;
        }
        private readonly List<FieldZoneState> _zones = new();

        public void SetZones(IEnumerable<ZoneDefinition> definitions)
        {
            _zones.Clear();

            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var definition in definitions)
            {
                if (definition is null)
                {
                    throw new ArgumentException("Zone definitions cannot contain null entries.", nameof(definitions));
                }

                if (!seen.Add(definition.ZoneId))
                {
                    throw new ArgumentException($"Duplicate zone identifier '{definition.ZoneId}'.", nameof(definitions));
                }

                _zones.Add(new FieldZoneState(definition));
            }
        }
    }

    private sealed class AbLineState
    {
        public AbLineState(AbLineDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Planner = new ABLinePlanner(definition.PointA, definition.PointB, definition.ToolWidthMeters, definition.OverlapMeters, definition.NudgeMeters, definition.ExtensionLengthMeters);
        }

        public AbLineDefinition Definition { get; }

        public ABLinePlanner Planner { get; }

        public AbLineSnapshot CreateSnapshot()
        {
            return new AbLineSnapshot(
                Definition.Id,
                Definition.PointA,
                Definition.PointB,
                Definition.ToolWidthMeters,
                Definition.OverlapMeters,
                Definition.NudgeMeters,
                Planner.Heading,
                Planner.LaneSpacing,
                Definition.ExtensionLengthMeters);
        }

        public AbLinePassSnapshot CreatePassSnapshot(int passIndex, double toolOffset, bool headingSameWay, PlanarPoint pivot)
        {
            var line = Planner.GetPassLine(passIndex, toolOffset, headingSameWay);
            var distance = Planner.GetSignedDistanceToPass(pivot, passIndex, toolOffset, headingSameWay);

            return new AbLinePassSnapshot(
                Definition.Id,
                passIndex,
                line,
                Planner.Heading,
                Planner.LaneSpacing,
                distance);
        }
    }

    private sealed class FieldZoneState
    {
        public FieldZoneState(ZoneDefinition definition)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public ZoneDefinition Definition { get; }

        public FieldZoneSnapshot CreateSnapshot()
        {
            var geometry = CreateGeometry(Definition.Geometry);
            var area = ComputeArea(geometry);

            return new FieldZoneSnapshot(
                Definition.ZoneId,
                Definition.Label,
                Definition.Type,
                Definition.Priority,
                Definition.Enabled,
                geometry,
                Definition.Buffers,
                Definition.ValidWhen,
                Definition.Provenance,
                area);
        }

        private static FieldZoneGeometry CreateGeometry(ZonePolygon polygon)
        {
            var outer = ToRing(polygon.Exterior);

            if (polygon.Holes.Count == 0)
            {
                return new FieldZoneGeometry(outer);
            }

            var holes = new List<IReadOnlyList<PlanarPoint>>(polygon.Holes.Count);
            foreach (var hole in polygon.Holes)
            {
                holes.Add(ToRing(hole));
            }

            return new FieldZoneGeometry(outer, holes);
        }

        private static IReadOnlyList<PlanarPoint> ToRing(ZoneLinearRing ring)
        {
            var vertices = new PlanarPoint[ring.Vertices.Count];
            for (var i = 0; i < ring.Vertices.Count; i++)
            {
                var vertex = ring.Vertices[i];
                vertices[i] = new PlanarPoint(vertex.Longitude, vertex.Latitude);
            }

            return new ReadOnlyCollection<PlanarPoint>(vertices);
        }

        private static double? ComputeArea(FieldZoneGeometry geometry)
        {
            if (geometry.OuterBoundary.Count < 3)
            {
                return null;
            }

            var area = Math.Abs(PlanarGeometryExtensions.ComputePolygonArea(geometry.OuterBoundary));

            foreach (var hole in geometry.Holes)
            {
                if (hole.Count >= 3)
                {
                    area -= Math.Abs(PlanarGeometryExtensions.ComputePolygonArea(hole));
                }
            }

            return area < 0 ? 0 : area;
        }
    }
}

/// <summary>
/// Immutable description of a field's geometry.
/// </summary>
public sealed class FieldGeometry
{
    private static readonly IReadOnlyList<IReadOnlyList<PlanarPoint>> EmptyRings = Array.Empty<IReadOnlyList<PlanarPoint>>();

    public FieldGeometry(
        string fieldId,
        IReadOnlyList<PlanarPoint> outerBoundary,
        IReadOnlyList<IReadOnlyList<PlanarPoint>>? holes = null,
        double? areaOverride = null)
    {
        if (string.IsNullOrWhiteSpace(fieldId))
        {
            throw new ArgumentException("Field identifier is required.", nameof(fieldId));
        }

        FieldId = fieldId;
        OuterBoundary = new ReadOnlyCollection<PlanarPoint>((outerBoundary ?? throw new ArgumentNullException(nameof(outerBoundary))).ToArray());

        if (OuterBoundary.Count == 0)
        {
            throw new ArgumentException("Field boundary must contain at least one vertex.", nameof(outerBoundary));
        }

        if (holes is null)
        {
            Holes = EmptyRings;
        }
        else
        {
            var normalized = new List<IReadOnlyList<PlanarPoint>>(holes.Count);
            foreach (var ring in holes)
            {
                normalized.Add(new ReadOnlyCollection<PlanarPoint>((ring ?? throw new ArgumentException("Hole rings cannot contain null entries.", nameof(holes))).ToArray()));
            }

            Holes = normalized;
        }

        AreaSquareMeters = areaOverride ?? ComputeArea(OuterBoundary, Holes);
    }

    /// <summary>Gets the unique field identifier.</summary>
    public string FieldId { get; }

    /// <summary>Gets the outer boundary polygon.</summary>
    public IReadOnlyList<PlanarPoint> OuterBoundary { get; }

    /// <summary>Gets the inner holes (headlands) represented as polygons.</summary>
    public IReadOnlyList<IReadOnlyList<PlanarPoint>> Holes { get; }

    /// <summary>Gets the inclusive area in square metres when available.</summary>
    public double? AreaSquareMeters { get; }

    private static double? ComputeArea(IReadOnlyList<PlanarPoint> boundary, IReadOnlyList<IReadOnlyList<PlanarPoint>> holes)
    {
        if (boundary.Count < 3)
        {
            return null;
        }

        var area = Math.Abs(PlanarGeometryExtensions.ComputePolygonArea(boundary));
        foreach (var hole in holes)
        {
            if (hole.Count >= 3)
            {
                area -= Math.Abs(PlanarGeometryExtensions.ComputePolygonArea(hole));
            }
        }

        return area < 0 ? 0 : area;
    }
}

/// <summary>
/// Coverage statistics snapshot for a field.
/// </summary>
/// <param name="FieldId">Field identifier.</param>
/// <param name="TotalAreaSquareMeters">Total mapped area in square metres.</param>
/// <param name="UserAreaSquareMeters">User-toggled area tally in square metres.</param>
/// <param name="FieldAreaSquareMeters">Inclusive field area used for coverage percentage calculations.</param>
/// <param name="CoveragePercent">Coverage percentage relative to the field area.</param>
/// <param name="PatchCount">Number of completed coverage patches.</param>
public sealed record FieldCoverageSnapshot(
    string FieldId,
    double TotalAreaSquareMeters,
    double UserAreaSquareMeters,
    double? FieldAreaSquareMeters,
    double? CoveragePercent,
    int PatchCount);

/// <summary>
/// Immutable snapshot of a field's state.
/// </summary>
/// <param name="Geometry">Field geometry.</param>
/// <param name="Coverage">Coverage statistics.</param>
/// <param name="AbLines">Registered AB lines.</param>
/// <param name="Zones">Registered zone overlays.</param>
public sealed record FieldStateSnapshot(
    FieldGeometry Geometry,
    FieldCoverageSnapshot Coverage,
    IReadOnlyList<AbLineSnapshot> AbLines,
    IReadOnlyList<FieldZoneSnapshot> Zones);

/// <summary>
/// Description of an AB line used by the mapping plugin.
/// </summary>
public sealed class AbLineDefinition
{
    public AbLineDefinition(
        string id,
        PlanarPoint pointA,
        PlanarPoint pointB,
        double toolWidthMeters,
        double overlapMeters,
        double nudgeMeters = 0,
        double extensionLengthMeters = 2000)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("AB line identifier is required.", nameof(id));
        }

        if (!double.IsFinite(toolWidthMeters) || toolWidthMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(toolWidthMeters), toolWidthMeters, "Tool width must be a positive finite value.");
        }

        if (!double.IsFinite(overlapMeters) || overlapMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overlapMeters), overlapMeters, "Overlap must be a non-negative finite value.");
        }

        if (overlapMeters >= toolWidthMeters)
        {
            throw new ArgumentException("Overlap must be smaller than the tool width.", nameof(overlapMeters));
        }

        if (!double.IsFinite(nudgeMeters))
        {
            throw new ArgumentOutOfRangeException(nameof(nudgeMeters), nudgeMeters, "Nudge must be finite.");
        }

        if (!double.IsFinite(extensionLengthMeters) || extensionLengthMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(extensionLengthMeters), extensionLengthMeters, "Extension length must be positive and finite.");
        }

        if (pointA.Equals(pointB))
        {
            throw new ArgumentException("AB line requires two distinct points.", nameof(pointB));
        }

        Id = id;
        PointA = pointA;
        PointB = pointB;
        ToolWidthMeters = toolWidthMeters;
        OverlapMeters = overlapMeters;
        NudgeMeters = nudgeMeters;
        ExtensionLengthMeters = extensionLengthMeters;
    }

    /// <summary>Gets the AB line identifier.</summary>
    public string Id { get; }

    /// <summary>Gets the first point defining the reference line.</summary>
    public PlanarPoint PointA { get; }

    /// <summary>Gets the second point defining the reference line.</summary>
    public PlanarPoint PointB { get; }

    /// <summary>Gets the tool width in metres.</summary>
    public double ToolWidthMeters { get; }

    /// <summary>Gets the overlap between adjacent passes in metres.</summary>
    public double OverlapMeters { get; }

    /// <summary>Gets the lateral nudge applied to all passes in metres.</summary>
    public double NudgeMeters { get; }

    /// <summary>Gets the extension length for generated passes in metres.</summary>
    public double ExtensionLengthMeters { get; }
}

/// <summary>
/// Snapshot of an AB line definition and derived planner characteristics.
/// </summary>
/// <param name="Id">Identifier of the AB line.</param>
/// <param name="PointA">First reference point.</param>
/// <param name="PointB">Second reference point.</param>
/// <param name="ToolWidthMeters">Tool width in metres.</param>
/// <param name="OverlapMeters">Overlap between passes in metres.</param>
/// <param name="NudgeMeters">Applied lateral nudge in metres.</param>
/// <param name="HeadingRadians">Heading of the reference line in radians.</param>
/// <param name="LaneSpacingMeters">Spacing between adjacent passes in metres.</param>
/// <param name="ExtensionLengthMeters">Extension length used when generating pass lines.</param>
public sealed record AbLineSnapshot(
    string Id,
    PlanarPoint PointA,
    PlanarPoint PointB,
    double ToolWidthMeters,
    double OverlapMeters,
    double NudgeMeters,
    double HeadingRadians,
    double LaneSpacingMeters,
    double ExtensionLengthMeters);

/// <summary>
/// Represents a single AB pass generated for the current context.
/// </summary>
/// <param name="AbLineId">Associated AB line identifier.</param>
/// <param name="PassIndex">Pass index relative to the reference line.</param>
/// <param name="Pass">Generated pass line.</param>
/// <param name="HeadingRadians">Heading of the pass in radians.</param>
/// <param name="LaneSpacingMeters">Spacing between adjacent passes.</param>
/// <param name="SignedDistanceMeters">Signed distance from the pass to the pivot.</param>
public sealed record AbLinePassSnapshot(
    string AbLineId,
    int PassIndex,
    PlanarLine Pass,
    double HeadingRadians,
    double LaneSpacingMeters,
    double SignedDistanceMeters);

/// <summary>
/// Snapshot describing a zone overlay registered with the mapping plugin.
/// </summary>
/// <param name="ZoneId">Stable identifier for the zone.</param>
/// <param name="Label">Human readable label for the zone.</param>
/// <param name="Type">Zone classification.</param>
/// <param name="Priority">Priority ordering when zones overlap.</param>
/// <param name="Enabled">Indicates whether the zone is enabled.</param>
/// <param name="Geometry">Polygon geometry expressed in planar coordinates.</param>
/// <param name="Buffers">Drive and work buffers associated with the zone.</param>
/// <param name="ValidWhen">Optional metadata describing when the zone applies.</param>
/// <param name="Provenance">Optional provenance metadata.</param>
/// <param name="AreaSquareMeters">Computed area of the polygon in square metres when available.</param>
public sealed record FieldZoneSnapshot(
    string ZoneId,
    string Label,
    ZoneType Type,
    uint Priority,
    bool Enabled,
    FieldZoneGeometry Geometry,
    ZoneBuffers Buffers,
    ZoneValidWhen? ValidWhen,
    ZoneProvenance? Provenance,
    double? AreaSquareMeters);

/// <summary>
/// Polygon geometry describing a zone overlay in planar coordinates.
/// </summary>
public sealed class FieldZoneGeometry
{
    private static readonly IReadOnlyList<IReadOnlyList<PlanarPoint>> EmptyRings = Array.Empty<IReadOnlyList<PlanarPoint>>();

    public FieldZoneGeometry(IReadOnlyList<PlanarPoint> outerBoundary, IReadOnlyList<IReadOnlyList<PlanarPoint>>? holes = null)
    {
        if (outerBoundary is null)
        {
            throw new ArgumentNullException(nameof(outerBoundary));
        }

        if (outerBoundary.Count == 0)
        {
            throw new ArgumentException("Outer boundary must contain at least one vertex.", nameof(outerBoundary));
        }

        OuterBoundary = new ReadOnlyCollection<PlanarPoint>(outerBoundary.ToArray());

        if (holes is null || holes.Count == 0)
        {
            Holes = EmptyRings;
            return;
        }

        var projected = new List<IReadOnlyList<PlanarPoint>>(holes.Count);
        foreach (var ring in holes)
        {
            if (ring is null)
            {
                throw new ArgumentException("Hole rings cannot contain null entries.", nameof(holes));
            }

            projected.Add(new ReadOnlyCollection<PlanarPoint>(ring.ToArray()));
        }

        Holes = projected;
    }

    /// <summary>Gets the outer boundary polygon.</summary>
    public IReadOnlyList<PlanarPoint> OuterBoundary { get; }

    /// <summary>Gets the optional holes.</summary>
    public IReadOnlyList<IReadOnlyList<PlanarPoint>> Holes { get; }
}

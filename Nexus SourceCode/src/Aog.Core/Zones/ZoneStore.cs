using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Index.Strtree;

namespace Aog.Core.Zones;

/// <summary>
/// In-memory zone persistence service that maintains an R-tree index for spatial queries.
/// </summary>
public sealed class ZoneStore
{
    private static readonly PreparedGeometryFactory PreparedGeometryFactory = new();

    private readonly object _sync = new();
    private readonly GeometryFactory _geometryFactory;
    private readonly Dictionary<string, ZoneRecord> _zones = new(StringComparer.Ordinal);

    private STRtree<ZoneRecord> _spatialIndex = new();
    private ulong _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneStore"/> class.
    /// </summary>
    /// <param name="crsEpsg">EPSG code describing the coordinate reference system for stored zones.</param>
    public ZoneStore(int crsEpsg)
    {
        if (crsEpsg <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(crsEpsg), crsEpsg, "CRS EPSG code must be positive.");
        }

        _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: crsEpsg);
    }

    /// <summary>
    /// Gets the EPSG code for the coordinate reference system associated with the store.
    /// </summary>
    public int CoordinateReferenceSystemEpsg => _geometryFactory.SRID;

    /// <summary>
    /// Gets the current snapshot version. Incremented after every mutation.
    /// </summary>
    public ulong Version
    {
        get
        {
            lock (_sync)
            {
                return _version;
            }
        }
    }

    /// <summary>
    /// Replaces the stored zones with the supplied collection.
    /// </summary>
    /// <param name="zones">Zone definitions that become authoritative.</param>
    /// <returns>Delta describing the change.</returns>
    public ZoneStoreDelta MountZones(IEnumerable<ZoneDefinition> zones)
    {
        if (zones is null)
        {
            throw new ArgumentNullException(nameof(zones));
        }

        var list = zones.ToList();
        var identifiers = new HashSet<string>(StringComparer.Ordinal);
        var records = new List<ZoneRecord>(list.Count);

        foreach (var zone in list)
        {
            if (zone is null)
            {
                throw new ArgumentException("Zones collection cannot contain null entries.", nameof(zones));
            }

            if (!identifiers.Add(zone.ZoneId))
            {
                throw new ArgumentException($"Duplicate zone identifier '{zone.ZoneId}'.", nameof(zones));
            }

            records.Add(CreateRecord(zone));
        }

        lock (_sync)
        {
            var deleted = _zones.Keys
                .Except(identifiers, StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            _zones.Clear();
            foreach (var record in records)
            {
                _zones[record.Definition.ZoneId] = record;
            }

            _version++;
            RebuildSpatialIndex();

            var upserts = Array.AsReadOnly(records.Select(r => r.Definition).ToArray());
            var deletedReadOnly = Array.AsReadOnly(deleted);
            return new ZoneStoreDelta(_version, upserts, deletedReadOnly);
        }
    }

    /// <summary>
    /// Inserts or updates a zone definition.
    /// </summary>
    /// <param name="zone">Zone definition to persist.</param>
    /// <returns>Delta describing the upsert.</returns>
    public ZoneStoreDelta Upsert(ZoneDefinition zone)
    {
        if (zone is null)
        {
            throw new ArgumentNullException(nameof(zone));
        }

        var record = CreateRecord(zone);

        lock (_sync)
        {
            _zones[zone.ZoneId] = record;
            _version++;
            RebuildSpatialIndex();

            var upserts = Array.AsReadOnly(new[] { record.Definition });
            return new ZoneStoreDelta(_version, upserts, Array.AsReadOnly(Array.Empty<string>()));
        }
    }

    /// <summary>
    /// Attempts to remove a zone definition.
    /// </summary>
    /// <param name="zoneId">Identifier of the zone to remove.</param>
    /// <param name="delta">Delta describing the removal when successful.</param>
    /// <returns><c>true</c> when the zone existed and was removed.</returns>
    public bool TryRemove(string zoneId, out ZoneStoreDelta? delta)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Zone identifier is required.", nameof(zoneId));
        }

        lock (_sync)
        {
            if (!_zones.Remove(zoneId))
            {
                delta = null;
                return false;
            }

            _version++;
            RebuildSpatialIndex();

            delta = new ZoneStoreDelta(
                _version,
                Array.AsReadOnly(Array.Empty<ZoneDefinition>()),
                Array.AsReadOnly(new[] { zoneId }));
            return true;
        }
    }

    /// <summary>
    /// Attempts to retrieve a zone definition by identifier.
    /// </summary>
    public bool TryGetZone(string zoneId, out ZoneDefinition? zone)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            throw new ArgumentException("Zone identifier is required.", nameof(zoneId));
        }

        lock (_sync)
        {
            if (_zones.TryGetValue(zoneId, out var record))
            {
                zone = record.Definition;
                return true;
            }

            zone = null;
            return false;
        }
    }

    /// <summary>
    /// Returns a snapshot of zones matching the enabled filter.
    /// </summary>
    /// <param name="includeDisabled">Include disabled zones when <c>true</c>.</param>
    public ZoneStoreSnapshot GetSnapshot(bool includeDisabled = true)
    {
        lock (_sync)
        {
            var ordered = _zones.Values
                .Select(record => record.Definition)
                .Where(zone => includeDisabled || zone.Enabled)
                .OrderByDescending(zone => zone.Priority)
                .ThenBy(zone => zone.ZoneId, StringComparer.Ordinal)
                .ToList();

            return new ZoneStoreSnapshot(_version, new ReadOnlyCollection<ZoneDefinition>(ordered));
        }
    }

    /// <summary>
    /// Lists zones matching the enabled filter.
    /// </summary>
    /// <param name="includeDisabled">Include disabled zones when <c>true</c>.</param>
    public IReadOnlyList<ZoneDefinition> ListZones(bool includeDisabled = true)
    {
        return GetSnapshot(includeDisabled).Zones;
    }

    /// <summary>
    /// Returns zones that intersect the specified bounding box.
    /// </summary>
    /// <param name="bounds">Axis-aligned bounding box in the project CRS.</param>
    /// <param name="includeDisabled">Include disabled zones when <c>true</c>.</param>
    public IReadOnlyList<ZoneDefinition> GetZonesInBounds(ZoneBoundingBox bounds, bool includeDisabled = true)
    {
        var envelope = new Envelope(bounds.MinLongitude, bounds.MaxLongitude, bounds.MinLatitude, bounds.MaxLatitude);

        lock (_sync)
        {
            if (_zones.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<ZoneDefinition>());
            }

            var matches = _spatialIndex.Query(envelope);
            if (matches.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<ZoneDefinition>());
            }

            var queryGeometry = _geometryFactory.ToGeometry(envelope);
            var filtered = new List<ZoneDefinition>(matches.Count);

            foreach (var record in matches)
            {
                if (!includeDisabled && !record.Definition.Enabled)
                {
                    continue;
                }

                if (record.PreparedGeometry.Intersects(queryGeometry))
                {
                    filtered.Add(record.Definition);
                }
            }

            if (filtered.Count == 0)
            {
                return Array.AsReadOnly(Array.Empty<ZoneDefinition>());
            }

            filtered.Sort(CompareZones);
            return new ReadOnlyCollection<ZoneDefinition>(filtered);
        }
    }

    private static int CompareZones(ZoneDefinition left, ZoneDefinition right)
    {
        var priority = right.Priority.CompareTo(left.Priority);
        if (priority != 0)
        {
            return priority;
        }

        return string.Compare(left.ZoneId, right.ZoneId, StringComparison.Ordinal);
    }

    private ZoneRecord CreateRecord(ZoneDefinition zone)
    {
        var geometry = ToGeometry(zone.Geometry);
        var prepared = PreparedGeometryFactory.Prepare(geometry);
        var envelope = geometry.EnvelopeInternal;
        return new ZoneRecord(zone, geometry, prepared, envelope);
    }

    private Geometry ToGeometry(ZonePolygon polygon)
    {
        var shell = _geometryFactory.CreateLinearRing(ToCoordinateArray(polygon.Exterior.Vertices));
        var holes = polygon.Holes.Count == 0
            ? Array.Empty<LinearRing>()
            : polygon.Holes.Select(ring => _geometryFactory.CreateLinearRing(ToCoordinateArray(ring.Vertices))).ToArray();

        Polygon geometry = _geometryFactory.CreatePolygon(shell, holes);
        if (!geometry.IsValid)
        {
            var cleaned = geometry.Buffer(0);
            if (cleaned is Polygon cleanedPolygon)
            {
                geometry = cleanedPolygon;
            }
            else if (cleaned is MultiPolygon multiPolygon && multiPolygon.NumGeometries == 1)
            {
                geometry = (Polygon)multiPolygon.GetGeometryN(0);
            }
            else
            {
                throw new InvalidOperationException("Zone geometry could not be normalised to a polygon.");
            }
        }

        geometry.Normalize();
        return geometry;
    }

    private Coordinate[] ToCoordinateArray(IReadOnlyList<ZoneCoordinate> vertices)
    {
        if (vertices is null)
        {
            throw new ArgumentNullException(nameof(vertices));
        }

        var needsClosure = !vertices[0].Equals(vertices[^1]);
        var coordinateCount = vertices.Count + (needsClosure ? 1 : 0);
        var coordinates = new Coordinate[coordinateCount];

        for (var i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            coordinates[i] = vertex.ElevationMeters.HasValue
                ? new CoordinateZ(vertex.Longitude, vertex.Latitude, vertex.ElevationMeters.Value)
                : new Coordinate(vertex.Longitude, vertex.Latitude);
        }

        if (needsClosure)
        {
            coordinates[^1] = coordinates[0].Copy();
        }

        return coordinates;
    }

    private void RebuildSpatialIndex()
    {
        var tree = new STRtree<ZoneRecord>(_zones.Count);
        foreach (var record in _zones.Values)
        {
            tree.Insert(record.Envelope, record);
        }

        tree.Build();
        _spatialIndex = tree;
    }

    private sealed record ZoneRecord(
        ZoneDefinition Definition,
        Geometry Geometry,
        PreparedGeometry PreparedGeometry,
        Envelope Envelope);
}

/// <summary>
/// Snapshot of the current zone registry.
/// </summary>
/// <param name="Version">Snapshot version.</param>
/// <param name="Zones">Zones included in the snapshot.</param>
public sealed record ZoneStoreSnapshot(ulong Version, IReadOnlyList<ZoneDefinition> Zones);

/// <summary>
/// Delta describing zone mutations.
/// </summary>
/// <param name="Version">Version after the mutation.</param>
/// <param name="Upserts">Zones that were added or updated.</param>
/// <param name="DeletedZoneIds">Identifiers for zones that were removed.</param>
public sealed record ZoneStoreDelta(
    ulong Version,
    IReadOnlyList<ZoneDefinition> Upserts,
    IReadOnlyList<string> DeletedZoneIds);

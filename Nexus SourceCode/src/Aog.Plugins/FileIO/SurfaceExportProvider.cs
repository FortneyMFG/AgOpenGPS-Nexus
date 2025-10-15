using System;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Layers;

namespace Aog.Plugins.FileIO;

/// <summary>
/// Wrapper that delegates GeoJSON export flows to <see cref="FileIoSurfaceService"/>.
/// </summary>
public sealed class SurfaceExportProvider
{
    private readonly FileIoSurfaceService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurfaceExportProvider"/> class.
    /// </summary>
    public SurfaceExportProvider()
        : this(new FileIoSurfaceService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurfaceExportProvider"/> class.
    /// </summary>
    /// <param name="service">The underlying surface service implementation.</param>
    public SurfaceExportProvider(FileIoSurfaceService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Exports the supplied surface document to GeoJSON.
    /// </summary>
    public Task ExportAsync(
        AgronomicLayerDocument document,
        string path,
        CancellationToken cancellationToken = default)
    {
        return _service.ExportSurfaceToGeoJsonAsync(document, path, cancellationToken);
    }
}

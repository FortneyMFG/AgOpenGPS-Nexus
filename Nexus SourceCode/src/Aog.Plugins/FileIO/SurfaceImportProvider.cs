using System;
using Aog.Core.Layers;

namespace Aog.Plugins.FileIO;

/// <summary>
/// Thin wrapper around <see cref="FileIoSurfaceService"/> that exposes import semantics
/// expected by simulation manifests.
/// </summary>
public sealed class SurfaceImportProvider
{
    private readonly FileIoSurfaceService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="SurfaceImportProvider"/> class.
    /// </summary>
    public SurfaceImportProvider()
        : this(new FileIoSurfaceService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SurfaceImportProvider"/> class.
    /// </summary>
    /// <param name="service">The underlying surface service implementation.</param>
    public SurfaceImportProvider(FileIoSurfaceService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Imports a surface from a delimited file and returns the normalized layer document.
    /// </summary>
    public AgronomicLayerDocument Import(
        string path,
        string layerId,
        string kind,
        string units,
        double cellSizeMeters,
        string source,
        string transform,
        string createdBy)
    {
        return _service.ImportSurfaceFromDelimitedFile(
            path,
            layerId,
            kind,
            units,
            cellSizeMeters,
            source,
            transform,
            createdBy);
    }
}

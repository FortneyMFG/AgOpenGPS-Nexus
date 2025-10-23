using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Aog.Plugins.Mapping.Raster;

/// <summary>
/// XYZ templated HTTP tile source (e.g., https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png).
/// </summary>
public sealed class XyzTileSource : ITileSource, IDisposable
{
    private readonly Uri _template;
    private readonly string[] _subdomains;
    private readonly HttpClient _httpClient;
    private int _subdomainIndex;
    private bool _disposed;

    public XyzTileSource(string template, IEnumerable<string>? subdomains = null, HttpMessageHandler? handler = null)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Template is required.", nameof(template));
        }

        _template = new Uri(template, UriKind.Absolute);
        _subdomains = subdomains is null ? Array.Empty<string>() : System.Linq.Enumerable.ToArray(subdomains);
        _httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
    }

    public ValueTask<Stream?> OpenTileAsync(TileId tileId, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(XyzTileSource));
        }

        return OpenAsync(tileId, cancellationToken);
    }

    private async ValueTask<Stream?> OpenAsync(TileId tileId, CancellationToken cancellationToken)
    {
        var uri = BuildUri(tileId);
        try
        {
            var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private Uri BuildUri(TileId tileId)
    {
        var template = _template.OriginalString
            .Replace("{z}", tileId.Zoom.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{x}", tileId.X.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{y}", tileId.Y.ToString(), StringComparison.OrdinalIgnoreCase);

        if (_subdomains.Length > 0)
        {
            var index = System.Threading.Interlocked.Increment(ref _subdomainIndex);
            var subdomain = _subdomains[index % _subdomains.Length];
            template = template.Replace("{s}", subdomain, StringComparison.OrdinalIgnoreCase);
        }

        return new Uri(template, UriKind.Absolute);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }
}

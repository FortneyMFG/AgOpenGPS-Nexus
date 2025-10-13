using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aog.Agio.Linux;

/// <summary>
/// Minimal gpsd client that streams TPV reports over an injected transport.
/// </summary>
public sealed class GpsdClient
{
    private const string WatchCommand = "?WATCH={\"enable\":true,\"json\":true}";

    private readonly IGpsdTransport _transport;
    private readonly ILogger<GpsdClient> _logger;

    public GpsdClient(IGpsdTransport transport, ILogger<GpsdClient> logger)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Connects to gpsd and yields TPV reports until cancellation or disconnection.
    /// </summary>
    public async IAsyncEnumerable<GpsdTpvReport> WatchAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var session = await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
        await session.SendAsync(WatchCommand, cancellationToken).ConfigureAwait(false);

        await foreach (var line in session.ReadLinesAsync(cancellationToken).ConfigureAwait(false))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var report = TryParseTpv(line);
            if (report is not null)
            {
                yield return report;
            }
        }
    }

    private GpsdTpvReport? TryParseTpv(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (!root.TryGetProperty("class", out var classProperty) || classProperty.GetString() is not "TPV")
            {
                return null;
            }

            var device = root.TryGetProperty("device", out var deviceProp) ? deviceProp.GetString() : null;
            var timestamp = root.TryGetProperty("time", out var timeProp) ? TryParseTimestamp(timeProp.GetString()) : null;
            var latitude = root.TryGetProperty("lat", out var latProp) ? latProp.GetDoubleOrNull() : null;
            var longitude = root.TryGetProperty("lon", out var lonProp) ? lonProp.GetDoubleOrNull() : null;
            var altitude = root.TryGetProperty("alt", out var altProp) ? altProp.GetDoubleOrNull() : null;
            var speed = root.TryGetProperty("speed", out var speedProp) ? speedProp.GetDoubleOrNull() : null;
            var track = root.TryGetProperty("track", out var trackProp) ? trackProp.GetDoubleOrNull() : null;
            var mode = root.TryGetProperty("mode", out var modeProp) ? modeProp.GetInt32OrNull() : null;

            return new GpsdTpvReport(device, timestamp, latitude, longitude, altitude, speed, track, mode);
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse gpsd JSON line: {Json}", json);
            return null;
        }
    }

    private static DateTimeOffset? TryParseTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

internal static class JsonElementExtensions
{
    public static double? GetDoubleOrNull(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetDouble(out var value) => value,
            JsonValueKind.Null => null,
            _ => null,
        };
    }

    public static int? GetInt32OrNull(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var value) => value,
            JsonValueKind.Null => null,
            _ => null,
        };
    }
}

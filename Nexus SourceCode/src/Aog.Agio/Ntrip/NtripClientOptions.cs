namespace Aog.Agio.Ntrip;

/// <summary>
/// Configures how the NTRIP client connects to casters and maintains sessions.
/// </summary>
public sealed class NtripClientOptions
{
    private const string DefaultUserAgent = "AgOpenGPS-Nexus/1.0";

    private string _host = string.Empty;
    private string _mountPoint = string.Empty;
    private string _userAgent = DefaultUserAgent;

    /// <summary>
    /// Gets or sets the caster host name or IP address.
    /// </summary>
    public string Host
    {
        get => _host;
        set => _host = value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the TCP port used for the NTRIP session.
    /// </summary>
    public int Port { get; set; } = 2101;

    /// <summary>
    /// Gets or sets the mountpoint identifier requested from the caster.
    /// </summary>
    public string MountPoint
    {
        get => _mountPoint;
        set => _mountPoint = value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Gets or sets the username used for basic authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the password used for basic authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether TLS should be negotiated with the caster.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether invalid TLS certificates are accepted.
    /// </summary>
    public bool AllowInvalidCertificates { get; set; }

    /// <summary>
    /// Gets or sets the user agent advertised during session negotiation.
    /// </summary>
    public string UserAgent
    {
        get => _userAgent;
        set => _userAgent = string.IsNullOrWhiteSpace(value) ? DefaultUserAgent : value.Trim();
    }

    /// <summary>
    /// Gets or sets the maximum amount of time to wait for the TCP connection to establish.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the delay applied after an unexpected disconnect before reconnecting.
    /// </summary>
    public TimeSpan ReconnectBackoff { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the optional NMEA GGA sentence transmitted during negotiation.
    /// </summary>
    public string? NmeaGgaSentence { get; set; }

    /// <summary>
    /// Gets or sets the size of the receive buffer used when streaming corrections.
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 4096;

    /// <summary>
    /// Validates the configured options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host))
        {
            throw new ArgumentException("NTRIP host must be provided.", nameof(Host));
        }

        if (Port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(Port), Port, "Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(MountPoint))
        {
            throw new ArgumentException("NTRIP mountpoint must be provided.", nameof(MountPoint));
        }

        if (MountPoint.Contains(' ', StringComparison.Ordinal))
        {
            throw new ArgumentException("Mountpoint cannot contain whitespace.", nameof(MountPoint));
        }

        if ((Username is null) != (Password is null))
        {
            throw new ArgumentException("Username and password must both be supplied when authentication is enabled.");
        }

        if (ConnectionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ConnectionTimeout), ConnectionTimeout, "Connection timeout must be positive.");
        }

        if (ReconnectBackoff < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ReconnectBackoff), ReconnectBackoff, "Reconnect backoff cannot be negative.");
        }

        if (ReceiveBufferSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ReceiveBufferSize), ReceiveBufferSize, "Receive buffer size must be positive.");
        }

        if (ReceiveBufferSize > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(ReceiveBufferSize), ReceiveBufferSize, "Receive buffer size cannot exceed 65535 bytes.");
        }

        if (!string.IsNullOrEmpty(NmeaGgaSentence))
        {
            if (NmeaGgaSentence.Contains('\r', StringComparison.Ordinal) || NmeaGgaSentence.Contains('\n', StringComparison.Ordinal))
            {
                throw new ArgumentException("GGA sentence must not contain newline characters.", nameof(NmeaGgaSentence));
            }

            if (!NmeaGgaSentence.StartsWith("$", StringComparison.Ordinal))
            {
                throw new ArgumentException("GGA sentence must start with '$'.", nameof(NmeaGgaSentence));
            }
        }

        if (!IsSafeHeaderValue(UserAgent))
        {
            throw new ArgumentException("User agent contains invalid characters.", nameof(UserAgent));
        }
    }

    /// <summary>
    /// Computes the HTTP request path for the configured mountpoint.
    /// </summary>
    /// <returns>The encoded request path.</returns>
    public string GetRequestPath()
    {
        var mount = MountPoint.StartsWith("/", StringComparison.Ordinal) ? MountPoint : $"/{MountPoint}";
        return Uri.EscapeDataString(mount);
    }

    private static bool IsSafeHeaderValue(string value)
    {
        foreach (var ch in value)
        {
            if (ch < ' ' || ch >= 127)
            {
                return false;
            }
        }

        return true;
    }
}

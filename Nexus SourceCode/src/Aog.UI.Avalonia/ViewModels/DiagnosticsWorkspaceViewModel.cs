using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Provides a metadata-rich workspace that surfaces AGiO diagnostics, GPS health,
/// transport loops, and log history in the shell sidebar.
/// </summary>
public sealed class DiagnosticsWorkspaceViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsWorkspaceViewModel"/> class.
    /// </summary>
    /// <param name="telemetry">Telemetry privacy view-model backing upload controls.</param>
    /// <param name="deviceManager">Device Manager compatibility dashboard view-model.</param>
    /// <param name="gps">Snapshot of the current GPS fix state.</param>
    /// <param name="networkChannels">Configured UDP channels and their live health.</param>
    /// <param name="serialProfiles">Serial profiles surfaced by the AGiO host.</param>
    /// <param name="loops">AGiO loop telemetry describing frequency and utilisation.</param>
    /// <param name="events">Log entries highlighting recent diagnostics activity.</param>
    /// <param name="lastUpdatedUtc">Timestamp of the diagnostic sample in UTC.</param>
    /// <param name="connectionSummary">Summary of the active connection configuration.</param>
    /// <param name="logSummary">Optional summary describing where the log data originated.</param>
    public DiagnosticsWorkspaceViewModel(
        TelemetryPrivacyViewModel telemetry,
        DeviceManagerCompatibilityViewModel deviceManager,
        GpsDiagnosticsViewModel gps,
        IReadOnlyList<DiagnosticsChannelViewModel> networkChannels,
        IReadOnlyList<AgioSerialProfileViewModel> serialProfiles,
        IReadOnlyList<AgioLoopViewModel> loops,
        IReadOnlyList<DiagnosticsEventViewModel> events,
        DateTime lastUpdatedUtc,
        string connectionSummary,
        string? logSummary = null)
    {
        Telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        DeviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
        Gps = gps ?? throw new ArgumentNullException(nameof(gps));
        NetworkChannels = networkChannels ?? throw new ArgumentNullException(nameof(networkChannels));
        SerialProfiles = serialProfiles ?? throw new ArgumentNullException(nameof(serialProfiles));
        Loops = loops ?? throw new ArgumentNullException(nameof(loops));
        Events = events ?? throw new ArgumentNullException(nameof(events));
        ConnectionSummary = connectionSummary ?? throw new ArgumentNullException(nameof(connectionSummary));

        if (lastUpdatedUtc.Kind == DateTimeKind.Unspecified)
        {
            lastUpdatedUtc = DateTime.SpecifyKind(lastUpdatedUtc, DateTimeKind.Utc);
        }

        LastUpdatedUtc = lastUpdatedUtc.ToUniversalTime();
        LogSummary = string.IsNullOrWhiteSpace(logSummary)
            ? "Sample diagnostics derived from NX-309/311 telemetry fixtures."
            : logSummary;

        NetworkChannelCollection = new ReadOnlyCollection<DiagnosticsChannelViewModel>(NetworkChannels.ToList());
        SerialProfileCollection = new ReadOnlyCollection<AgioSerialProfileViewModel>(SerialProfiles.ToList());
        LoopCollection = new ReadOnlyCollection<AgioLoopViewModel>(Loops.ToList());
        EventCollection = new ReadOnlyCollection<DiagnosticsEventViewModel>(Events.ToList());
    }

    /// <summary>Gets the telemetry opt-in panel backing crash report uploads.</summary>
    public TelemetryPrivacyViewModel Telemetry { get; }

    /// <summary>Gets the Device Manager compatibility summary powering health roll-ups.</summary>
    public DeviceManagerCompatibilityViewModel DeviceManager { get; }

    /// <summary>Gets the GPS fix snapshot describing RTK and satellite state.</summary>
    public GpsDiagnosticsViewModel Gps { get; }

    /// <summary>Gets the configured network channels exposed by AGiO.</summary>
    public IReadOnlyList<DiagnosticsChannelViewModel> NetworkChannels { get; }

    /// <summary>Gets a read-only collection that XAML bindings can enumerate.</summary>
    public ReadOnlyCollection<DiagnosticsChannelViewModel> NetworkChannelCollection { get; }

    /// <summary>Gets the AGiO serial profiles surfaced in the diagnostics workspace.</summary>
    public IReadOnlyList<AgioSerialProfileViewModel> SerialProfiles { get; }

    /// <summary>Gets a read-only collection for XAML bindings to traverse serial profiles.</summary>
    public ReadOnlyCollection<AgioSerialProfileViewModel> SerialProfileCollection { get; }

    /// <summary>Gets the AGiO loop telemetry describing update rates and utilisation.</summary>
    public IReadOnlyList<AgioLoopViewModel> Loops { get; }

    /// <summary>Gets a read-only collection of AGiO loops for XAML bindings.</summary>
    public ReadOnlyCollection<AgioLoopViewModel> LoopCollection { get; }

    /// <summary>Gets the recent diagnostics events surfaced by the workspace.</summary>
    public IReadOnlyList<DiagnosticsEventViewModel> Events { get; }

    /// <summary>Gets a read-only collection of diagnostics events for binding.</summary>
    public ReadOnlyCollection<DiagnosticsEventViewModel> EventCollection { get; }

    /// <summary>Gets when the diagnostic snapshot was last updated.</summary>
    public DateTime LastUpdatedUtc { get; }

    /// <summary>Gets a human readable display of the timestamp.</summary>
    public string LastUpdatedDisplay => LastUpdatedUtc.ToString("u", CultureInfo.InvariantCulture);

    /// <summary>Gets a summary of the connection configuration powering AGiO.</summary>
    public string ConnectionSummary { get; }

    /// <summary>Gets a short description of the events list provenance.</summary>
    public string LogSummary { get; }

    /// <summary>Gets a telemetry summary string derived from <see cref="Telemetry"/>.</summary>
    public string TelemetrySummary
    {
        get
        {
            if (Telemetry.IsTelemetryOptedIn)
            {
                return Telemetry.PendingReportSummary;
            }

            return "Telemetry uploads are disabled. Enable telemetry to upload crash reports.";
        }
    }

    /// <summary>Gets a device health summary derived from the Device Manager card.</summary>
    public string DeviceHealthSummary => string.Format(
        CultureInfo.InvariantCulture,
        "{0} — {1}",
        DeviceManager.SummaryTitle,
        DeviceManager.SummaryDescription);

    /// <summary>Gets a human readable summary of the GPS fix.</summary>
    public string GpsSummary => string.Format(
        CultureInfo.InvariantCulture,
        "{0} fix · {1} satellites · HDOP {2:0.0}",
        Gps.FixQuality,
        Gps.SatelliteCount,
        Gps.Hdop);

    /// <summary>Gets a roll-up summary of loop and network status.</summary>
    public string OverallSummary
    {
        get
        {
            var channelSummary = NetworkChannels.Count == 0
                ? "No UDP channels configured."
                : string.Join(
                    ", ",
                    NetworkChannels.Select(channel =>
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0}: {1}",
                            channel.Name,
                            channel.StatusDisplay)));

            var loopSummary = Loops.Count == 0
                ? "No AGiO loops reporting telemetry."
                : string.Join(
                    ", ",
                    Loops.Select(loop => loop.StatusDisplay));

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}. {1}. {2}",
                GpsSummary,
                channelSummary,
                loopSummary);
        }
    }

    /// <summary>Gets a value indicating whether any warning/error events are present.</summary>
    public bool HasAlerts => Events.Any(evt => evt.Severity is DiagnosticsEventSeverity.Warning or DiagnosticsEventSeverity.Error or DiagnosticsEventSeverity.Critical);

    /// <summary>
    /// Creates a representative sample workspace used by design-time bindings and tests.
    /// </summary>
    /// <param name="telemetry">Telemetry privacy view-model shared with the shell.</param>
    /// <param name="deviceManager">Device Manager compatibility view-model shared with the shell.</param>
    /// <param name="connection">Connection settings view-model describing the active AGiO endpoint.</param>
    /// <returns>A populated diagnostics workspace.</returns>
    public static DiagnosticsWorkspaceViewModel CreateSample(
        TelemetryPrivacyViewModel telemetry,
        DeviceManagerCompatibilityViewModel deviceManager,
        ConnectionSettingsViewModel connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        // Respect the shared telemetry view-model's current opt-in state. The shell
        // passes a live instance that reflects persisted privacy choices.
        var gps = new GpsDiagnosticsViewModel(
            fixQuality: "RTK Fixed",
            satelliteCount: 16,
            hdop: 0.7,
            vdop: 1.2,
            latitude: 51.042381,
            longitude: -101.857214,
            altitudeMeters: 412.5,
            speedKph: 8.4,
            headingDegrees: 272.6,
            correctionSource: "RTCM3 (NTRIP)",
            ageOfDifferential: TimeSpan.FromSeconds(1.2),
            lastUpdateUtc: DateTime.UtcNow.AddSeconds(-2));

        var networkChannels = new List<DiagnosticsChannelViewModel>
        {
            new(
                name: "pose.loop",
                transport: "UDP",
                endpoint: "239.10.10.10:5501",
                packetsPerSecond: 20.2,
                packetLossPercent: 0.1,
                isConnected: true,
                lastPacketAge: TimeSpan.FromMilliseconds(120)),
            new(
                name: "imu.loop",
                transport: "UDP",
                endpoint: "239.10.10.12:5503",
                packetsPerSecond: 50.0,
                packetLossPercent: 0.6,
                isConnected: true,
                lastPacketAge: TimeSpan.FromMilliseconds(84)),
            new(
                name: "ntrip.corrections",
                transport: "TCP",
                endpoint: "caster.agopengps.local:2101",
                packetsPerSecond: 1.0,
                packetLossPercent: 0,
                isConnected: true,
                lastPacketAge: TimeSpan.FromMilliseconds(600)),
            new(
                name: "telemetry.log",
                transport: "UDP",
                endpoint: "239.10.10.15:5520",
                packetsPerSecond: 5.0,
                packetLossPercent: 4.1,
                isConnected: false,
                lastPacketAge: TimeSpan.FromSeconds(12)),
        };

        var serialProfiles = new List<AgioSerialProfileViewModel>
        {
            new(
                port: "/dev/ttyACM0",
                deviceLabel: "AutoSteer mainboard",
                baudRate: 115200,
                dataBits: 8,
                parity: "None",
                stopBits: 1,
                isActive: true,
                handshake: "RTCM + steer telemetry",
                notes: "USB CDC interface detected with stable clock."),
            new(
                port: "/dev/ttyUSB1",
                deviceLabel: "Rate controller",
                baudRate: 57600,
                dataBits: 8,
                parity: "Even",
                stopBits: 1,
                isActive: false,
                handshake: "ISOXML mapping",
                notes: "Waiting for implement wake-up."),
        };

        var loops = new List<AgioLoopViewModel>
        {
            new(
                loopId: "pose.loop",
                description: "Vehicle pose publish",
                frequencyHz: 20,
                targetFrequencyHz: 20,
                utilisationPercent: 34,
                averageLatencyMilliseconds: 3.2,
                hasBacklog: false),
            new(
                loopId: "section.loop",
                description: "Section control state",
                frequencyHz: 10,
                targetFrequencyHz: 10,
                utilisationPercent: 42,
                averageLatencyMilliseconds: 6.1,
                hasBacklog: false),
            new(
                loopId: "telemetry.persist",
                description: "Telemetry journal",
                frequencyHz: 2,
                targetFrequencyHz: 5,
                utilisationPercent: 78,
                averageLatencyMilliseconds: 18.4,
                hasBacklog: true),
        };

        var events = new List<DiagnosticsEventViewModel>
        {
            new(
                timestampUtc: DateTime.UtcNow.AddSeconds(-42),
                severity: DiagnosticsEventSeverity.Information,
                source: "AgIO",
                message: "pose.loop tick duration 3.1 ms (target 4.0 ms).",
                remediation: null),
            new(
                timestampUtc: DateTime.UtcNow.AddSeconds(-28),
                severity: DiagnosticsEventSeverity.Warning,
                source: "Telemetry",
                message: "telemetry.persist loop running below target frequency (2 Hz vs target 5 Hz).",
                remediation: "Check disk bandwidth and reduce log retention if the warning persists."),
            new(
                timestampUtc: DateTime.UtcNow.AddSeconds(-12),
                severity: DiagnosticsEventSeverity.Error,
                source: "UDP",
                message: "telemetry.log channel dropped 4.1% packets in the last minute.",
                remediation: "Validate switch QoS rules for multicast 239.10.10.15:5520."),
        };

        var connectionSummary = string.Format(
            CultureInfo.InvariantCulture,
            "Backend: {0} · Endpoint: {1} · GPS policy: {2}",
            connection.SelectedBackend,
            connection.AgioEndpoint,
            connection.SelectedGpsSourcePolicy);

        return new DiagnosticsWorkspaceViewModel(
            telemetry,
            deviceManager,
            gps,
            networkChannels,
            serialProfiles,
            loops,
            events,
            DateTime.UtcNow,
            connectionSummary,
            "Recent telemetry sampled from NX-301 regression fixtures.");
    }
}

/// <summary>Represents a configured network transport channel and its health.</summary>
public sealed class DiagnosticsChannelViewModel
{
    public DiagnosticsChannelViewModel(
        string name,
        string transport,
        string endpoint,
        double packetsPerSecond,
        double packetLossPercent,
        bool isConnected,
        TimeSpan lastPacketAge)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Channel name must be provided.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(transport))
        {
            throw new ArgumentException("Transport must be provided.", nameof(transport));
        }

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new ArgumentException("Endpoint must be provided.", nameof(endpoint));
        }

        if (packetsPerSecond < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packetsPerSecond));
        }

        if (packetLossPercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(packetLossPercent));
        }

        Name = name;
        Transport = transport;
        Endpoint = endpoint;
        PacketsPerSecond = packetsPerSecond;
        PacketLossPercent = packetLossPercent;
        IsConnected = isConnected;
        LastPacketAge = lastPacketAge < TimeSpan.Zero ? TimeSpan.Zero : lastPacketAge;
    }

    /// <summary>Gets the friendly name of the transport channel.</summary>
    public string Name { get; }

    /// <summary>Gets the transport type (UDP/TCP/etc.).</summary>
    public string Transport { get; }

    /// <summary>Gets the remote endpoint or multicast group.</summary>
    public string Endpoint { get; }

    /// <summary>Gets the observed packets per second.</summary>
    public double PacketsPerSecond { get; }

    /// <summary>Gets the observed packet loss percentage.</summary>
    public double PacketLossPercent { get; }

    /// <summary>Gets a value indicating whether the channel is connected.</summary>
    public bool IsConnected { get; }

    /// <summary>Gets how long ago the last packet was seen.</summary>
    public TimeSpan LastPacketAge { get; }

    /// <summary>Gets the health classification derived from packet loss/connection state.</summary>
    public DiagnosticsChannelHealth Health
    {
        get
        {
            if (!IsConnected || LastPacketAge > TimeSpan.FromSeconds(5))
            {
                return DiagnosticsChannelHealth.Offline;
            }

            if (PacketLossPercent > 2)
            {
                return DiagnosticsChannelHealth.Degraded;
            }

            return DiagnosticsChannelHealth.Healthy;
        }
    }

    /// <summary>Gets a short status string summarising rate and loss.</summary>
    public string StatusDisplay => string.Format(
        CultureInfo.InvariantCulture,
        "{0:0.0} pkt/s · loss {1:0.0}%",
        PacketsPerSecond,
        PacketLossPercent);
}

/// <summary>Enumerates the high level health state of a diagnostics channel.</summary>
public enum DiagnosticsChannelHealth
{
    /// <summary>The channel is healthy with low loss and recent packets.</summary>
    Healthy,

    /// <summary>The channel is connected but reporting packet loss.</summary>
    Degraded,

    /// <summary>The channel is offline or has stopped reporting packets.</summary>
    Offline,
}

/// <summary>Represents an AGiO serial profile surfaced in the workspace.</summary>
public sealed class AgioSerialProfileViewModel
{
    public AgioSerialProfileViewModel(
        string port,
        string deviceLabel,
        int baudRate,
        int dataBits,
        string parity,
        int stopBits,
        bool isActive,
        string handshake,
        string notes)
    {
        if (string.IsNullOrWhiteSpace(port))
        {
            throw new ArgumentException("Port must be provided.", nameof(port));
        }

        if (string.IsNullOrWhiteSpace(deviceLabel))
        {
            throw new ArgumentException("Device label must be provided.", nameof(deviceLabel));
        }

        if (baudRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baudRate));
        }

        if (dataBits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dataBits));
        }

        if (stopBits <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stopBits));
        }

        Port = port;
        DeviceLabel = deviceLabel;
        BaudRate = baudRate;
        DataBits = dataBits;
        Parity = parity ?? "Unknown";
        StopBits = stopBits;
        IsActive = isActive;
        Handshake = handshake ?? string.Empty;
        Notes = notes ?? string.Empty;
    }

    /// <summary>Gets the system path of the serial device.</summary>
    public string Port { get; }

    /// <summary>Gets the friendly device label.</summary>
    public string DeviceLabel { get; }

    /// <summary>Gets the configured baud rate.</summary>
    public int BaudRate { get; }

    /// <summary>Gets the configured data bits.</summary>
    public int DataBits { get; }

    /// <summary>Gets the configured parity string.</summary>
    public string Parity { get; }

    /// <summary>Gets the configured stop bits.</summary>
    public int StopBits { get; }

    /// <summary>Gets a value indicating whether the profile is actively streaming.</summary>
    public bool IsActive { get; }

    /// <summary>Gets the handshake/transport descriptor.</summary>
    public string Handshake { get; }

    /// <summary>Gets additional notes about the device.</summary>
    public string Notes { get; }

    /// <summary>Gets a formatted summary of the serial configuration.</summary>
    public string ConfigurationDisplay => string.Format(
        CultureInfo.InvariantCulture,
        "{0} baud · {1}{2}{3}",
        BaudRate,
        DataBits,
        Parity.StartsWith("None", StringComparison.OrdinalIgnoreCase) ? "N" : Parity[..1].ToUpperInvariant(),
        StopBits);
}

/// <summary>Represents an AGiO loop and its current telemetry characteristics.</summary>
public sealed class AgioLoopViewModel
{
    public AgioLoopViewModel(
        string loopId,
        string description,
        double frequencyHz,
        double targetFrequencyHz,
        double utilisationPercent,
        double averageLatencyMilliseconds,
        bool hasBacklog)
    {
        if (string.IsNullOrWhiteSpace(loopId))
        {
            throw new ArgumentException("Loop identifier must be provided.", nameof(loopId));
        }

        if (frequencyHz < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(frequencyHz));
        }

        if (targetFrequencyHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetFrequencyHz));
        }

        if (utilisationPercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(utilisationPercent));
        }

        if (averageLatencyMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(averageLatencyMilliseconds));
        }

        LoopId = loopId;
        Description = description ?? string.Empty;
        FrequencyHz = frequencyHz;
        TargetFrequencyHz = targetFrequencyHz;
        UtilisationPercent = utilisationPercent;
        AverageLatencyMilliseconds = averageLatencyMilliseconds;
        HasBacklog = hasBacklog;
    }

    /// <summary>Gets the loop identifier.</summary>
    public string LoopId { get; }

    /// <summary>Gets the loop description.</summary>
    public string Description { get; }

    /// <summary>Gets the measured frequency in Hz.</summary>
    public double FrequencyHz { get; }

    /// <summary>Gets the target frequency in Hz.</summary>
    public double TargetFrequencyHz { get; }

    /// <summary>Gets the utilisation percentage.</summary>
    public double UtilisationPercent { get; }

    /// <summary>Gets the mean latency in milliseconds.</summary>
    public double AverageLatencyMilliseconds { get; }

    /// <summary>Gets a value indicating whether backlog was detected.</summary>
    public bool HasBacklog { get; }

    /// <summary>Gets the loop health classification.</summary>
    public DiagnosticsLoopHealth Health
    {
        get
        {
            if (FrequencyHz < TargetFrequencyHz * 0.5 || HasBacklog)
            {
                return DiagnosticsLoopHealth.Degraded;
            }

            if (FrequencyHz < TargetFrequencyHz * 0.9)
            {
                return DiagnosticsLoopHealth.Warning;
            }

            return DiagnosticsLoopHealth.Healthy;
        }
    }

    /// <summary>Gets a formatted status summary for display.</summary>
    public string StatusDisplay => string.Format(
        CultureInfo.InvariantCulture,
        "{0}: {1:0.##} Hz (target {2:0.#} Hz) · util {3:0.#}% · latency {4:0.0} ms",
        LoopId,
        FrequencyHz,
        TargetFrequencyHz,
        UtilisationPercent,
        AverageLatencyMilliseconds);
}

/// <summary>Enumerates health states for AGiO loops.</summary>
public enum DiagnosticsLoopHealth
{
    /// <summary>The loop is meeting frequency targets.</summary>
    Healthy,

    /// <summary>The loop is slightly under target frequency.</summary>
    Warning,

    /// <summary>The loop has backlog or significant drift.</summary>
    Degraded,
}

/// <summary>Represents a diagnostics event surfaced in the workspace.</summary>
public sealed class DiagnosticsEventViewModel
{
    public DiagnosticsEventViewModel(
        DateTime timestampUtc,
        DiagnosticsEventSeverity severity,
        string source,
        string message,
        string? remediation)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException("Source must be provided.", nameof(source));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message must be provided.", nameof(message));
        }

        if (timestampUtc.Kind == DateTimeKind.Unspecified)
        {
            timestampUtc = DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc);
        }

        TimestampUtc = timestampUtc.ToUniversalTime();
        Severity = severity;
        Source = source;
        Message = message;
        Remediation = remediation;
    }

    /// <summary>Gets the timestamp of the event.</summary>
    public DateTime TimestampUtc { get; }

    /// <summary>Gets the human readable timestamp string.</summary>
    public string TimestampDisplay => TimestampUtc.ToString("u", CultureInfo.InvariantCulture);

    /// <summary>Gets the severity classification.</summary>
    public DiagnosticsEventSeverity Severity { get; }

    /// <summary>Gets the severity label.</summary>
    public string SeverityDisplay => Severity.ToString();

    /// <summary>Gets the component emitting the event.</summary>
    public string Source { get; }

    /// <summary>Gets the message describing the event.</summary>
    public string Message { get; }

    /// <summary>Gets optional remediation guidance.</summary>
    public string? Remediation { get; }
}

/// <summary>Enumerates event severities surfaced by the diagnostics workspace.</summary>
public enum DiagnosticsEventSeverity
{
    /// <summary>Informational message.</summary>
    Information,

    /// <summary>Warning requiring operator awareness.</summary>
    Warning,

    /// <summary>Error indicating the workflow is degraded.</summary>
    Error,

    /// <summary>Critical fault that requires immediate attention.</summary>
    Critical,
}

/// <summary>Represents a GPS diagnostic snapshot with formatted helper strings.</summary>
public sealed class GpsDiagnosticsViewModel
{
    public GpsDiagnosticsViewModel(
        string fixQuality,
        int satelliteCount,
        double hdop,
        double vdop,
        double latitude,
        double longitude,
        double altitudeMeters,
        double speedKph,
        double headingDegrees,
        string correctionSource,
        TimeSpan ageOfDifferential,
        DateTime lastUpdateUtc)
    {
        if (string.IsNullOrWhiteSpace(fixQuality))
        {
            throw new ArgumentException("Fix quality must be provided.", nameof(fixQuality));
        }

        FixQuality = fixQuality;
        SatelliteCount = satelliteCount;
        Hdop = hdop;
        Vdop = vdop;
        Latitude = latitude;
        Longitude = longitude;
        AltitudeMeters = altitudeMeters;
        SpeedKph = speedKph;
        HeadingDegrees = headingDegrees;
        CorrectionSource = correctionSource ?? string.Empty;
        AgeOfDifferential = ageOfDifferential < TimeSpan.Zero ? TimeSpan.Zero : ageOfDifferential;
        LastUpdateUtc = lastUpdateUtc.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(lastUpdateUtc, DateTimeKind.Utc)
            : lastUpdateUtc.ToUniversalTime();
    }

    /// <summary>Gets the reported fix quality (e.g. RTK Fixed).</summary>
    public string FixQuality { get; }

    /// <summary>Gets the satellite count.</summary>
    public int SatelliteCount { get; }

    /// <summary>Gets the HDOP value.</summary>
    public double Hdop { get; }

    /// <summary>Gets the VDOP value.</summary>
    public double Vdop { get; }

    /// <summary>Gets the current latitude in decimal degrees.</summary>
    public double Latitude { get; }

    /// <summary>Gets the current longitude in decimal degrees.</summary>
    public double Longitude { get; }

    /// <summary>Gets the altitude in metres.</summary>
    public double AltitudeMeters { get; }

    /// <summary>Gets the ground speed in kilometres per hour.</summary>
    public double SpeedKph { get; }

    /// <summary>Gets the vehicle heading in degrees.</summary>
    public double HeadingDegrees { get; }

    /// <summary>Gets the correction source (e.g. NTRIP caster).</summary>
    public string CorrectionSource { get; }

    /// <summary>Gets the age of differential corrections.</summary>
    public TimeSpan AgeOfDifferential { get; }

    /// <summary>Gets when the GPS state was last updated.</summary>
    public DateTime LastUpdateUtc { get; }

    /// <summary>Gets a formatted coordinate string.</summary>
    public string PositionDisplay => string.Format(
        CultureInfo.InvariantCulture,
        "{0:F6}, {1:F6}",
        Latitude,
        Longitude);

    /// <summary>Gets a formatted altitude string.</summary>
    public string AltitudeDisplay => string.Format(
        CultureInfo.InvariantCulture,
        "{0:0.0} m", AltitudeMeters);

    /// <summary>Gets a formatted speed string.</summary>
    public string SpeedDisplay => string.Format(
        CultureInfo.InvariantCulture,
        "{0:0.0} km/h", SpeedKph);

    /// <summary>Gets a formatted heading string.</summary>
    public string HeadingDisplay => string.Format(
        CultureInfo.InvariantCulture,
        "{0:0.0}°", HeadingDegrees);

    /// <summary>Gets a formatted correction summary string.</summary>
    public string CorrectionSummary => string.Format(
        CultureInfo.InvariantCulture,
        "{0} · age {1:0.0}s",
        string.IsNullOrWhiteSpace(CorrectionSource) ? "No correction" : CorrectionSource,
        AgeOfDifferential.TotalSeconds);

    /// <summary>Gets a formatted last update string.</summary>
    public string LastUpdateDisplay => LastUpdateUtc.ToString("u", CultureInfo.InvariantCulture);
}

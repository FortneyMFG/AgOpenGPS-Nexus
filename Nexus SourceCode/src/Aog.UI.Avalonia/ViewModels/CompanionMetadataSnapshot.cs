using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Captures metadata-driven state so companion clients can mirror the desktop experience.
/// </summary>
public sealed record CompanionMetadataSnapshot(
    CompanionLegendSnapshot Legend,
    CompanionInspectorSnapshot Inspector,
    CompanionDashboardSnapshot Dashboard,
    CompanionReplayTimelineSnapshot ReplayTimeline,
    CompanionFieldHealthSnapshot FieldHealth,
    CompanionSimulationSnapshot Simulation,
    CompanionDiagnosticsSnapshot Diagnostics)
{
    /// <summary>
    /// Creates a snapshot from the supplied metadata-driven view-models.
    /// </summary>
    /// <param name="legend">Legend metadata describing visible layers.</param>
    /// <param name="inspector">Inspector metadata for the pinned observation.</param>
    /// <param name="dashboard">Dashboard view-model exposing telemetry history.</param>
    /// <param name="timeline">Replay timeline view-model with bookmarks and charts.</param>
    /// <returns>A snapshot that can be serialised for companion clients.</returns>
    public static CompanionMetadataSnapshot From(
        LayerLegendViewModel legend,
        LayerInspectorViewModel inspector,
        SteerDashboardViewModel dashboard,
        ReplayTimelineViewModel timeline,
        FieldHealthSeverityPanelViewModel fieldHealth,
        SimulationBarViewModel simulation,
        DiagnosticsWorkspaceViewModel diagnostics)
    {
        ArgumentNullException.ThrowIfNull(legend);
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(dashboard);
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(fieldHealth);
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var legendEntries = legend.Entries
            .Select(entry => new CompanionLegendEntry(
                entry.LayerId,
                entry.DisplayName,
                entry.RangeDisplay,
                entry.ModeDisplay,
                entry.Description,
                entry.GradientStart.ToString(),
                entry.GradientEnd.ToString(),
                entry.MinimumValue,
                entry.MaximumValue,
                entry.Units))
            .ToArray();

        var inspectorTransport = inspector.TransportMetadata
            .Select(field => new CompanionInspectorField(field.Label, field.Value, field.Description))
            .ToArray();
        var inspectorPayload = inspector.PayloadMetadata
            .Select(field => new CompanionInspectorField(field.Label, field.Value, field.Description))
            .ToArray();

        var dashboardSeries = dashboard.Series
            .Select(series => new CompanionDashboardSeries(
                series.Id,
                series.Title,
                series.Units,
                series.Values.ToArray(),
                ExtractColor(series.Stroke),
                ExtractColor(series.Fill),
                series.StrokeThickness))
            .ToArray();

        var tuningParameters = dashboard.TuningParameters
            .Select(parameter => new CompanionDashboardParameter(
                parameter.Id,
                parameter.Label,
                parameter.Minimum,
                parameter.Maximum,
                parameter.Step,
                parameter.DisplayFormat,
                parameter.Value))
            .ToArray();

        var bookmarks = timeline.Bookmarks
            .Select(bookmark => new CompanionReplayBookmark(
                bookmark.Label,
                bookmark.Notes,
                bookmark.Timestamp.TotalSeconds,
                bookmark.TimestampDisplay))
            .ToArray();

        var fieldHealthEntries = fieldHealth.Entries
            .Select(entry => new CompanionFieldHealthSeverityEntry(
                entry.Severity,
                entry.DisplayName,
                entry.Summary,
                entry.RecommendedAction,
                entry.AreaImpactDisplay,
                entry.Notes,
                entry.Color.ToString()))
            .ToArray();

        var simulationRoutes = simulation.Routes
            .Select(route => new CompanionSimulationRoute(
                route.Stream,
                route.SelectedSource,
                route.SelectedMode))
            .ToArray();

        var simulationSnapshot = new CompanionSimulationSnapshot(
            simulation.StatusText,
            simulation.PlayPauseLabel,
            simulation.SelectedPlaybackRate,
            simulation.Position.TotalSeconds,
            simulation.Duration.TotalSeconds,
            simulation.ActiveScenarioTitle,
            simulation.ActiveScenarioDescription,
            simulation.ActiveScenarioOptions,
            simulationRoutes);

        var diagnosticsChannels = diagnostics.NetworkChannels
            .Select(channel => new CompanionDiagnosticsChannel(
                channel.Name,
                channel.Transport,
                channel.Endpoint,
                channel.PacketsPerSecond,
                channel.PacketLossPercent,
                channel.StatusDisplay,
                channel.Health.ToString()))
            .ToArray();

        var diagnosticsLoops = diagnostics.Loops
            .Select(loop => new CompanionDiagnosticsLoop(
                loop.LoopId,
                loop.Description,
                loop.FrequencyHz,
                loop.TargetFrequencyHz,
                loop.UtilisationPercent,
                loop.AverageLatencyMilliseconds,
                loop.Health.ToString(),
                loop.HasBacklog,
                loop.StatusDisplay))
            .ToArray();

        var diagnosticsSerialProfiles = diagnostics.SerialProfiles
            .Select(profile => new CompanionDiagnosticsSerialProfile(
                profile.Port,
                profile.DeviceLabel,
                profile.BaudRate,
                profile.DataBits,
                profile.Parity,
                profile.StopBits,
                profile.IsActive,
                profile.Handshake,
                profile.Notes,
                profile.ConfigurationDisplay))
            .ToArray();

        var diagnosticsEvents = diagnostics.Events
            .Select(evt => new CompanionDiagnosticsEvent(
                evt.TimestampDisplay,
                evt.SeverityDisplay,
                evt.Source,
                evt.Message,
                evt.Remediation))
            .ToArray();

        var diagnosticsSnapshot = new CompanionDiagnosticsSnapshot(
            diagnostics.ConnectionSummary,
            diagnostics.GpsSummary,
            new CompanionGpsSnapshot(
                diagnostics.Gps.FixQuality,
                diagnostics.Gps.SatelliteCount,
                diagnostics.Gps.Hdop,
                diagnostics.Gps.Vdop,
                diagnostics.Gps.PositionDisplay,
                diagnostics.Gps.AltitudeDisplay,
                diagnostics.Gps.SpeedDisplay,
                diagnostics.Gps.HeadingDisplay,
                diagnostics.Gps.CorrectionSummary,
                diagnostics.Gps.LastUpdateDisplay),
            diagnostics.TelemetrySummary,
            diagnostics.DeviceHealthSummary,
            diagnosticsChannels,
            diagnosticsLoops,
            diagnosticsSerialProfiles,
            diagnosticsEvents,
            diagnostics.LastUpdatedDisplay,
            diagnostics.LogSummary,
            diagnostics.HasAlerts,
            diagnostics.OverallSummary);

        return new CompanionMetadataSnapshot(
            new CompanionLegendSnapshot(legendEntries),
            new CompanionInspectorSnapshot(
                inspector.LayerId,
                inspector.LayerName,
                inspector.ModeDisplay,
                inspector.Description,
                inspector.ValueDisplay,
                inspector.TargetDisplay,
                inspector.QualityDisplay,
                inspector.WeightDisplay,
                inspector.LocationDisplay,
                inspector.TimestampDisplay,
                inspector.SourceDisplay,
                inspector.RateAvailabilityDisplay,
                inspectorTransport,
                inspectorPayload),
            new CompanionDashboardSnapshot(
                dashboard.Status,
                dashboard.GainSummary,
                dashboardSeries,
                tuningParameters),
            new CompanionReplayTimelineSnapshot(
                timeline.ExportStatus,
                timeline.SpeedSamples.ToArray(),
                timeline.HeadingSamples.ToArray(),
                bookmarks),
            new CompanionFieldHealthSnapshot(
                fieldHealth.LayerDisplayName,
                fieldHealth.StatusMessage,
                fieldHealth.TotalAreaDisplay,
                fieldHealth.LastSurveyedDisplay,
                fieldHealth.ObserverDisplay,
                fieldHealth.FilterSummary,
                fieldHealthEntries),
            simulationSnapshot,
            diagnosticsSnapshot);
    }

    private static string? ExtractColor(IBrush? brush)
    {
        return brush switch
        {
            ISolidColorBrush solid => solid.Color.ToString(),
            _ => null,
        };
    }
}

/// <summary>Legend metadata for companion clients.</summary>
/// <param name="Entries">Legend entries that describe each layer.</param>
public sealed record CompanionLegendSnapshot(IReadOnlyList<CompanionLegendEntry> Entries);

/// <summary>Represents a single legend entry in the companion snapshot.</summary>
/// <param name="LayerId">Stable identifier.</param>
/// <param name="DisplayName">Human readable name.</param>
/// <param name="RangeDisplay">Formatted numeric range.</param>
/// <param name="ModeDisplay">Planned/measured indicator.</param>
/// <param name="Description">Optional helper text.</param>
/// <param name="GradientStart">Gradient start colour.</param>
/// <param name="GradientEnd">Gradient end colour.</param>
/// <param name="MinimumValue">Minimum numeric value.</param>
/// <param name="MaximumValue">Maximum numeric value.</param>
/// <param name="Units">Engineering units, if any.</param>
public sealed record CompanionLegendEntry(
    string LayerId,
    string DisplayName,
    string RangeDisplay,
    string ModeDisplay,
    string? Description,
    string GradientStart,
    string GradientEnd,
    double MinimumValue,
    double MaximumValue,
    string? Units);

/// <summary>Inspector metadata for the pinned cell.</summary>
/// <param name="LayerId">Identifier of the inspected layer.</param>
/// <param name="LayerName">Display name of the layer.</param>
/// <param name="ModeDisplay">Planned/measured indicator.</param>
/// <param name="Description">Optional inspector description.</param>
/// <param name="ValueDisplay">Formatted live value.</param>
/// <param name="TargetDisplay">Formatted target value.</param>
/// <param name="QualityDisplay">Quality string.</param>
/// <param name="WeightDisplay">Sample weight.</param>
/// <param name="LocationDisplay">World coordinate display.</param>
/// <param name="TimestampDisplay">Timestamp display string.</param>
/// <param name="SourceDisplay">Transport source description.</param>
/// <param name="RateAvailability">Rate availability label.</param>
/// <param name="TransportMetadata">Transport metadata key/value pairs.</param>
/// <param name="PayloadMetadata">Payload metadata key/value pairs.</param>
public sealed record CompanionInspectorSnapshot(
    string LayerId,
    string LayerName,
    string ModeDisplay,
    string? Description,
    string ValueDisplay,
    string TargetDisplay,
    string QualityDisplay,
    string WeightDisplay,
    string LocationDisplay,
    string TimestampDisplay,
    string SourceDisplay,
    string RateAvailability,
    IReadOnlyList<CompanionInspectorField> TransportMetadata,
    IReadOnlyList<CompanionInspectorField> PayloadMetadata);

/// <summary>Represents a labelled metadata field.</summary>
/// <param name="Label">Field label.</param>
/// <param name="Value">Field value.</param>
/// <param name="Description">Optional helper text.</param>
public sealed record CompanionInspectorField(string Label, string Value, string? Description);

/// <summary>Dashboard metadata for the steering view.</summary>
/// <param name="Status">Status string shown in the dashboard.</param>
/// <param name="GainSummary">Formatted PID gain summary.</param>
/// <param name="Series">Time-series metadata.</param>
/// <param name="TuningParameters">Parameter definitions for tuning sliders.</param>
public sealed record CompanionDashboardSnapshot(
    string Status,
    string GainSummary,
    IReadOnlyList<CompanionDashboardSeries> Series,
    IReadOnlyList<CompanionDashboardParameter> TuningParameters);

/// <summary>Field health severity metadata for companion clients.</summary>
/// <param name="LayerDisplayName">Display name for the active layer.</param>
/// <param name="StatusMessage">Status summary describing current severity.</param>
/// <param name="TotalAreaDisplay">Formatted display of the impacted area.</param>
/// <param name="LastSurveyedDisplay">Formatted last scouted timestamp.</param>
/// <param name="ObserverDisplay">Display value identifying the observer.</param>
/// <param name="FilterSummary">Summary of active history filters.</param>
/// <param name="Entries">Severity entries published by the plugin.</param>
public sealed record CompanionFieldHealthSnapshot(
    string LayerDisplayName,
    string StatusMessage,
    string TotalAreaDisplay,
    string LastSurveyedDisplay,
    string ObserverDisplay,
    string FilterSummary,
    IReadOnlyList<CompanionFieldHealthSeverityEntry> Entries);

/// <summary>Represents a single severity entry in the companion snapshot.</summary>
/// <param name="Severity">Severity identifier.</param>
/// <param name="DisplayName">Human readable severity name.</param>
/// <param name="Summary">Summary of the severity state.</param>
/// <param name="RecommendedAction">Recommended operator action.</param>
/// <param name="AreaImpactDisplay">Formatted impact description.</param>
/// <param name="Notes">Optional supplemental notes.</param>
/// <param name="Color">Colour encoded as ARGB.</param>
public sealed record CompanionFieldHealthSeverityEntry(
    string Severity,
    string DisplayName,
    string Summary,
    string RecommendedAction,
    string? AreaImpactDisplay,
    string? Notes,
    string Color);

/// <summary>Metadata describing a dashboard series.</summary>
/// <param name="Id">Series identifier.</param>
/// <param name="Title">Display title.</param>
/// <param name="Units">Engineering units.</param>
/// <param name="Values">Historical samples.</param>
/// <param name="StrokeColor">Stroke colour expressed as #AARRGGBB.</param>
/// <param name="FillColor">Fill colour expressed as #AARRGGBB.</param>
/// <param name="StrokeThickness">Stroke thickness used for rendering.</param>
public sealed record CompanionDashboardSeries(
    string Id,
    string Title,
    string Units,
    IReadOnlyList<double> Values,
    string? StrokeColor,
    string? FillColor,
    double StrokeThickness);

/// <summary>Metadata describing a tuning parameter control.</summary>
/// <param name="Id">Parameter identifier.</param>
/// <param name="Label">Display label.</param>
/// <param name="Minimum">Minimum value.</param>
/// <param name="Maximum">Maximum value.</param>
/// <param name="Step">Step value for keyboard nudges.</param>
/// <param name="DisplayFormat">Composite string used for display.</param>
/// <param name="Value">Current value.</param>
public sealed record CompanionDashboardParameter(
    string Id,
    string Label,
    double Minimum,
    double Maximum,
    double Step,
    string DisplayFormat,
    double Value);

/// <summary>Replay timeline metadata used by companion clients.</summary>
/// <param name="ExportStatus">Latest export status.</param>
/// <param name="SpeedSamples">Speed samples used for the chart.</param>
/// <param name="HeadingSamples">Heading change samples.</param>
/// <param name="Bookmarks">Replay bookmarks.</param>
public sealed record CompanionReplayTimelineSnapshot(
    string ExportStatus,
    IReadOnlyList<double> SpeedSamples,
    IReadOnlyList<double> HeadingSamples,
    IReadOnlyList<CompanionReplayBookmark> Bookmarks);

/// <summary>Bookmark metadata surfaced in the replay timeline.</summary>
/// <param name="Label">Bookmark label.</param>
/// <param name="Notes">Extended description.</param>
/// <param name="TimestampSeconds">Timestamp expressed in seconds.</param>
/// <param name="TimestampDisplay">Formatted timestamp string.</param>
public sealed record CompanionReplayBookmark(
    string Label,
    string Notes,
    double TimestampSeconds,
    string TimestampDisplay);

/// <summary>Simulation control metadata for the companion parity harness.</summary>
/// <param name="Status">Playback status text.</param>
/// <param name="PlayPauseLabel">Label for the play/pause affordance.</param>
/// <param name="PlaybackRate">Current playback rate multiplier.</param>
/// <param name="PositionSeconds">Current playback position in seconds.</param>
/// <param name="DurationSeconds">Total duration in seconds.</param>
/// <param name="ScenarioTitle">Title describing the active scenario.</param>
/// <param name="ScenarioDescription">Long-form scenario description.</param>
/// <param name="ScenarioOptions">Formatted scenario options.</param>
/// <param name="Routes">Route selections for each simulation stream.</param>
public sealed record CompanionSimulationSnapshot(
    string Status,
    string PlayPauseLabel,
    double PlaybackRate,
    double PositionSeconds,
    double DurationSeconds,
    string ScenarioTitle,
    string ScenarioDescription,
    string ScenarioOptions,
    IReadOnlyList<CompanionSimulationRoute> Routes);

/// <summary>Represents a single simulation route selection.</summary>
/// <param name="Stream">Stream identifier.</param>
/// <param name="SelectedSource">Selected provider for the stream.</param>
/// <param name="SelectedMode">Routing mode.</param>
public sealed record CompanionSimulationRoute(
    string Stream,
    string SelectedSource,
    string SelectedMode);

/// <summary>Diagnostics workspace metadata exposed to companion clients.</summary>
/// <param name="ConnectionSummary">Summary of the active AGiO connection.</param>
/// <param name="GpsSummary">Short GPS fix summary.</param>
/// <param name="Gps">Detailed GPS metadata.</param>
/// <param name="TelemetrySummary">Telemetry opt-in summary.</param>
/// <param name="DeviceHealthSummary">Device Manager health summary.</param>
/// <param name="NetworkChannels">Diagnostics for network transports.</param>
/// <param name="Loops">Diagnostics for AGiO loops.</param>
/// <param name="SerialProfiles">Diagnostics for serial profiles.</param>
/// <param name="Events">Recent diagnostics events.</param>
/// <param name="LastUpdated">Timestamp of the diagnostics sample.</param>
/// <param name="LogSummary">Summary describing event provenance.</param>
/// <param name="HasAlerts">Indicates whether alerts are present.</param>
/// <param name="OverallSummary">Roll-up summary string.</param>
public sealed record CompanionDiagnosticsSnapshot(
    string ConnectionSummary,
    string GpsSummary,
    CompanionGpsSnapshot Gps,
    string TelemetrySummary,
    string DeviceHealthSummary,
    IReadOnlyList<CompanionDiagnosticsChannel> NetworkChannels,
    IReadOnlyList<CompanionDiagnosticsLoop> Loops,
    IReadOnlyList<CompanionDiagnosticsSerialProfile> SerialProfiles,
    IReadOnlyList<CompanionDiagnosticsEvent> Events,
    string LastUpdated,
    string LogSummary,
    bool HasAlerts,
    string OverallSummary);

/// <summary>Represents the GPS metadata mirrored to companion clients.</summary>
/// <param name="FixQuality">Fix quality string.</param>
/// <param name="SatelliteCount">Number of satellites tracked.</param>
/// <param name="Hdop">Horizontal dilution of precision.</param>
/// <param name="Vdop">Vertical dilution of precision.</param>
/// <param name="Position">Formatted coordinate string.</param>
/// <param name="Altitude">Formatted altitude string.</param>
/// <param name="Speed">Formatted speed string.</param>
/// <param name="Heading">Formatted heading string.</param>
/// <param name="CorrectionSummary">Summary of correction source and age.</param>
/// <param name="LastUpdate">Timestamp describing the last update.</param>
public sealed record CompanionGpsSnapshot(
    string FixQuality,
    int SatelliteCount,
    double Hdop,
    double Vdop,
    string Position,
    string Altitude,
    string Speed,
    string Heading,
    string CorrectionSummary,
    string LastUpdate);

/// <summary>Represents diagnostics for a single network channel.</summary>
/// <param name="Name">Channel name.</param>
/// <param name="Transport">Transport type.</param>
/// <param name="Endpoint">Endpoint address.</param>
/// <param name="PacketsPerSecond">Observed packets per second.</param>
/// <param name="PacketLossPercent">Observed packet loss percentage.</param>
/// <param name="Status">Formatted status string.</param>
/// <param name="Health">Health classification.</param>
public sealed record CompanionDiagnosticsChannel(
    string Name,
    string Transport,
    string Endpoint,
    double PacketsPerSecond,
    double PacketLossPercent,
    string Status,
    string Health);

/// <summary>Represents diagnostics for a single AGiO loop.</summary>
/// <param name="LoopId">Loop identifier.</param>
/// <param name="Description">Loop description.</param>
/// <param name="FrequencyHz">Measured frequency.</param>
/// <param name="TargetFrequencyHz">Target frequency.</param>
/// <param name="UtilisationPercent">Utilisation percentage.</param>
/// <param name="AverageLatencyMilliseconds">Average latency in milliseconds.</param>
/// <param name="Health">Health classification.</param>
/// <param name="HasBacklog">Indicates backlog state.</param>
/// <param name="StatusDisplay">Formatted status string.</param>
public sealed record CompanionDiagnosticsLoop(
    string LoopId,
    string Description,
    double FrequencyHz,
    double TargetFrequencyHz,
    double UtilisationPercent,
    double AverageLatencyMilliseconds,
    string Health,
    bool HasBacklog,
    string StatusDisplay);

/// <summary>Represents diagnostics metadata for a serial profile.</summary>
/// <param name="Port">Serial port path.</param>
/// <param name="DeviceLabel">Friendly device label.</param>
/// <param name="BaudRate">Configured baud rate.</param>
/// <param name="DataBits">Configured data bits.</param>
/// <param name="Parity">Configured parity.</param>
/// <param name="StopBits">Configured stop bits.</param>
/// <param name="IsActive">Indicates whether the port is streaming.</param>
/// <param name="Handshake">Transport handshake summary.</param>
/// <param name="Notes">Additional notes.</param>
/// <param name="ConfigurationDisplay">Formatted configuration summary.</param>
public sealed record CompanionDiagnosticsSerialProfile(
    string Port,
    string DeviceLabel,
    int BaudRate,
    int DataBits,
    string Parity,
    int StopBits,
    bool IsActive,
    string Handshake,
    string Notes,
    string ConfigurationDisplay);

/// <summary>Represents a single diagnostics event for companion parity.</summary>
/// <param name="Timestamp">Timestamp display string.</param>
/// <param name="Severity">Severity classification.</param>
/// <param name="Source">Source subsystem.</param>
/// <param name="Message">Event message.</param>
/// <param name="Remediation">Optional remediation guidance.</param>
public sealed record CompanionDiagnosticsEvent(
    string Timestamp,
    string Severity,
    string Source,
    string Message,
    string? Remediation);

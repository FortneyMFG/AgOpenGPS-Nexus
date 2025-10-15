using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Surfaces metadata-driven observations for the currently pinned map layer cell.
/// </summary>
public sealed class LayerInspectorViewModel : ObservableObject
{
    private readonly ObservableCollection<LayerInspectorFieldViewModel> _transportFields = new();
    private readonly ObservableCollection<LayerInspectorFieldViewModel> _payloadFields = new();
    private readonly ReadOnlyObservableCollection<LayerInspectorFieldViewModel> _transportMetadata;
    private readonly ReadOnlyObservableCollection<LayerInspectorFieldViewModel> _payloadMetadata;
    private string _valueDisplay = "—";
    private string _targetDisplay = "—";
    private string _qualityDisplay = "—";
    private string _weightDisplay = "—";
    private string _locationDisplay = "—";
    private string _timestampDisplay = "—";
    private string _sourceDisplay = "—";
    private bool _isRateUnavailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="LayerInspectorViewModel"/> class.
    /// </summary>
    /// <param name="layerId">Layer identifier sourced from metadata.</param>
    /// <param name="layerName">Human readable layer name.</param>
    /// <param name="isPlanned">Whether the layer represents planned metadata.</param>
    /// <param name="description">Optional description explaining the inspector context.</param>
    public LayerInspectorViewModel(string layerId, string layerName, bool isPlanned, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(layerId))
        {
            throw new ArgumentException("Layer identifier is required.", nameof(layerId));
        }

        if (string.IsNullOrWhiteSpace(layerName))
        {
            throw new ArgumentException("Layer name is required.", nameof(layerName));
        }

        LayerId = layerId;
        LayerName = layerName.Trim();
        IsPlanned = isPlanned;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        ModeDisplay = isPlanned ? "Planned layer" : "Measured layer";

        _transportMetadata = new ReadOnlyObservableCollection<LayerInspectorFieldViewModel>(_transportFields);
        _payloadMetadata = new ReadOnlyObservableCollection<LayerInspectorFieldViewModel>(_payloadFields);
    }

    /// <summary>Gets the layer identifier.</summary>
    public string LayerId { get; }

    /// <summary>Gets the layer name.</summary>
    public string LayerName { get; }

    /// <summary>Gets a value indicating whether the layer represents planned metadata.</summary>
    public bool IsPlanned { get; }

    /// <summary>Gets descriptive helper text for the inspector card.</summary>
    public string? Description { get; }

    /// <summary>Gets a badge describing whether the layer is planned or measured.</summary>
    public string ModeDisplay { get; }

    /// <summary>Gets the formatted value display surfaced at the top of the card.</summary>
    public string ValueDisplay
    {
        get => _valueDisplay;
        private set => SetProperty(ref _valueDisplay, value);
    }

    /// <summary>Gets the formatted target display shown next to the live value.</summary>
    public string TargetDisplay
    {
        get => _targetDisplay;
        private set => SetProperty(ref _targetDisplay, value);
    }

    /// <summary>Gets the formatted quality display.</summary>
    public string QualityDisplay
    {
        get => _qualityDisplay;
        private set => SetProperty(ref _qualityDisplay, value);
    }

    /// <summary>Gets the formatted weight display.</summary>
    public string WeightDisplay
    {
        get => _weightDisplay;
        private set => SetProperty(ref _weightDisplay, value);
    }

    /// <summary>Gets the formatted location display in world coordinates.</summary>
    public string LocationDisplay
    {
        get => _locationDisplay;
        private set => SetProperty(ref _locationDisplay, value);
    }

    /// <summary>Gets the formatted timestamp display.</summary>
    public string TimestampDisplay
    {
        get => _timestampDisplay;
        private set => SetProperty(ref _timestampDisplay, value);
    }

    /// <summary>Gets the transport source description.</summary>
    public string SourceDisplay
    {
        get => _sourceDisplay;
        private set => SetProperty(ref _sourceDisplay, value);
    }

    /// <summary>Gets a value indicating whether the controller flagged rate NA.</summary>
    public bool IsRateUnavailable
    {
        get => _isRateUnavailable;
        private set
        {
            if (SetProperty(ref _isRateUnavailable, value))
            {
                OnPropertyChanged(nameof(RateAvailabilityDisplay));
            }
        }
    }

    /// <summary>Gets a user-friendly message describing rate availability.</summary>
    public string RateAvailabilityDisplay => IsRateUnavailable ? "Rate flagged unavailable" : "Rate available";

    /// <summary>Gets transport metadata derived from the carrier protocol.</summary>
    public ReadOnlyObservableCollection<LayerInspectorFieldViewModel> TransportMetadata => _transportMetadata;

    /// <summary>Gets raw payload metadata rendered inside the inspector.</summary>
    public ReadOnlyObservableCollection<LayerInspectorFieldViewModel> PayloadMetadata => _payloadMetadata;

    /// <summary>Gets a value indicating whether transport metadata is available.</summary>
    public bool HasTransportMetadata => _transportMetadata.Count > 0;

    /// <summary>Gets a value indicating whether payload metadata is available.</summary>
    public bool HasPayloadMetadata => _payloadMetadata.Count > 0;

    /// <summary>
    /// Applies an observation emitted by the layer controller.
    /// </summary>
    /// <param name="value">Observed engineering value.</param>
    /// <param name="valueFormat">Composite string used to format the value.</param>
    /// <param name="units">Engineering units associated with the observation.</param>
    /// <param name="targetValue">Optional target value derived from the plan.</param>
    /// <param name="qualityDisplay">Quality string surfaced to the operator.</param>
    /// <param name="weight">Sample weight contributed by this observation.</param>
    /// <param name="isRateUnavailable">Whether the observation is flagged as unavailable.</param>
    /// <param name="location">World coordinates for the sampled cell.</param>
    /// <param name="timestamp">Timestamp associated with the observation.</param>
    /// <param name="sourceDisplay">Transport source description.</param>
    /// <param name="transportMetadata">Transport metadata key/value pairs.</param>
    /// <param name="payloadMetadata">Raw payload metadata key/value pairs.</param>
    public void ApplyObservation(
        double value,
        string valueFormat,
        string? units,
        double? targetValue,
        string qualityDisplay,
        double weight,
        bool isRateUnavailable,
        Point location,
        DateTimeOffset timestamp,
        string sourceDisplay,
        IEnumerable<KeyValuePair<string, string>> transportMetadata,
        IEnumerable<KeyValuePair<string, string>> payloadMetadata)
    {
        ArgumentNullException.ThrowIfNull(valueFormat);
        ArgumentNullException.ThrowIfNull(qualityDisplay);
        ArgumentNullException.ThrowIfNull(sourceDisplay);
        ArgumentNullException.ThrowIfNull(transportMetadata);
        ArgumentNullException.ThrowIfNull(payloadMetadata);

        ValueDisplay = FormatValue(value, valueFormat, units);
        TargetDisplay = targetValue.HasValue ? FormatValue(targetValue.Value, valueFormat, units) : "—";
        QualityDisplay = string.IsNullOrWhiteSpace(qualityDisplay) ? "—" : qualityDisplay.Trim();
        WeightDisplay = FormattableString.Invariant($"{weight:P0}");
        LocationDisplay = FormattableString.Invariant($"X {location.X:0.0} m / Y {location.Y:0.0} m");
        TimestampDisplay = timestamp.ToString("u", CultureInfo.InvariantCulture);
        SourceDisplay = string.IsNullOrWhiteSpace(sourceDisplay) ? "—" : sourceDisplay.Trim();
        IsRateUnavailable = isRateUnavailable;

        ReplaceMetadata(_transportFields, transportMetadata);
        ReplaceMetadata(_payloadFields, payloadMetadata);
        OnPropertyChanged(nameof(HasTransportMetadata));
        OnPropertyChanged(nameof(HasPayloadMetadata));
    }

    private static string FormatValue(double value, string format, string? units)
    {
        var formatted = string.Format(CultureInfo.InvariantCulture, format, value);
        if (string.IsNullOrWhiteSpace(units))
        {
            return formatted;
        }

        return string.Concat(formatted, " ", units.Trim());
    }

    private static void ReplaceMetadata(
        ObservableCollection<LayerInspectorFieldViewModel> collection,
        IEnumerable<KeyValuePair<string, string>> source)
    {
        collection.Clear();

        foreach (var pair in source)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value is null)
            {
                continue;
            }

            collection.Add(new LayerInspectorFieldViewModel(pair.Key, pair.Value));
        }
    }
}

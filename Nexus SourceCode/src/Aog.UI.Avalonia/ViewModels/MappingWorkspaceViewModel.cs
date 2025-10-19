using System;
using System.Collections.Generic;
using Aog.Core.Machines.Axle;
using Aog.UI.Avalonia.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Aggregates the data required to render the primary mapping workspace.
/// </summary>
public sealed class MappingWorkspaceViewModel : ObservableObject
{
    private readonly VehiclePose _initialPose;
    private VehiclePose _vehiclePose;

    public MappingWorkspaceViewModel(
        IReadOnlyList<MapLayer> layers,
        IReadOnlyList<GuidanceTrack> guidanceTracks,
        VehiclePose vehiclePose,
        LayerLegendViewModel legend,
        AxleCentricProfile? equipmentProfile)
    {
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        GuidanceTracks = guidanceTracks ?? throw new ArgumentNullException(nameof(guidanceTracks));
        _vehiclePose = vehiclePose;
        _initialPose = vehiclePose;
        Legend = legend ?? throw new ArgumentNullException(nameof(legend));
        EquipmentProfile = equipmentProfile;
    }

    /// <summary>Gets the map layers rendered in the workspace.</summary>
    public IReadOnlyList<MapLayer> Layers { get; }

    /// <summary>Gets the guidance tracks displayed on top of the map.</summary>
    public IReadOnlyList<GuidanceTrack> GuidanceTracks { get; }

    /// <summary>Gets or sets the vehicle pose used for the equipment overlay.</summary>
    public VehiclePose VehiclePose
    {
        get => _vehiclePose;
        private set => SetProperty(ref _vehiclePose, value);
    }

    /// <summary>Gets the legend describing the active layers.</summary>
    public LayerLegendViewModel Legend { get; }

    /// <summary>Gets the default equipment profile surfaced to the operator.</summary>
    public AxleCentricProfile? EquipmentProfile { get; }

    /// <summary>Gets a value indicating whether the workspace has any map layers to render.</summary>
    public bool HasLayers => Layers.Count > 0;

    /// <summary>Gets a value indicating whether the workspace is empty.</summary>
    public bool IsEmpty => !HasLayers;

    /// <summary>Applies a heading delta and forward motion in meters to the current pose.</summary>
    /// <param name="forwardMeters">Distance travelled in the vehicle's forward direction.</param>
    /// <param name="headingDeltaDegrees">Change in heading expressed in degrees.</param>
    public void ApplyMovement(double forwardMeters, double headingDeltaDegrees)
    {
        var newHeading = NormalizeHeading(VehiclePose.HeadingDegrees + headingDeltaDegrees);
        var headingRadians = newHeading * Math.PI / 180.0;
        var deltaX = forwardMeters * Math.Cos(headingRadians);
        var deltaY = forwardMeters * Math.Sin(headingRadians);
        var updated = new VehiclePose(
            VehiclePose.X + deltaX,
            VehiclePose.Y + deltaY,
            newHeading);
        VehiclePose = updated;
    }

    /// <summary>Resets the vehicle pose back to its boot-time initial value.</summary>
    public void ResetPose() => VehiclePose = _initialPose;

    private static double NormalizeHeading(double headingDegrees)
    {
        var normalized = headingDegrees % 360.0;
        if (normalized < 0)
        {
            normalized += 360.0;
        }

        return normalized;
    }
}

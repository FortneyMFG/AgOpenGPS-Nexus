using System;
using Aog.Core.V1;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model exposing the AutoSteer plugin command state to the UI.
/// </summary>
public sealed class SteerPanelViewModel : ObservableObject
{
    private const double DefaultMinAngleDeg = -35;
    private const double DefaultMaxAngleDeg = 35;

    private bool _isEnabled;
    private double _targetWheelAngleDegrees;
    private double _feedForward;
    private double _controllerOutput;

    /// <summary>
    /// Gets the minimum wheel angle allowed by the UI slider.
    /// </summary>
    public double MinimumWheelAngleDegrees { get; } = DefaultMinAngleDeg;

    /// <summary>
    /// Gets the maximum wheel angle allowed by the UI slider.
    /// </summary>
    public double MaximumWheelAngleDegrees { get; } = DefaultMaxAngleDeg;

    /// <summary>
    /// Gets or sets a value indicating whether steering control is enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    /// <summary>
    /// Gets or sets the desired wheel angle in degrees.
    /// </summary>
    public double TargetWheelAngleDegrees
    {
        get => _targetWheelAngleDegrees;
        set
        {
            var clamped = Math.Clamp(value, MinimumWheelAngleDegrees, MaximumWheelAngleDegrees);
            SetProperty(ref _targetWheelAngleDegrees, clamped);
        }
    }

    /// <summary>
    /// Gets or sets the feed-forward term supplied by the controller.
    /// </summary>
    public double FeedForward
    {
        get => _feedForward;
        set => SetProperty(ref _feedForward, value);
    }

    /// <summary>
    /// Gets or sets the controller output term reported to hardware.
    /// </summary>
    public double ControllerOutput
    {
        get => _controllerOutput;
        set => SetProperty(ref _controllerOutput, value);
    }

    /// <summary>
    /// Applies the latest <see cref="SteerCmd"/> published by the AutoSteer plugin.
    /// </summary>
    /// <param name="command">Steering command snapshot.</param>
    public void ApplySteerCommand(SteerCmd command)
    {
        ArgumentNullException.ThrowIfNull(command);

        IsEnabled = command.Enable;
        TargetWheelAngleDegrees = command.TargetWheelAngleDeg;
        FeedForward = command.FeedForward;
        ControllerOutput = command.ControllerOutput;
    }
}

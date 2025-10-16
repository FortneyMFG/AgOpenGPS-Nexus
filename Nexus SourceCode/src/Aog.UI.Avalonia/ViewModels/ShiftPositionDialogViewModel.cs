using System;
using System.Windows.Input;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for the shift position dialog.
/// </summary>
public sealed class ShiftPositionDialogViewModel : ObservableObject
{
    private readonly double _nudgeStep;
    private double _offsetEastMeters;
    private double _offsetNorthMeters;
    private double _headingOffsetDegrees;
    private double _appliedEastMeters;
    private double _appliedNorthMeters;
    private double _appliedHeadingDegrees;
    private string _statusMessage = string.Empty;
    private DateTimeOffset? _lastApplied;
    private int _applicationCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShiftPositionDialogViewModel"/> class.
    /// </summary>
    public ShiftPositionDialogViewModel(double nudgeStep = 0.1)
    {
        if (nudgeStep <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nudgeStep));
        }

        _nudgeStep = nudgeStep;

        ApplyCommand = new DelegateCommand(_ => Apply());
        ResetCommand = new DelegateCommand(_ => Reset());
        NudgeNorthCommand = new DelegateCommand(_ => NudgeNorth());
        NudgeSouthCommand = new DelegateCommand(_ => NudgeSouth());
        NudgeEastCommand = new DelegateCommand(_ => NudgeEast());
        NudgeWestCommand = new DelegateCommand(_ => NudgeWest());
        RotateLeftCommand = new DelegateCommand(_ => Rotate(-0.25));
        RotateRightCommand = new DelegateCommand(_ => Rotate(0.25));
    }

    /// <summary>Gets or sets the east offset in meters.</summary>
    public double OffsetEastMeters
    {
        get => _offsetEastMeters;
        set => SetProperty(ref _offsetEastMeters, Math.Round(value, 3));
    }

    /// <summary>Gets or sets the north offset in meters.</summary>
    public double OffsetNorthMeters
    {
        get => _offsetNorthMeters;
        set => SetProperty(ref _offsetNorthMeters, Math.Round(value, 3));
    }

    /// <summary>Gets or sets the heading offset in degrees.</summary>
    public double HeadingOffsetDegrees
    {
        get => _headingOffsetDegrees;
        set
        {
            var normalized = value % 360;
            if (normalized > 180)
            {
                normalized -= 360;
            }
            else if (normalized < -180)
            {
                normalized += 360;
            }

            SetProperty(ref _headingOffsetDegrees, Math.Round(normalized, 2));
        }
    }

    /// <summary>Gets the last applied east offset.</summary>
    public double AppliedEastMeters
    {
        get => _appliedEastMeters;
        private set => SetProperty(ref _appliedEastMeters, value);
    }

    /// <summary>Gets the last applied north offset.</summary>
    public double AppliedNorthMeters
    {
        get => _appliedNorthMeters;
        private set => SetProperty(ref _appliedNorthMeters, value);
    }

    /// <summary>Gets the last applied heading offset.</summary>
    public double AppliedHeadingDegrees
    {
        get => _appliedHeadingDegrees;
        private set => SetProperty(ref _appliedHeadingDegrees, value);
    }

    /// <summary>Gets the number of times apply has been invoked.</summary>
    public int ApplicationCount
    {
        get => _applicationCount;
        private set => SetProperty(ref _applicationCount, value);
    }

    /// <summary>Gets the status message describing the last action.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets the timestamp of the last apply action.</summary>
    public DateTimeOffset? LastApplied
    {
        get => _lastApplied;
        private set => SetProperty(ref _lastApplied, value);
    }

    /// <summary>Gets the command that applies the offsets.</summary>
    public ICommand ApplyCommand { get; }

    /// <summary>Gets the command that resets offsets to zero.</summary>
    public ICommand ResetCommand { get; }

    /// <summary>Gets the command that nudges north.</summary>
    public ICommand NudgeNorthCommand { get; }

    /// <summary>Gets the command that nudges south.</summary>
    public ICommand NudgeSouthCommand { get; }

    /// <summary>Gets the command that nudges east.</summary>
    public ICommand NudgeEastCommand { get; }

    /// <summary>Gets the command that nudges west.</summary>
    public ICommand NudgeWestCommand { get; }

    /// <summary>Gets the command that rotates left.</summary>
    public ICommand RotateLeftCommand { get; }

    /// <summary>Gets the command that rotates right.</summary>
    public ICommand RotateRightCommand { get; }

    /// <summary>
    /// Creates a sample view-model instance with offsets applied.
    /// </summary>
    public static ShiftPositionDialogViewModel CreateSample()
    {
        var viewModel = new ShiftPositionDialogViewModel(0.05)
        {
            OffsetEastMeters = 0.42,
            OffsetNorthMeters = -0.28,
            HeadingOffsetDegrees = 1.6,
            StatusMessage = "Applied offsets from RTK calibration.",
            LastApplied = DateTimeOffset.UtcNow.AddMinutes(-8),
            ApplicationCount = 3,
            AppliedEastMeters = 0.42,
            AppliedNorthMeters = -0.28,
            AppliedHeadingDegrees = 1.6,
        };

        return viewModel;
    }

    private void Apply()
    {
        AppliedEastMeters = OffsetEastMeters;
        AppliedNorthMeters = OffsetNorthMeters;
        AppliedHeadingDegrees = HeadingOffsetDegrees;
        ApplicationCount += 1;
        LastApplied = DateTimeOffset.UtcNow;
        StatusMessage = $"Applied offsets E {AppliedEastMeters:F2} m / N {AppliedNorthMeters:F2} m / Heading {AppliedHeadingDegrees:F2}°";
    }

    private void Reset()
    {
        OffsetEastMeters = 0;
        OffsetNorthMeters = 0;
        HeadingOffsetDegrees = 0;
        StatusMessage = "Reset offsets to zero.";
    }

    private void NudgeNorth() => OffsetNorthMeters += _nudgeStep;

    private void NudgeSouth() => OffsetNorthMeters -= _nudgeStep;

    private void NudgeEast() => OffsetEastMeters += _nudgeStep;

    private void NudgeWest() => OffsetEastMeters -= _nudgeStep;

    private void Rotate(double deltaDegrees)
    {
        HeadingOffsetDegrees += deltaDegrees;
        StatusMessage = $"Adjusted heading by {deltaDegrees:F2}°.";
    }
}

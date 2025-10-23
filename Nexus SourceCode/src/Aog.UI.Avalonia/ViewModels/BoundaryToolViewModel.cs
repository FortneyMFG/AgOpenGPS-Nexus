using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for the boundary tool dialog.
/// </summary>
public sealed class BoundaryToolViewModel : ObservableObject
{
    private const int OperationHistoryLimit = 12;

    private readonly ObservableCollection<BoundaryPolygonViewModel> _polygons;
    private readonly ReadOnlyObservableCollection<BoundaryPolygonViewModel> _polygonsView;
    private readonly ObservableCollection<BoundaryOperationViewModel> _operationJournal;
    private readonly ReadOnlyObservableCollection<BoundaryOperationViewModel> _operationJournalView;

    private readonly DelegateCommand _simplifySelectedCommand;
    private readonly DelegateCommand _mergeWithNextCommand;
    private readonly DelegateCommand _captureBoundaryCommand;
    private readonly DelegateCommand _exportShapefileCommand;
    private readonly DelegateCommand _toggleLockCommand;
    private readonly DelegateCommand _toggleVisibilityCommand;
    private readonly DelegateCommand _startRecordingCommand;
    private readonly DelegateCommand _stopRecordingCommand;

    private BoundaryPolygonViewModel? _selectedPolygon;
    private string _statusMessage = string.Empty;
    private DateTimeOffset? _lastOperation;
    private bool _autoCloseBoundary;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundaryToolViewModel"/> class.
    /// </summary>
    public BoundaryToolViewModel(
        string fieldName,
        double boundaryAreaHectares,
        double exclusionAreaHectares,
        IEnumerable<BoundaryPolygonViewModel> polygons)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        if (boundaryAreaHectares < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(boundaryAreaHectares));
        }

        if (exclusionAreaHectares < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusionAreaHectares));
        }

        FieldName = fieldName;
        BoundaryAreaHectares = boundaryAreaHectares;
        ExclusionAreaHectares = exclusionAreaHectares;

        _polygons = new ObservableCollection<BoundaryPolygonViewModel>(
            (polygons ?? Array.Empty<BoundaryPolygonViewModel>()).ToList());
        _polygonsView = new ReadOnlyObservableCollection<BoundaryPolygonViewModel>(_polygons);

        _operationJournal = new ObservableCollection<BoundaryOperationViewModel>();
        _operationJournalView = new ReadOnlyObservableCollection<BoundaryOperationViewModel>(_operationJournal);

        _simplifySelectedCommand = new DelegateCommand(_ => SimplifySelected(), _ => SelectedPolygon?.CanSimplify == true);
        _mergeWithNextCommand = new DelegateCommand(_ => MergeWithNext(), _ => CanMergeSelected());
        _captureBoundaryCommand = new DelegateCommand(_ => CaptureBoundary());
        _exportShapefileCommand = new DelegateCommand(_ => ExportShapefile(), _ => _polygons.Count > 0);
        _toggleLockCommand = new DelegateCommand(_ => ToggleLock(), _ => SelectedPolygon is not null);
        _toggleVisibilityCommand = new DelegateCommand(_ => ToggleVisibility(), _ => SelectedPolygon is not null);
        _startRecordingCommand = new DelegateCommand(_ => StartRecording());
        _stopRecordingCommand = new DelegateCommand(_ => StopRecording());

        UpdateCommandStates();
    }

    /// <summary>Gets the display name for the active field.</summary>
    public string FieldName { get; }

    /// <summary>Gets the boundary area in hectares.</summary>
    public double BoundaryAreaHectares { get; }

    /// <summary>Gets the exclusion area in hectares.</summary>
    public double ExclusionAreaHectares { get; }

    /// <summary>Gets the list of polygons managed by the dialog.</summary>
    public ReadOnlyObservableCollection<BoundaryPolygonViewModel> Polygons => _polygonsView;

    /// <summary>Gets the operation journal entries.</summary>
    public ReadOnlyObservableCollection<BoundaryOperationViewModel> OperationJournal => _operationJournalView;

    /// <summary>Gets or sets the selected polygon.</summary>
    public BoundaryPolygonViewModel? SelectedPolygon
    {
        get => _selectedPolygon;
        set
        {
            if (SetProperty(ref _selectedPolygon, value))
            {
                UpdateCommandStates();
            }
        }
    }

    /// <summary>Gets the timestamp of the last operation.</summary>
    public DateTimeOffset? LastOperation
    {
        get => _lastOperation;
        private set => SetProperty(ref _lastOperation, value);
    }

    /// <summary>Gets a status message summarising the last operation.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets or sets whether the boundary should be auto-closed.</summary>
    public bool AutoCloseBoundary
    {
        get => _autoCloseBoundary;
        set => SetProperty(ref _autoCloseBoundary, value);
    }

    /// <summary>Gets the command that starts boundary capture.</summary>
    public DelegateCommand StartRecordingCommand => _startRecordingCommand;

    /// <summary>Gets the command that stops boundary capture.</summary>
    public DelegateCommand StopRecordingCommand => _stopRecordingCommand;

    /// <summary>Gets the command that simplifies the selected polygon.</summary>
    public DelegateCommand SimplifySelectedCommand => _simplifySelectedCommand;

    /// <summary>Gets the command that merges the selected polygon with the next compatible polygon.</summary>
    public DelegateCommand MergeWithNextCommand => _mergeWithNextCommand;

    /// <summary>Gets the command that captures a live boundary.</summary>
    public DelegateCommand CaptureBoundaryCommand => _captureBoundaryCommand;

    /// <summary>Gets the command that exports the boundary to a shapefile.</summary>
    public DelegateCommand ExportShapefileCommand => _exportShapefileCommand;

    /// <summary>Gets the command that toggles the lock state for the selected polygon.</summary>
    public DelegateCommand ToggleLockCommand => _toggleLockCommand;

    /// <summary>Gets the command that toggles visibility for the selected polygon.</summary>
    public DelegateCommand ToggleVisibilityCommand => _toggleVisibilityCommand;

    /// <summary>
    /// Creates a sample view-model instance for design-time usage.
    /// </summary>
    public static BoundaryToolViewModel CreateSample()
    {
        var polygons = new[]
        {
            new BoundaryPolygonViewModel(
                "Outer boundary",
                isInclusion: true,
                areaHectares: 28.42,
                perimeterMeters: 3110,
                vertexCount: 684,
                centroid: new Point(0, 0),
                coverageShare: 0.94),
            new BoundaryPolygonViewModel(
                "Headland buffer",
                isInclusion: true,
                areaHectares: 3.20,
                perimeterMeters: 890,
                vertexCount: 248,
                centroid: new Point(4, 6),
                coverageShare: 0.62),
            new BoundaryPolygonViewModel(
                "Shelter belt exclusion",
                isInclusion: false,
                areaHectares: 1.86,
                perimeterMeters: 455,
                vertexCount: 122,
                centroid: new Point(-18, 24),
                coverageShare: 0.12),
            new BoundaryPolygonViewModel(
                "Pond exclusion",
                isInclusion: false,
                areaHectares: 0.74,
                perimeterMeters: 180,
                vertexCount: 86,
                centroid: new Point(12, -6),
                coverageShare: 0.08),
        };

        var viewModel = new BoundaryToolViewModel("East Ridge", 28.42, 2.6, polygons)
        {
            AutoCloseBoundary = true,
        };

        viewModel.SelectedPolygon = polygons.FirstOrDefault();

        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-12);
        viewModel._operationJournal.Add(new BoundaryOperationViewModel("Captured", "Field Boundary", "Imported from recorded coverage.", timestamp));
        viewModel.LastOperation = timestamp;
        viewModel.StatusMessage = "Captured boundary from recorded coverage at 14:32.";
        viewModel.UpdateCommandStates();

        return viewModel;
    }

    private void StartRecording()
    {
        StatusMessage = "Started recording boundary capture.";
        RecordOperation("Start", "Recording", "Began capturing new boundary.");
    }

    private void StopRecording()
    {
        StatusMessage = "Stopped recording boundary capture.";
        RecordOperation("Stop", "Recording", "Stopped boundary capture.");
    }

    private void SimplifySelected()
    {
        if (SelectedPolygon is null)
        {
            return;
        }

        var removedVertices = SelectedPolygon.Simplify();
        RecordOperation("Simplified", SelectedPolygon.DisplayName, $"Removed {removedVertices} vertices.");
        StatusMessage = removedVertices > 0
            ? $"Simplified '{SelectedPolygon.DisplayName}' (removed {removedVertices} vertices)."
            : $"'{SelectedPolygon.DisplayName}' already optimal.";
        UpdateCommandStates();
    }

    private bool CanMergeSelected()
    {
        if (SelectedPolygon is null)
        {
            return false;
        }

        var currentIndex = _polygons.IndexOf(SelectedPolygon);
        if (currentIndex < 0)
        {
            return false;
        }

        return _polygons
            .Skip(currentIndex + 1)
            .Any(p => p.IsInclusion == SelectedPolygon.IsInclusion);
    }

    private void MergeWithNext()
    {
        if (SelectedPolygon is null)
        {
            return;
        }

        var currentIndex = _polygons.IndexOf(SelectedPolygon);
        if (currentIndex < 0)
        {
            return;
        }

        var partner = _polygons
            .Skip(currentIndex + 1)
            .FirstOrDefault(p => p.IsInclusion == SelectedPolygon.IsInclusion);
        if (partner is null)
        {
            return;
        }

        SelectedPolygon.MergeWith(partner);
        _polygons.Remove(partner);

        RecordOperation("Merged", SelectedPolygon.DisplayName, $"Merged with '{partner.DisplayName}'.");
        StatusMessage = $"Merged '{SelectedPolygon.DisplayName}' with '{partner.DisplayName}'.";
        UpdateCommandStates();
    }

    private void CaptureBoundary()
    {
        var newPolygon = new BoundaryPolygonViewModel(
            $"Capture {_polygons.Count + 1}",
            isInclusion: true,
            areaHectares: 0.42,
            perimeterMeters: 138,
            vertexCount: 58,
            centroid: new Point(-6 + _polygons.Count, 4 + _polygons.Count),
            coverageShare: 0.21);

        _polygons.Add(newPolygon);
        SelectedPolygon = newPolygon;

        RecordOperation("Capture", newPolygon.DisplayName, "Created from live coverage capture.");
        StatusMessage = $"Captured '{newPolygon.DisplayName}' from live coverage.";
        UpdateCommandStates();
    }

    private void ExportShapefile()
    {
        RecordOperation("Export", FieldName, "Exported boundary package to SHP/PRJ pair.");
        StatusMessage = "Exported boundary shapefile package to disk.";
    }

    private void ToggleLock()
    {
        if (SelectedPolygon is null)
        {
            return;
        }

        SelectedPolygon.IsLocked = !SelectedPolygon.IsLocked;
        var state = SelectedPolygon.IsLocked ? "locked" : "unlocked";

        RecordOperation("Lock", SelectedPolygon.DisplayName, $"Polygon {state}.");
        StatusMessage = $"{SelectedPolygon.DisplayName} is now {state}.";
    }

    private void ToggleVisibility()
    {
        if (SelectedPolygon is null)
        {
            return;
        }

        SelectedPolygon.IsVisible = !SelectedPolygon.IsVisible;
        var state = SelectedPolygon.IsVisible ? "visible" : "hidden";

        RecordOperation("Visibility", SelectedPolygon.DisplayName, $"Polygon marked {state}.");
        StatusMessage = $"{SelectedPolygon.DisplayName} marked {state}.";
    }

    private void RecordOperation(string action, string subject, string detail)
    {
        var entry = new BoundaryOperationViewModel(action, subject, detail, DateTimeOffset.UtcNow);
        _operationJournal.Insert(0, entry);

        while (_operationJournal.Count > OperationHistoryLimit)
        {
            _operationJournal.RemoveAt(_operationJournal.Count - 1);
        }

        LastOperation = entry.Timestamp;
    }

    private void UpdateCommandStates()
    {
        _simplifySelectedCommand.RaiseCanExecuteChanged();
        _mergeWithNextCommand.RaiseCanExecuteChanged();
        _exportShapefileCommand.RaiseCanExecuteChanged();
        _toggleLockCommand.RaiseCanExecuteChanged();
        _toggleVisibilityCommand.RaiseCanExecuteChanged();
    }
}

/// <summary>
/// Represents a polygon managed by the boundary tool.
/// </summary>
public sealed class BoundaryPolygonViewModel : ObservableObject
{
    private static readonly IBrush InclusionBrush = new SolidColorBrush(Color.FromUInt32(0xFF4CC2FF));
    private static readonly IBrush ExclusionBrush = new SolidColorBrush(Color.FromUInt32(0xFFFFAA5C));

    private readonly double _originalPerimeter;
    private readonly int _originalVertexCount;
    private bool _isLocked;
    private bool _isVisible = true;
    private double _areaHectares;
    private double _coverageShare;
    private double _perimeterMeters;
    private int _vertexCount;
    private Point _centroid;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundaryPolygonViewModel"/> class.
    /// </summary>
    public BoundaryPolygonViewModel(
        string displayName,
        bool isInclusion,
        double areaHectares,
        double perimeterMeters,
        int vertexCount,
        Point centroid,
        double coverageShare)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        if (areaHectares <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(areaHectares));
        }

        if (perimeterMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(perimeterMeters));
        }

        if (vertexCount < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(vertexCount));
        }

        DisplayName = displayName;
        IsInclusion = isInclusion;
        _areaHectares = areaHectares;
        _perimeterMeters = perimeterMeters;
        _originalPerimeter = perimeterMeters;
        _vertexCount = vertexCount;
        _originalVertexCount = vertexCount;
        _centroid = centroid;
        _coverageShare = Math.Clamp(coverageShare, 0, 1);
    }

    /// <summary>Gets the polygon display name.</summary>
    public string DisplayName { get; }

    /// <summary>Gets a value indicating whether the polygon is part of the main boundary.</summary>
    public bool IsInclusion { get; }

    /// <summary>Gets a display string describing the polygon type.</summary>
    public string TypeDisplay => IsInclusion ? "Inclusion" : "Exclusion";

    /// <summary>Gets a display string describing how the polygon is used.</summary>
    public string ModeDisplay => IsInclusion ? "Inclusion boundary" : "Exclusion zone";

    /// <summary>Gets the brush used to render the polygon indicator.</summary>
    public IBrush FillBrush => IsInclusion ? InclusionBrush : ExclusionBrush;

    /// <summary>Gets the polygon area in hectares.</summary>
    public double AreaHectares
    {
        get => _areaHectares;
        private set => SetProperty(ref _areaHectares, value);
    }

    /// <summary>Gets or sets a value indicating whether the polygon is locked.</summary>
    public bool IsLocked
    {
        get => _isLocked;
        set => SetProperty(ref _isLocked, value);
    }

    /// <summary>Gets or sets a value indicating whether the polygon is visible on the map.</summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>Gets or sets the polygon perimeter in meters.</summary>
    public double PerimeterMeters
    {
        get => _perimeterMeters;
        private set => SetProperty(ref _perimeterMeters, value);
    }

    /// <summary>Gets or sets the polygon vertex count.</summary>
    public int VertexCount
    {
        get => _vertexCount;
        private set => SetProperty(ref _vertexCount, value);
    }

    /// <summary>Gets the polygon centroid.</summary>
    public Point Centroid
    {
        get => _centroid;
        private set
        {
            if (SetProperty(ref _centroid, value))
            {
                OnPropertyChanged(nameof(CentroidDisplay));
            }
        }
    }

    /// <summary>Gets a formatted display string representing the centroid.</summary>
    public string CentroidDisplay => string.Format(CultureInfo.InvariantCulture, "{0:F1}, {1:F1}", Centroid.X, Centroid.Y);

    /// <summary>Gets the share of coverage samples that touched the polygon.</summary>
    public double CoverageShare
    {
        get => _coverageShare;
        private set => SetProperty(ref _coverageShare, Math.Clamp(value, 0, 1));
    }

    /// <summary>Gets a value indicating whether the polygon can be simplified.</summary>
    public bool CanSimplify => VertexCount > _originalVertexCount * 0.35;

    /// <summary>
    /// Applies a simplification to the polygon and returns the number of removed vertices.
    /// </summary>
    public int Simplify()
    {
        if (!CanSimplify)
        {
            return 0;
        }

        var removed = Math.Max(4, (int)Math.Round(VertexCount * 0.18));
        VertexCount = Math.Max(12, VertexCount - removed);
        PerimeterMeters = Math.Max(_originalPerimeter * 0.6, PerimeterMeters - (removed * 0.4));
        return removed;
    }

    /// <summary>
    /// Merges the current polygon with the supplied partner.
    /// </summary>
    public void MergeWith(BoundaryPolygonViewModel partner)
    {
        ArgumentNullException.ThrowIfNull(partner);
        if (partner.IsInclusion != IsInclusion)
        {
            throw new InvalidOperationException("Polygons must be the same type to merge.");
        }

        var currentArea = AreaHectares;
        var partnerArea = partner.AreaHectares;
        var combinedArea = currentArea + partnerArea;
        var combinedPerimeter = Math.Max(PerimeterMeters, partner.PerimeterMeters) + Math.Min(PerimeterMeters, partner.PerimeterMeters) * 0.25;
        var combinedVertices = VertexCount + partner.VertexCount;

        var areaRatio = combinedArea <= 0 ? 1 : currentArea / combinedArea;
        var centroidX = (Centroid.X * areaRatio) + (partner.Centroid.X * (1 - areaRatio));
        var centroidY = (Centroid.Y * areaRatio) + (partner.Centroid.Y * (1 - areaRatio));

        AreaHectares = Math.Max(combinedArea, double.Epsilon);
        CoverageShare = combinedArea <= 0
            ? CoverageShare
            : ((currentArea * CoverageShare) + (partnerArea * partner.CoverageShare)) / combinedArea;

        PerimeterMeters = combinedPerimeter;
        VertexCount = Math.Max(12, combinedVertices - 32);
        IsLocked = IsLocked && partner.IsLocked;
        IsVisible = IsVisible || partner.IsVisible;
        Centroid = new Point(centroidX, centroidY);
    }
}

/// <summary>
/// Represents an operation performed in the boundary tool.
/// </summary>
public sealed class BoundaryOperationViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoundaryOperationViewModel"/> class.
    /// </summary>
    public BoundaryOperationViewModel(string action, string subject, string detail, DateTimeOffset timestamp)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Detail = detail ?? throw new ArgumentNullException(nameof(detail));
        Timestamp = timestamp;
    }

    /// <summary>Gets the action label.</summary>
    public string Action { get; }

    /// <summary>Gets the subject of the operation.</summary>
    public string Subject { get; }

    /// <summary>Gets the detail or description.</summary>
    public string Detail { get; }

    /// <summary>Gets the timestamp when the operation occurred.</summary>
    public DateTimeOffset Timestamp { get; }
}

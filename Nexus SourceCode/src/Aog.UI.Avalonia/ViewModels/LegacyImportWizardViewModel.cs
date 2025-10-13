using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Aog.Core.Legacy;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for the legacy guidance import wizard.
/// </summary>
public sealed class LegacyImportWizardViewModel : ObservableObject
{
    private readonly ILegacyGuidanceImportService _importService;
    private readonly Func<LegacyGuidanceImportResult, bool>? _applyCallback;
    private readonly ObservableCollection<LegacyAbLineSummaryViewModel> _abLines = new();

    private LegacyGuidanceImportResult? _result;
    private string? _fieldName;
    private string? _abLineCsvPath;
    private string? _boundaryShapePath;
    private bool _isBusy;
    private bool _hasError;
    private string _statusMessage = "Select legacy exports to begin.";
    private string _boundarySummary = "—";
    private bool _hasImportResult;

    public LegacyImportWizardViewModel(
        ILegacyGuidanceImportService importService,
        Func<LegacyGuidanceImportResult, bool>? applyCallback = null)
    {
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
        _applyCallback = applyCallback;
    }

    /// <summary>Gets or sets the friendly field name.</summary>
    public string? FieldName
    {
        get => _fieldName;
        set => SetProperty(ref _fieldName, value);
    }

    /// <summary>Gets the selected AB line CSV path.</summary>
    public string? AbLineCsvPath
    {
        get => _abLineCsvPath;
        private set
        {
            if (SetProperty(ref _abLineCsvPath, value))
            {
                ResetImportState();
                OnPropertyChanged(nameof(CanImport));
            }
        }
    }

    /// <summary>Gets the selected boundary shapefile path.</summary>
    public string? BoundaryShapePath
    {
        get => _boundaryShapePath;
        private set
        {
            if (SetProperty(ref _boundaryShapePath, value))
            {
                ResetImportState();
                OnPropertyChanged(nameof(CanImport));
            }
        }
    }

    /// <summary>Gets the collection of imported AB line summaries.</summary>
    public ObservableCollection<LegacyAbLineSummaryViewModel> AbLines => _abLines;

    /// <summary>Gets a summary of the imported boundary.</summary>
    public string BoundarySummary
    {
        get => _boundarySummary;
        private set => SetProperty(ref _boundarySummary, value);
    }

    /// <summary>Gets or sets a value indicating whether an import is in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanImport));
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    /// <summary>Gets a value indicating whether the last operation produced an error.</summary>
    public bool HasError
    {
        get => _hasError;
        private set => SetProperty(ref _hasError, value);
    }

    /// <summary>Gets the status message displayed to the user.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets a value indicating whether an import result is available.</summary>
    public bool HasImportResult
    {
        get => _hasImportResult;
        private set
        {
            if (SetProperty(ref _hasImportResult, value))
            {
                OnPropertyChanged(nameof(CanApply));
            }
        }
    }

    /// <summary>Gets a value indicating whether import can be triggered.</summary>
    public bool CanImport => !IsBusy
        && !string.IsNullOrWhiteSpace(AbLineCsvPath)
        && !string.IsNullOrWhiteSpace(BoundaryShapePath);

    /// <summary>Gets a value indicating whether the imported routes can be applied.</summary>
    public bool CanApply => !IsBusy && HasImportResult;

    /// <summary>Updates the selected AB line CSV path.</summary>
    public void SetAbLineCsvPath(string path)
    {
        AbLineCsvPath = path;
    }

    /// <summary>Updates the selected boundary shapefile path.</summary>
    public void SetBoundaryShapePath(string path)
    {
        BoundaryShapePath = path;
        if (string.IsNullOrWhiteSpace(FieldName))
        {
            FieldName = Path.GetFileNameWithoutExtension(path);
        }
    }

    /// <summary>Performs the import using the configured paths.</summary>
    public async Task ImportAsync()
    {
        if (!CanImport)
        {
            HasError = true;
            StatusMessage = "Select both an AB line CSV and a boundary shapefile.";
            return;
        }

        if (!File.Exists(AbLineCsvPath!))
        {
            HasError = true;
            StatusMessage = "AB line CSV file not found.";
            return;
        }

        if (!File.Exists(BoundaryShapePath!))
        {
            HasError = true;
            StatusMessage = "Boundary shapefile not found.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FieldName))
        {
            FieldName = Path.GetFileNameWithoutExtension(BoundaryShapePath!) ?? "Legacy Field";
        }

        try
        {
            IsBusy = true;
            HasError = false;
            StatusMessage = "Importing legacy guidance…";

            using var csvStream = File.OpenRead(AbLineCsvPath!);
            var result = _importService.Import(FieldName!, csvStream, BoundaryShapePath!);
            _result = result;

            _abLines.Clear();
            foreach (var line in result.AbLines)
            {
                _abLines.Add(new LegacyAbLineSummaryViewModel(line.Name, line.LengthMeters, line.HeadingDegrees));
            }

            BoundarySummary = result.Boundary.Count == 0
                ? "No boundary points imported."
                : $"{result.Boundary.Count} boundary points (origin {result.Origin.LatitudeDeg:F6}, {result.Origin.LongitudeDeg:F6})";

            HasImportResult = true;
            StatusMessage = $"Imported {result.AbLines.Count} AB lines and {result.Boundary.Count} boundary points.";
        }
        catch (Exception ex)
        {
            HasError = true;
            HasImportResult = false;
            _result = null;
            _abLines.Clear();
            BoundarySummary = "—";
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Applies the imported routes to the configured callback.</summary>
    public bool TryApplyRoutes()
    {
        if (_result is null || IsBusy)
        {
            HasError = true;
            StatusMessage = "Import legacy data before applying routes.";
            return false;
        }

        if (_applyCallback is null)
        {
            HasError = false;
            StatusMessage = $"Routes applied for '{_result.FieldName}'.";
            return true;
        }

        try
        {
            var applied = _applyCallback(_result);
            if (applied)
            {
                HasError = false;
                StatusMessage = $"Routes applied for '{_result.FieldName}'.";
            }

            return applied;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to apply routes: {ex.Message}";
            return false;
        }
    }

    private void ResetImportState()
    {
        _result = null;
        _abLines.Clear();
        BoundarySummary = "—";
        HasImportResult = false;
        HasError = false;
        StatusMessage = "Select legacy exports to begin.";
    }
}

/// <summary>
/// Presents a summary of an imported legacy AB line.
/// </summary>
public sealed class LegacyAbLineSummaryViewModel
{
    public LegacyAbLineSummaryViewModel(string name, double lengthMeters, double headingDegrees)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name;
        LengthMeters = lengthMeters;
        HeadingDegrees = headingDegrees;
    }

    /// <summary>Gets the AB line label.</summary>
    public string Name { get; }

    /// <summary>Gets the AB line length in metres.</summary>
    public double LengthMeters { get; }

    /// <summary>Gets the AB line heading in degrees.</summary>
    public double HeadingDegrees { get; }

    /// <summary>Gets a formatted length display.</summary>
    public string LengthDisplay => $"{LengthMeters:F1} m";

    /// <summary>Gets a formatted heading display.</summary>
    public string HeadingDisplay => $"{HeadingDegrees:F1}°";
}

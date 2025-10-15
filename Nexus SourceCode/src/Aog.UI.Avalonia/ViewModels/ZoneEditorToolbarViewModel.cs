using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Nodes;
using Aog.Core.Layers;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Surface that coordinates the zone editor toolbar sample used in the shell UI.
/// </summary>
public sealed class ZoneEditorToolbarViewModel : ObservableObject
{
    private const string LayerId = "layer:demo.zones";
    private const string JobId = "job:demo.nexus";

    private readonly ILayerEditEventJournalService _journal;
    private readonly LayerEditEventContext _context;
    private readonly List<ZoneEditorJournalEntryViewModel> _appliedEntries = new();
    private readonly Stack<ZoneEditorJournalEntryViewModel> _redoStack = new();
    private readonly IReadOnlyList<ZoneEditorToolOptionViewModel> _tools;

    private bool _isSessionActive;
    private string? _sessionId;
    private int _sequence;
    private ZoneEditorTool _activeTool;
    private bool _snapToFieldBoundaries = true;
    private bool _snapToGuidance = true;
    private bool _showAttributePanel;
    private string _statusMessage;

    private readonly DelegateCommand _startSessionCommand;
    private readonly DelegateCommand _commitSessionCommand;
    private readonly DelegateCommand _cancelSessionCommand;
    private readonly DelegateCommand _undoCommand;
    private readonly DelegateCommand _redoCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZoneEditorToolbarViewModel"/> class.
    /// </summary>
    /// <param name="journal">Layer edit journal used to create sample entries.</param>
    public ZoneEditorToolbarViewModel(ILayerEditEventJournalService journal)
    {
        _journal = journal ?? throw new ArgumentNullException(nameof(journal));
        _context = new LayerEditEventContext("farm:demo", new[] { "field:north-40" }, "season:2025");
        _statusMessage = "Open the zone editor to begin editing planned agronomy overlays.";

        _tools = CreateTools();
        _activeTool = ZoneEditorTool.Polygon;
        UpdateToolSelection();

        JournalEntries = new ObservableCollection<ZoneEditorJournalEntryViewModel>();

        _startSessionCommand = new DelegateCommand(_ => StartSession(), _ => !IsSessionActive);
        _commitSessionCommand = new DelegateCommand(_ => CommitSession(), _ => IsSessionActive);
        _cancelSessionCommand = new DelegateCommand(_ => CancelSession(), _ => IsSessionActive);
        _undoCommand = new DelegateCommand(_ => UndoLast(), _ => UndoCount > 0);
        _redoCommand = new DelegateCommand(_ => RedoLast(), _ => RedoCount > 0);
    }

    /// <summary>Gets the tools shown in the toolbar.</summary>
    public IReadOnlyList<ZoneEditorToolOptionViewModel> Tools => _tools;

    /// <summary>Gets the entries committed during the active session.</summary>
    public ObservableCollection<ZoneEditorJournalEntryViewModel> JournalEntries { get; }

    /// <summary>Gets the text describing the active layer.</summary>
    public string ActiveLayerDisplayName => "Demo agronomy zones";

    /// <summary>Gets a description of the current session state.</summary>
    public string SessionStatusDisplay =>
        IsSessionActive && _sessionId is not null
            ? $"Editing session {_sessionId} ({UndoCount} undo / {RedoCount} redo)"
            : "No active zone editing session";

    /// <summary>Gets or sets a value indicating whether snap-to-field-boundaries is enabled.</summary>
    public bool SnapToFieldBoundaries
    {
        get => _snapToFieldBoundaries;
        set => SetProperty(ref _snapToFieldBoundaries, value);
    }

    /// <summary>Gets or sets a value indicating whether guidance snapping is enabled.</summary>
    public bool SnapToGuidance
    {
        get => _snapToGuidance;
        set => SetProperty(ref _snapToGuidance, value);
    }

    /// <summary>Gets or sets a value indicating whether the attribute inspector is visible.</summary>
    public bool ShowAttributePanel
    {
        get => _showAttributePanel;
        set => SetProperty(ref _showAttributePanel, value);
    }

    /// <summary>Gets the currently active drawing tool.</summary>
    public ZoneEditorTool ActiveTool
    {
        get => _activeTool;
        private set
        {
            if (!SetProperty(ref _activeTool, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ActiveToolDisplay));
            UpdateToolSelection();
        }
    }

    /// <summary>Gets a friendly label for the active tool.</summary>
    public string ActiveToolDisplay => ActiveTool switch
    {
        ZoneEditorTool.Polygon => "Polygon",
        ZoneEditorTool.Rectangle => "Rectangle",
        ZoneEditorTool.Brush => "Brush",
        ZoneEditorTool.Eraser => "Eraser",
        _ => ActiveTool.ToString(),
    };

    /// <summary>Gets the current status message surfaced to the operator.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Gets the count of undo operations available.</summary>
    public int UndoCount => _appliedEntries.Count;

    /// <summary>Gets the count of redo operations available.</summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>Gets a value indicating whether a session is active.</summary>
    public bool IsSessionActive
    {
        get => _isSessionActive;
        private set
        {
            if (!SetProperty(ref _isSessionActive, value))
            {
                return;
            }

            _startSessionCommand.RaiseCanExecuteChanged();
            _commitSessionCommand.RaiseCanExecuteChanged();
            _cancelSessionCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(SessionStatusDisplay));
        }
    }

    /// <summary>Gets the command that begins a new editing session.</summary>
    public DelegateCommand StartSessionCommand => _startSessionCommand;

    /// <summary>Gets the command that commits a simulated edit.</summary>
    public DelegateCommand CommitSessionCommand => _commitSessionCommand;

    /// <summary>Gets the command that cancels the active session.</summary>
    public DelegateCommand CancelSessionCommand => _cancelSessionCommand;

    /// <summary>Gets the command that undoes the latest edit.</summary>
    public DelegateCommand UndoCommand => _undoCommand;

    /// <summary>Gets the command that replays the latest undone edit.</summary>
    public DelegateCommand RedoCommand => _redoCommand;

    /// <summary>Updates the active tool selection.</summary>
    /// <param name="tool">Tool to activate.</param>
    public void SelectTool(ZoneEditorTool tool)
    {
        ActiveTool = tool;
        StatusMessage = tool switch
        {
            ZoneEditorTool.Polygon => "Click to drop vertices and double-click to close the polygon.",
            ZoneEditorTool.Rectangle => "Drag to create management rectangles aligned to the guidance grid.",
            ZoneEditorTool.Brush => "Brush adjustments respect snapping preferences and attribute overrides.",
            ZoneEditorTool.Eraser => "Remove features or carve holes while preserving provenance logs.",
            _ => "Select a zone editing affordance.",
        };
    }

    private IReadOnlyList<ZoneEditorToolOptionViewModel> CreateTools()
    {
        return new[]
        {
            new ZoneEditorToolOptionViewModel(
                ZoneEditorTool.Polygon,
                "Polygon",
                "Draw a free-form polygon to seed a management zone.",
                "P",
                SelectTool),
            new ZoneEditorToolOptionViewModel(
                ZoneEditorTool.Rectangle,
                "Rectangle",
                "Drag rectangles aligned to field axes for quick boundaries.",
                "R",
                SelectTool),
            new ZoneEditorToolOptionViewModel(
                ZoneEditorTool.Brush,
                "Brush",
                "Brush adjustments blend geometry and attribute changes.",
                "B",
                SelectTool),
            new ZoneEditorToolOptionViewModel(
                ZoneEditorTool.Eraser,
                "Eraser",
                "Carve holes or delete existing zones.",
                "E",
                SelectTool),
        };
    }

    private void UpdateToolSelection()
    {
        foreach (var tool in _tools)
        {
            tool.IsSelected = tool.Tool == _activeTool;
        }
    }

    private void StartSession()
    {
        if (IsSessionActive)
        {
            return;
        }

        _sessionId = $"session:demo.{DateTime.UtcNow:yyyyMMddHHmmss}";
        _sequence = 0;
        _appliedEntries.Clear();
        _redoStack.Clear();
        JournalEntries.Clear();
        UpdateHistoryState();

        IsSessionActive = true;
        StatusMessage = "Editing session initialized. Select a tool to author zones.";
    }

    private void CancelSession()
    {
        if (!IsSessionActive)
        {
            return;
        }

        _sessionId = null;
        _sequence = 0;
        _appliedEntries.Clear();
        _redoStack.Clear();
        JournalEntries.Clear();
        UpdateHistoryState();

        IsSessionActive = false;
        StatusMessage = "Zone editing session cancelled. Pending edits were discarded.";
    }

    private async void CommitSession()
    {
        if (!IsSessionActive || _sessionId is null)
        {
            return;
        }

        try
        {
            _sequence++;
            var entry = await AppendSampleEntryAsync(_sequence, _sessionId);
            var viewModel = new ZoneEditorJournalEntryViewModel(_sequence, entry);
            _appliedEntries.Add(viewModel);
            JournalEntries.Add(viewModel);
            _redoStack.Clear();
            UpdateHistoryState();

            StatusMessage = $"{entry.Tool} edit committed with {entry.Operations.Count} operation(s).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to commit zone edit: {ex.Message}";
        }
    }

    private void UndoLast()
    {
        if (_appliedEntries.Count == 0)
        {
            return;
        }

        var last = _appliedEntries[^1];
        _appliedEntries.RemoveAt(_appliedEntries.Count - 1);
        JournalEntries.Remove(last);
        _redoStack.Push(last);
        UpdateHistoryState();

        StatusMessage = "Last zone edit has been undone locally.";
    }

    private void RedoLast()
    {
        if (_redoStack.Count == 0)
        {
            return;
        }

        var entry = _redoStack.Pop();
        _appliedEntries.Add(entry);
        JournalEntries.Add(entry);
        UpdateHistoryState();

        StatusMessage = "Zone edit replayed from redo history.";
    }

    private async System.Threading.Tasks.Task<LayerEditEventEntry> AppendSampleEntryAsync(int sequence, string sessionId)
    {
        var operationType = ActiveTool switch
        {
            ZoneEditorTool.Brush => "update",
            ZoneEditorTool.Eraser => "delete",
            _ => "create",
        };

        var toolName = ActiveTool switch
        {
            ZoneEditorTool.Polygon => "polygon",
            ZoneEditorTool.Rectangle => "rectangle",
            ZoneEditorTool.Brush => "brush",
            ZoneEditorTool.Eraser => "eraser",
            _ => "polygon",
        };

        var featureId = $"feature:{sessionId.Replace(':', '-')}-{sequence:00}";

        var (geometryBefore, geometryAfter) = BuildGeometry(operationType);

        var vertexEdits = ActiveTool == ZoneEditorTool.Brush
            ? new[]
            {
                new LayerEditEventVertexEdit(0, new[] { 12.4, 8.2 }, new[] { 12.8, 8.6 }),
                new LayerEditEventVertexEdit(3, new[] { 15.0, 11.7 }, new[] { 15.4, 12.2 }),
            }
            : Array.Empty<LayerEditEventVertexEdit>();

        var attributePatches = ActiveTool switch
        {
            ZoneEditorTool.Brush => new[]
            {
                new LayerEditEventAttributePatch("/attributes/rate", "replace", JsonValue.Create(180)),
            },
            ZoneEditorTool.Eraser => new[]
            {
                new LayerEditEventAttributePatch("/attributes/active", "remove", null),
            },
            _ => new[]
            {
                new LayerEditEventAttributePatch("/attributes/crop", "add", JsonValue.Create("Soybeans")),
            },
        };

        var summary = new LayerEditEventOperationSummary(
            areaDeltaSqMeters: ActiveTool == ZoneEditorTool.Eraser ? -950 : 950,
            perimeterDeltaMeters: ActiveTool == ZoneEditorTool.Brush ? 24.5 : 36.8,
            changedAttributes: attributePatches.Select(p => p.Path.TrimStart('/').Split('/')[^1]).ToArray());

        var operation = new LayerEditEventOperation(
            operationType,
            featureId,
            geometryBefore: geometryBefore,
            geometryAfter: geometryAfter,
            attributesBefore: ActiveTool == ZoneEditorTool.Brush ? JsonNode.Parse("{\"rate\":160}") : null,
            attributesAfter: ActiveTool == ZoneEditorTool.Eraser ? null : JsonNode.Parse("{\"rate\":180,\"crop\":\"Soybeans\"}"),
            vertexEdits: vertexEdits,
            attributePatches: attributePatches,
            summary: summary);

        var request = new LayerEditEventAppendRequest(
            LayerId,
            JobId,
            _context,
            new[] { operation },
            actor: "operator:demo",
            tool: toolName)
        {
            SessionId = sessionId,
            OperationGroupId = $"undoGroup:{Guid.NewGuid():N}",
            Notes = $"Sample {toolName} operation generated from the toolbar integration.",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        return await _journal.AppendAsync(request);
    }

    private static (JsonNode? before, JsonNode? after) BuildGeometry(string operationType)
    {
        JsonNode? before = null;
        JsonNode? after = null;

        const string BasePolygon = "{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[4,0],[4,3],[0,3],[0,0]]]}";
        const string ExpandedPolygon = "{\"type\":\"Polygon\",\"coordinates\":[[[0,0],[4.4,0],[4.4,3.2],[0,3.2],[0,0]]]}";

        if (operationType is "create")
        {
            after = JsonNode.Parse(BasePolygon);
        }
        else if (operationType is "update")
        {
            before = JsonNode.Parse(BasePolygon);
            after = JsonNode.Parse(ExpandedPolygon);
        }
        else if (operationType is "delete")
        {
            before = JsonNode.Parse(BasePolygon);
        }

        return (before, after);
    }

    private void UpdateHistoryState()
    {
        OnPropertyChanged(nameof(UndoCount));
        OnPropertyChanged(nameof(RedoCount));
        OnPropertyChanged(nameof(SessionStatusDisplay));
        _undoCommand.RaiseCanExecuteChanged();
        _redoCommand.RaiseCanExecuteChanged();
    }
}

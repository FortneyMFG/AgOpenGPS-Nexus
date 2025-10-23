using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Layout;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.Hosting;
using Avalonia;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// Provides a view-model facade over the global tiled shell layout.
/// </summary>
public sealed class BlockLayoutViewModel : INotifyPropertyChanged
{
    private const int MajorGridColumns = 10;
    private const int MinorDivisionsPerMajor = 2;
    private const int MinorGridColumns = MajorGridColumns * MinorDivisionsPerMajor;

    private static readonly IReadOnlyDictionary<string, string> DefaultStatusMessages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host.ToggleAutosteer"] = "Autosteer toggled.",
            ["Host.CycleAbLine"] = "AB line cycled.",
            ["Host.ToggleUTurn"] = "U-turn toggled.",
            ["Host.ToggleSections"] = "Section master toggled.",
            ["Host.StartGuidance"] = "Guidance started.",
            ["Host.PauseGuidance"] = "Guidance paused.",
            ["Host.StopGuidance"] = "Guidance stopped.",
            ["Host.NudgeLeft"] = "Guidance nudged left.",
            ["Host.NudgeRight"] = "Guidance nudged right.",
        };

    private readonly IBlockLayoutStore _layoutStore;
    private readonly IBlockCatalog _catalog;
    private readonly IShellCommandDispatcher _commandDispatcher;
    private readonly List<BlockInstance> _instances;
    private Func<BlockDefinition, bool>? _commandInterceptor;
    private Action<string> _statusReporter;
    private bool _isLocked = true;
    private Size _viewport;
    private PaneLayoutResult? _paneLayout;

    public BlockLayoutViewModel(
        IBlockLayoutStore layoutStore,
        IBlockCatalog catalog,
        IShellCommandDispatcher commandDispatcher,
        IUiPreferencesService preferencesService,
        Action<string>? statusReporter = null)
    {
        _layoutStore = layoutStore ?? throw new ArgumentNullException(nameof(layoutStore));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        ArgumentNullException.ThrowIfNull(preferencesService);
        _statusReporter = statusReporter ?? (_ => { });

        var storedInstances = _layoutStore.Load().ToList();
        _instances = storedInstances;

        var preferences = preferencesService.GetPreferences().ShellLayout ?? new ShellLayoutPreferences();
        Grid = preferences.Grid ?? new ShellGridLayout();

        Blocks = new ObservableCollection<BlockItemViewModel>();
        BuildInitialCollections();
        PaneLayout = PaneLayoutCompiler.Compile(Grid);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the observable block collection hosted on the global tiled panel.</summary>
    public ObservableCollection<BlockItemViewModel> Blocks { get; }

    /// <summary>Gets the global grid definition describing the tiled layout.</summary>
    public ShellGridLayout Grid { get; }

    /// <summary>Gets or sets a value indicating whether layout modifications are locked.</summary>
    public bool IsLocked
    {
        get => _isLocked;
        set
        {
            if (_isLocked == value)
            {
                return;
            }

            _isLocked = value;
            OnPropertyChanged();
            RefreshCommandStates();
        }
    }

    /// <summary>Gets the computed pane layout visual metadata.</summary>
    public PaneLayoutResult? PaneLayout
    {
        get => _paneLayout;
        private set
        {
            if (!ReferenceEquals(_paneLayout, value))
            {
                _paneLayout = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>Gets the last measured viewport size in device pixels.</summary>
    public Size Viewport
    {
        get => _viewport;
        private set
        {
            if (_viewport == value)
            {
                return;
            }

            _viewport = value;
            OnPropertyChanged();
        }
    }

    public void SetStatusReporter(Action<string> reporter)
    {
        _statusReporter = reporter ?? (_ => { });
    }

    public void SetCommandInterceptor(Func<BlockDefinition, bool>? interceptor)
    {
        _commandInterceptor = interceptor;
    }

    public void UpdateViewport(Size viewport)
    {
        Viewport = viewport;
        Grid.GutterPx = 0;

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            Grid.Columns = MinorGridColumns;
            Grid.Rows = Math.Max(MinorDivisionsPerMajor, Grid.Rows);
            Grid.CellPx = 0;
            PaneLayout = PaneLayoutCompiler.Compile(Grid);
            return;
        }

        var minorCell = viewport.Width / MinorGridColumns;
        Grid.CellPx = minorCell;
        Grid.Columns = MinorGridColumns;
        Grid.Rows = Math.Max(MinorDivisionsPerMajor, (int)Math.Floor(viewport.Height / minorCell));
        PaneLayout = PaneLayoutCompiler.Compile(Grid);
    }

    internal bool CanInteract(BlockItemViewModel item) => item is not null;

    internal async void Invoke(BlockItemViewModel item)
    {
        if (item is null)
        {
            return;
        }

        var definition = item.Definition;
        if (_commandInterceptor?.Invoke(definition) == true)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(definition.CommandKey))
        {
            _statusReporter(GetStatusMessage(definition));
            await DispatchCommandAsync(definition.CommandKey).ConfigureAwait(false);
            return;
        }

        if (!string.IsNullOrWhiteSpace(definition.Label))
        {
            _statusReporter($"{definition.Label} activated.");
        }
    }

    internal bool CanDelete(BlockItemViewModel item)
    {
        return item is not null && !IsLocked;
    }

    internal void Delete(BlockItemViewModel item)
    {
        if (!CanDelete(item))
        {
            return;
        }

        _instances.Remove(item.Instance);
        Blocks.Remove(item);
        Grid.Tiles.RemoveAll(tile => string.Equals(tile.Id, item.TileId, StringComparison.OrdinalIgnoreCase));
        Save();
    }

    private void BuildInitialCollections()
    {
        if (_instances.Count == 0)
        {
            return;
        }

        for (var i = 0; i < _instances.Count; i++)
        {
            var instance = _instances[i];
            var definition = _catalog.Get(instance.DefinitionId);
            if (definition is null)
            {
                continue;
            }

            var tile = EnsureTile(instance, i);
            var item = new BlockItemViewModel(instance, definition, this, tile);
            Blocks.Add(item);
        }

        TrimOrphanedTiles();
    }

    private TileSpec EnsureTile(BlockInstance instance, int index)
    {
        var tileId = instance.InstanceId.Value.ToString();
        var tile = Grid.Tiles.FirstOrDefault(t => string.Equals(t.Id, tileId, StringComparison.OrdinalIgnoreCase));
        if (tile is not null)
        {
            return tile;
        }

        var row = index % Math.Max(1, Grid.Rows);
        var column = (index / Math.Max(1, Grid.Rows)) * 2;
        tile = new TileSpec
        {
            Id = tileId,
            Row = row,
            Col = column,
        };
        Grid.Tiles.Add(tile);
        return tile;
    }

    private void TrimOrphanedTiles()
    {
        var validIds = new HashSet<string>(Blocks.Select(b => b.TileId), StringComparer.OrdinalIgnoreCase);
        Grid.Tiles.RemoveAll(tile => !validIds.Contains(tile.Id));
    }

    private void RefreshCommandStates()
    {
        foreach (var item in Blocks)
        {
            item.RefreshCommandStates();
        }
    }

    private string GetStatusMessage(BlockDefinition definition)
    {
        if (definition is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(definition.CommandKey) &&
            DefaultStatusMessages.TryGetValue(definition.CommandKey, out var message))
        {
            return message;
        }

        return string.IsNullOrWhiteSpace(definition.Label)
            ? "Command executed."
            : $"{definition.Label} activated.";
    }

    private async Task DispatchCommandAsync(string commandKey)
    {
        if (string.IsNullOrWhiteSpace(commandKey))
        {
            return;
        }

        var separatorIndex = commandKey.IndexOf('.');
        if (separatorIndex <= 0 || separatorIndex >= commandKey.Length - 1)
        {
            return;
        }

        var injectionPoint = commandKey[..separatorIndex];
        var commandId = commandKey[(separatorIndex + 1)..];
        await _commandDispatcher.DispatchAsync(injectionPoint, commandId, CancellationToken.None).ConfigureAwait(false);
    }

    private void Save()
    {
        _layoutStore.Save(_instances);
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

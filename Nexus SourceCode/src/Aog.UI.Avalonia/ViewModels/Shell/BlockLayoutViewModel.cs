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
using Aog.UI.Avalonia.ViewModels;
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
    private readonly IUiPreferencesService _preferencesService;
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
        _preferencesService = preferencesService;
        _statusReporter = statusReporter ?? (_ => { });

        var storedInstances = _layoutStore.Load().ToList();
        _instances = storedInstances;

        var preferences = preferencesService.GetPreferences().ShellLayout ?? new ShellLayoutPreferences();
        Grid = preferences.Grid ?? new ShellGridLayout();
        Grid.Tiles ??= new List<TileSpec>();
        Grid.Panels ??= new List<PanelSpec>();
        EnsureDefaultPanels();
        LeftSidebarLayout = (preferences.LeftSidebar ?? SidebarLayoutSettings.CreateVerticalDefaults()).Clone();
        RightSidebarLayout = (preferences.RightSidebar ?? SidebarLayoutSettings.CreateVerticalDefaults()).Clone();
        TopSidebarLayout = (preferences.TopSidebar ?? SidebarLayoutSettings.CreateTopDefaults()).Clone();
        BottomSidebarLayout = (preferences.BottomSidebar ?? SidebarLayoutSettings.CreateBottomDefaults()).Clone();
        WorkspaceLayout = (preferences.Workspace ?? SidebarLayoutSettings.CreateWorkspaceDefaults()).Clone();

        Blocks = new ObservableCollection<BlockItemViewModel>();
        LeftSidebarButtons = new ObservableCollection<SidebarButtonViewModel>();
        RightSidebarButtons = new ObservableCollection<SidebarButtonViewModel>();
        TopSidebarButtons = new ObservableCollection<SidebarButtonViewModel>();
        BottomSidebarButtons = new ObservableCollection<SidebarButtonViewModel>();
        BuildInitialCollections();
        SnapTilesToGrid();
        PaneLayout = PaneLayoutCompiler.Compile(Grid);
        RebuildSidebars();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the observable block collection hosted on the global tiled panel.</summary>
    public ObservableCollection<BlockItemViewModel> Blocks { get; }

    /// <summary>Gets the global grid definition describing the tiled layout.</summary>
    public ShellGridLayout Grid { get; }

    /// <summary>Gets the layout settings used to size the left sidebar.</summary>
    public SidebarLayoutSettings LeftSidebarLayout { get; }

    /// <summary>Gets the layout settings used to size the right sidebar.</summary>
    public SidebarLayoutSettings RightSidebarLayout { get; }

    /// <summary>Gets the layout settings used to size the top sidebar strip.</summary>
    public SidebarLayoutSettings TopSidebarLayout { get; }

    /// <summary>Gets the layout settings used to size the bottom sidebar strip.</summary>
    public SidebarLayoutSettings BottomSidebarLayout { get; }

    /// <summary>Gets the layout settings used to size the workspace surface.</summary>
    public SidebarLayoutSettings WorkspaceLayout { get; }

    /// <summary>Gets the collection of buttons rendered along the left sidebar.</summary>
    public ObservableCollection<SidebarButtonViewModel> LeftSidebarButtons { get; }

    /// <summary>Gets the collection of buttons rendered along the right sidebar.</summary>
    public ObservableCollection<SidebarButtonViewModel> RightSidebarButtons { get; }

    /// <summary>Gets the collection of controls rendered along the top strip.</summary>
    public ObservableCollection<SidebarButtonViewModel> TopSidebarButtons { get; }

    /// <summary>Gets the collection of controls rendered along the bottom strip.</summary>
    public ObservableCollection<SidebarButtonViewModel> BottomSidebarButtons { get; }

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
        RebuildSidebars();
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
            SnapTilesToGrid();
            EnsureDefaultPanels();
            PaneLayout = PaneLayoutCompiler.Compile(Grid);
            return;
        }

        var minorCell = viewport.Width / MinorGridColumns;
        Grid.CellPx = minorCell;
        Grid.Columns = MinorGridColumns;
        Grid.Rows = Math.Max(MinorDivisionsPerMajor, (int)Math.Floor(viewport.Height / minorCell));
        SnapTilesToGrid();
        EnsureDefaultPanels();
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
        RebuildSidebars();
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

            if (!ShouldRenderOnGrid(instance.Region))
            {
                continue;
            }

            var tile = EnsureTile(instance);
            var item = new BlockItemViewModel(instance, definition, this, tile);
            Blocks.Add(item);
        }

        TrimOrphanedTiles();
    }

    private TileSpec EnsureTile(BlockInstance instance)
    {
        var tileId = instance.InstanceId.Value.ToString();
        var tile = Grid.Tiles.FirstOrDefault(t => string.Equals(t.Id, tileId, StringComparison.OrdinalIgnoreCase));
        if (tile is not null)
        {
            return tile;
        }

        tile = new TileSpec
        {
            Id = tileId,
            Row = 0,
            Col = 0,
        };
        tile.Anchor = GetDefaultAnchor(instance.Region);
        tile.Offset = (0, 0);
        tile.PaneAttached = false;
        Grid.Tiles.Add(tile);
        return tile;
    }

    private void TrimOrphanedTiles()
    {
        var validIds = new HashSet<string>(Blocks.Select(b => b.TileId), StringComparer.OrdinalIgnoreCase);
        Grid.Tiles.RemoveAll(tile => !validIds.Contains(tile.Id));
    }

    private void SnapTilesToGrid()
    {
        if (Grid is null)
        {
            return;
        }

        var columns = Math.Max(1, Grid.Columns);
        var rows = Math.Max(1, Grid.Rows);

        foreach (var block in Blocks)
        {
            var tile = block.Tile;
            var desiredColSpan = Math.Max(1, (int)Math.Round(block.WidthUnits * MinorDivisionsPerMajor));
            var desiredRowSpan = Math.Max(1, (int)Math.Round(block.HeightUnits * MinorDivisionsPerMajor));

            if (desiredColSpan > columns)
            {
                desiredColSpan = columns;
            }

            if (desiredRowSpan > rows)
            {
                desiredRowSpan = rows;
            }

            tile.ColSpan = desiredColSpan;
            tile.RowSpan = desiredRowSpan;

            var maxColumn = Math.Max(0, columns - desiredColSpan);
            var maxRow = Math.Max(0, rows - desiredRowSpan);

            if (!tile.PaneAttached)
            {
                tile.Offset = GetDefaultOffset(block.Instance.Region, block.Instance.Order, desiredColSpan, desiredRowSpan);
                var (anchorCol, anchorRow) = ResolveAnchorPosition(tile, columns, rows, desiredColSpan, desiredRowSpan);
                tile.Col = anchorCol;
                tile.Row = anchorRow;
            }
            else
            {
                tile.Offset = (0, 0);
            }

            tile.Col = Math.Clamp(tile.Col, 0, maxColumn);
            tile.Row = Math.Clamp(tile.Row, 0, maxRow);
        }
    }

    private void RefreshCommandStates()
    {
        foreach (var item in Blocks)
        {
            item.RefreshCommandStates();
            item.RefreshSettingsState();
        }
    }

    private void RebuildSidebars()
    {
        var builder = new BlockSidebarBuilder(_catalog, _statusReporter);
        RebuildSidebarCollection(builder, LeftSidebarButtons, BlockRegion.Left);
        RebuildSidebarCollection(builder, RightSidebarButtons, BlockRegion.Right);
        RebuildSidebarCollection(builder, TopSidebarButtons, BlockRegion.Top);
        RebuildSidebarCollection(builder, BottomSidebarButtons, BlockRegion.Bottom);
    }

    private void RebuildSidebarCollection(
        BlockSidebarBuilder builder,
        ObservableCollection<SidebarButtonViewModel> target,
        BlockRegion region)
    {
        target.Clear();
        var margin = GetSidebarButtonMargin(region);
        foreach (var button in builder.Build(region, _instances))
        {
            button.Margin = margin;
            target.Add(button);
        }
    }

    private Thickness GetSidebarButtonMargin(BlockRegion region)
    {
        var spacing = region switch
        {
            BlockRegion.Left => LeftSidebarLayout.Spacing,
            BlockRegion.Right => RightSidebarLayout.Spacing,
            BlockRegion.Top => TopSidebarLayout.Spacing,
            BlockRegion.Bottom => BottomSidebarLayout.Spacing,
            _ => 0d,
        };

        var normalized = Math.Max(0d, spacing) / 2d;
        return new Thickness(normalized);
    }

    private static bool ShouldRenderOnGrid(BlockRegion region)
    {
        return region is BlockRegion.Floating
            or BlockRegion.Overlay
            or BlockRegion.LeftSub
            or BlockRegion.Left
            or BlockRegion.Right
            or BlockRegion.Top
            or BlockRegion.Bottom;
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
        var preferences = _preferencesService.GetPreferences();
        var layout = preferences.ShellLayout ?? new ShellLayoutPreferences();
        layout.Grid = Grid;
        _preferencesService.UpdateShellLayout(layout);
    }

    private void EnsureDefaultPanels()
    {
        if (Grid.Panels.Count > 0)
        {
            return;
        }

        Grid.Panels.Add(new PanelSpec
        {
            Id = "panel.map",
            Left = 1,
            Bottom = 1,
            Right = -1,
            RightUsesGridSize = true,
            TopUsesGridSize = true,
            Anchor = RelativeAnchor.BottomLeft,
        });
    }

    private static (int col, int row) ResolveAnchorPosition(
        TileSpec tile,
        int columns,
        int rows,
        int colSpan,
        int rowSpan)
    {
        var maxColumn = Math.Max(0, columns - colSpan);
        var maxRow = Math.Max(0, rows - rowSpan);

        var col = tile.Anchor switch
        {
            RelativeAnchor.TopLeft => 0,
            RelativeAnchor.TopCenter => (columns - colSpan) / 2,
            RelativeAnchor.TopRight => maxColumn,
            RelativeAnchor.MiddleLeft => 0,
            RelativeAnchor.Center => (columns - colSpan) / 2,
            RelativeAnchor.MiddleRight => maxColumn,
            RelativeAnchor.BottomLeft => 0,
            RelativeAnchor.BottomCenter => (columns - colSpan) / 2,
            RelativeAnchor.BottomRight => maxColumn,
            _ => 0,
        };

        var row = tile.Anchor switch
        {
            RelativeAnchor.TopLeft => maxRow,
            RelativeAnchor.TopCenter => maxRow,
            RelativeAnchor.TopRight => maxRow,
            RelativeAnchor.MiddleLeft => (rows - rowSpan) / 2,
            RelativeAnchor.Center => (rows - rowSpan) / 2,
            RelativeAnchor.MiddleRight => (rows - rowSpan) / 2,
            RelativeAnchor.BottomLeft => 0,
            RelativeAnchor.BottomCenter => 0,
            RelativeAnchor.BottomRight => 0,
            _ => 0,
        };

        col = Math.Clamp(col + tile.Offset.dx, 0, maxColumn);
        row = Math.Clamp(row + tile.Offset.dy, 0, maxRow);
        return (col, row);
    }

    private static RelativeAnchor GetDefaultAnchor(BlockRegion region)
    {
        return region switch
        {
            BlockRegion.Left or BlockRegion.LeftSub => RelativeAnchor.BottomLeft,
            BlockRegion.Right => RelativeAnchor.BottomRight,
            BlockRegion.Top => RelativeAnchor.TopLeft,
            BlockRegion.Bottom => RelativeAnchor.BottomLeft,
            _ => RelativeAnchor.Center,
        };
    }

    private static (int dx, int dy) GetDefaultOffset(BlockRegion region, int order, int colSpan, int rowSpan)
    {
        var normalizedOrder = Math.Max(0, order);
        return region switch
        {
            BlockRegion.Left or BlockRegion.LeftSub => (0, normalizedOrder * Math.Max(1, rowSpan)),
            BlockRegion.Right => (0, normalizedOrder * Math.Max(1, rowSpan)),
            BlockRegion.Top => (normalizedOrder * Math.Max(1, colSpan), 0),
            BlockRegion.Bottom => (normalizedOrder * Math.Max(1, colSpan), 0),
            _ => (0, 0),
        };
    }

    internal IReadOnlyList<BlockSizeOptionViewModel> CreateSizeOptions(BlockItemViewModel block)
    {
        if (block is null)
        {
            return Array.Empty<BlockSizeOptionViewModel>();
        }

        var options = new List<BlockSizeOptionViewModel>();
        foreach (var (size, label) in EnumerateCandidateSizes())
        {
            if (!SupportsSize(block.Definition, size))
            {
                continue;
            }

            options.Add(new BlockSizeOptionViewModel(block, size, label, ApplyBlockSize));
        }

        var current = block.Instance.SizeOverride;
        if (current.HasValue && options.All(option => option.Size != current.Value))
        {
            var label = current.Value.ToString();
            options.Add(new BlockSizeOptionViewModel(block, current.Value, label, ApplyBlockSize));
        }

        return options;
    }

    internal void ApplyBlockSize(BlockItemViewModel block, BlockSize size)
    {
        if (block is null)
        {
            return;
        }

        var preferred = block.Definition.PreferredSize;
        block.Instance.SizeOverride = size == preferred ? null : size;
        SnapTilesToGrid();
        block.RefreshSettingsState();
        Save();
    }

    internal void MoveBlock(BlockItemViewModel block, int column, int row)
    {
        if (block is null)
        {
            return;
        }

        var tile = block.Tile;
        var columns = Math.Max(1, Grid.Columns);
        var rows = Math.Max(1, Grid.Rows);
        var maxColumn = Math.Max(0, columns - tile.ColSpan);
        var maxRow = Math.Max(0, rows - tile.RowSpan);
        tile.Col = Math.Clamp(column, 0, maxColumn);
        tile.Row = Math.Clamp(row, 0, maxRow);
        tile.PaneAttached = true;
        Save();
    }

    private static IEnumerable<(BlockSize size, string label)> EnumerateCandidateSizes()
    {
        yield return (BlockSize.Tile1x1, "1 x 1");
        yield return (BlockSize.Tile2x1, "2 x 1");
        yield return (BlockSize.Tile1x2, "1 x 2");
        yield return (BlockSize.Tile2x2, "2 x 2");
    }

    private static bool SupportsSize(BlockDefinition definition, BlockSize size)
    {
        return size switch
        {
            BlockSize.Tile1x2 or BlockSize.Tile2x2 => definition.SupportsFullHeight,
            _ => true,
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

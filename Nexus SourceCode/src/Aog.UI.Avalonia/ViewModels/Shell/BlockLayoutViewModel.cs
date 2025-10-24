using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
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
    private readonly DelegateCommand _showLayoutSettingsCommand;
    private readonly DelegateCommand _toggleFieldDockCommand;
    private bool _isLocked = true;
    private bool _isFieldDockPinned;
    private bool _isLauncherDropIndicatorVisible;
    private int _activeLauncherDrags;
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
        _isLocked = preferences.IsLayoutLocked;
        _isFieldDockPinned = preferences.IsFieldDockPinned;
        Grid = preferences.Grid ?? new ShellGridLayout();
        Grid.Tiles ??= new List<TileSpec>();
        Grid.Panels ??= new List<PanelSpec>();
        Grid.FloatingPanels ??= new List<FloatingPanelSpec>();
        Grid.FloatingBlocks ??= new List<FloatingBlockSpec>();
        EnsureDefaultPanels();
        WorkspaceLayout = (preferences.Workspace ?? SidebarLayoutSettings.CreateWorkspaceDefaults()).Clone();

        _showLayoutSettingsCommand = new DelegateCommand(_ => RequestLayoutSettings());
        _toggleFieldDockCommand = new DelegateCommand(_ => ToggleFieldDockPinned());
        Blocks = new ObservableCollection<BlockItemViewModel>();
        FloatingPanels = new ObservableCollection<FloatingPanelViewModel>();
        FloatingBlocks = new ObservableCollection<FloatingBlockViewModel>();
        LauncherCategories = new ObservableCollection<BlockLauncherCategoryViewModel>();
        BuildInitialCollections();
        SnapTilesToGrid();
        BuildFloatingCollections();
        PaneLayout = PaneLayoutCompiler.Compile(Grid);
        RebuildLauncher();
        UpdateLauncherDropIndicator();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LayoutSettingsRequested;

    public event EventHandler<FloatingPanelViewModel>? FloatingPanelSettingsRequested;

    public event EventHandler<FloatingBlockViewModel>? FloatingBlockSettingsRequested;

    /// <summary>Gets the observable block collection hosted on the global tiled panel.</summary>
    public ObservableCollection<BlockItemViewModel> Blocks { get; }

    /// <summary>Gets the floating panel collection rendered above the tiled layout.</summary>
    public ObservableCollection<FloatingPanelViewModel> FloatingPanels { get; }

    /// <summary>Gets the floating block collection rendered independently of the grid.</summary>
    public ObservableCollection<FloatingBlockViewModel> FloatingBlocks { get; }

    /// <summary>Gets the global grid definition describing the tiled layout.</summary>
    public ShellGridLayout Grid { get; }

    /// <summary>Gets the layout settings used to size the workspace surface.</summary>
    public SidebarLayoutSettings WorkspaceLayout { get; }

    /// <summary>Gets the command that launches the layout settings dialog.</summary>
    public ICommand ShowLayoutSettingsCommand => _showLayoutSettingsCommand;

    public void RequestLayoutSettings()
    {
        LayoutSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Gets the launcher categories available within the field settings dock.</summary>
    public ObservableCollection<BlockLauncherCategoryViewModel> LauncherCategories { get; }

    /// <summary>Gets or sets a value indicating whether the field settings dock remains pinned.</summary>
    public bool IsFieldDockPinned
    {
        get => _isFieldDockPinned;
        set
        {
            if (_isFieldDockPinned == value)
            {
                return;
            }

            _isFieldDockPinned = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLauncherDockVisible));
            UpdateLauncherDropIndicator();
            Save();
        }
    }

    /// <summary>Gets a value indicating whether the field settings dock should be visible.</summary>
    public bool IsLauncherDockVisible => !_isLocked || _isFieldDockPinned;

    /// <summary>Gets a value indicating whether the drop indicator should be shown.</summary>
    public bool IsLauncherDropIndicatorVisible
    {
        get => _isLauncherDropIndicatorVisible;
        private set
        {
            if (_isLauncherDropIndicatorVisible == value)
            {
                return;
            }

            _isLauncherDropIndicatorVisible = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Gets the command that toggles the dock pin state.</summary>
    public ICommand ToggleFieldDockPinCommand => _toggleFieldDockCommand;

    private void ToggleFieldDockPinned()
    {
        IsFieldDockPinned = !IsFieldDockPinned;
    }

    internal void BeginLauncherDrag()
    {
        if (_activeLauncherDrags < int.MaxValue)
        {
            _activeLauncherDrags++;
        }

        UpdateLauncherDropIndicator();
    }

    internal void EndLauncherDrag()
    {
        if (_activeLauncherDrags > 0)
        {
            _activeLauncherDrags--;
        }

        UpdateLauncherDropIndicator();
    }

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
            OnPropertyChanged(nameof(IsLauncherDockVisible));
            OnPropertyChanged(nameof(AreFloatingOverlaysVisible));
            ApplyLockStateToFloating();
            RefreshCommandStates();
            RefreshLauncherStates();
            UpdateLauncherDropIndicator();
            Save();
        }
    }

    /// <summary>Gets a value indicating whether floating overlays should be rendered.</summary>
    public bool AreFloatingOverlaysVisible => !_isLocked;

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
        Grid.GutterPx = Math.Max(0, WorkspaceLayout.Spacing / 2d);

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

    public void ApplyWorkspaceSettings(double spacing, double tileSize)
    {
        if (IsLocked)
        {
            return;
        }

        var normalizedSpacing = double.IsFinite(spacing) ? Math.Clamp(spacing, 0d, 400d) : WorkspaceLayout.Spacing;
        var normalizedTileSize = double.IsFinite(tileSize) ? Math.Clamp(tileSize, 32d, 512d) : WorkspaceLayout.BlockSize;

        if (Math.Abs(WorkspaceLayout.Spacing - normalizedSpacing) > double.Epsilon)
        {
            WorkspaceLayout.Spacing = normalizedSpacing;
        }

        if (Math.Abs(WorkspaceLayout.BlockSize - normalizedTileSize) > double.Epsilon)
        {
            WorkspaceLayout.BlockSize = normalizedTileSize;
        }

        OnPropertyChanged(nameof(WorkspaceLayout));

        UpdateViewport(Viewport);
        UpdateCollisionStates();
        Save();
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
        var floating = FloatingBlocks.FirstOrDefault(block => block.Instance.InstanceId.Value == item.Instance.InstanceId.Value);
        if (floating is not null)
        {
            FloatingBlocks.Remove(floating);
            Grid.FloatingBlocks.RemoveAll(spec => spec.InstanceId == floating.Instance.InstanceId.Value);
        }
        Save();
        UpdateCollisionStates();
        RefreshLauncherStates();
    }

    internal bool CanLaunch(BlockLauncherItemViewModel launcher)
    {
        return launcher is not null && !IsLocked;
    }

    internal BlockItemViewModel? Launch(BlockLauncherItemViewModel launcher, int? column = null, int? row = null)
    {
        if (!CanLaunch(launcher))
        {
            return null;
        }

        var definition = launcher.Definition;
        var instance = new BlockInstance
        {
            DefinitionId = definition.Id,
            Region = definition.PreferredDock,
            Order = GetNextCloneOrder(definition.PreferredDock),
            Origin = BlockOrigin.Clone,
        };

        _instances.Add(instance);

        BlockItemViewModel? created = null;
        if (ShouldRenderOnGrid(instance.Region))
        {
            var tile = EnsureTile(instance);
            if (column.HasValue && row.HasValue)
            {
                tile.Col = column.Value;
                tile.Row = row.Value;
                tile.PaneAttached = true;
            }

            created = new BlockItemViewModel(instance, definition, this, tile);
            Blocks.Add(created);
        }

        SnapTilesToGrid();
        RefreshCommandStates();
        UpdateCollisionStates();
        Save();
        _statusReporter(GetStatusMessage(definition));
        return created;
    }

    internal (int colSpan, int rowSpan) GetGridSpan(BlockLauncherItemViewModel launcher)
    {
        if (launcher is null)
        {
            return (1, 1);
        }

        var size = launcher.Instance.SizeOverride ?? launcher.Definition.PreferredSize;
        return CalculateGridSpan(size);
    }

    private static (int colSpan, int rowSpan) CalculateGridSpan(BlockSize size)
    {
        var colSpan = Math.Max(1, (int)Math.Round(GetWidthUnits(size) * MinorDivisionsPerMajor));
        var rowSpan = Math.Max(1, (int)Math.Round(GetHeightUnits(size) * MinorDivisionsPerMajor));
        return (colSpan, rowSpan);
    }

    internal bool CanMoveLauncherItem(BlockLauncherItemViewModel launcher, int offset)
    {
        if (launcher is null || offset == 0 || IsLocked)
        {
            return false;
        }

        var entries = GetLauncherEntries(launcher.ContainerId);
        if (entries.Count <= 1)
        {
            return false;
        }

        var currentIndex = entries.FindIndex(entry => entry.instance.InstanceId.Value == launcher.Instance.InstanceId.Value);
        if (currentIndex < 0)
        {
            return false;
        }

        var targetIndex = currentIndex + offset;
        return targetIndex >= 0 && targetIndex < entries.Count;
    }

    internal void MoveLauncherItem(BlockLauncherItemViewModel launcher, int offset)
    {
        if (!CanMoveLauncherItem(launcher, offset))
        {
            return;
        }

        var entries = GetLauncherEntries(launcher.ContainerId);
        var currentIndex = entries.FindIndex(entry => entry.instance.InstanceId.Value == launcher.Instance.InstanceId.Value);
        var targetIndex = currentIndex + offset;

        var current = entries[currentIndex].instance;
        var target = entries[targetIndex].instance;
        (current.Order, target.Order) = (target.Order, current.Order);

        RebuildLauncher();
        RefreshLauncherStates();
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

    private void BuildFloatingCollections()
    {
        FloatingPanels.Clear();
        FloatingBlocks.Clear();

        if (Grid.FloatingPanels is { Count: > 0 })
        {
            foreach (var panel in Grid.FloatingPanels)
            {
                if (panel is null)
                {
                    continue;
                }

                var panelViewModel = new FloatingPanelViewModel(panel, this);
                FloatingPanels.Add(panelViewModel);
            }
        }

        if (_instances.Count == 0)
        {
            return;
        }

        foreach (var instance in _instances)
        {
            if (instance is null)
            {
                continue;
            }

            if (instance.Region is not (BlockRegion.Floating or BlockRegion.Overlay))
            {
                continue;
            }

            var definition = _catalog.Get(instance.DefinitionId);
            if (definition is null)
            {
                continue;
            }

            var spec = EnsureFloatingBlockSpec(instance, definition);
            var blockViewModel = new FloatingBlockViewModel(instance, definition, spec, this);
            FloatingBlocks.Add(blockViewModel);
        }

        TrimOrphanedFloatingSpecs();
        ApplyLockStateToFloating();
        UpdateCollisionStates();
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

    private FloatingBlockSpec EnsureFloatingBlockSpec(BlockInstance instance, BlockDefinition definition)
    {
        var specs = Grid.FloatingBlocks;
        var spec = specs.FirstOrDefault(s => s.InstanceId == instance.InstanceId.Value);
        if (spec is null)
        {
            var (width, height) = CalculateFloatingBlockSize(definition);
            var (x, y) = GetDefaultFloatingPosition(instance.Region, instance.Order, width, height);
            spec = new FloatingBlockSpec
            {
                InstanceId = instance.InstanceId.Value,
                Width = width,
                Height = height,
                X = x,
                Y = y,
            };
            specs.Add(spec);
        }
        else
        {
            if (spec.Width <= 0 || spec.Height <= 0)
            {
                var (width, height) = CalculateFloatingBlockSize(definition);
                spec.Width = width;
                spec.Height = height;
            }

            if (Math.Abs(spec.X) <= double.Epsilon && Math.Abs(spec.Y) <= double.Epsilon)
            {
                var (x, y) = GetDefaultFloatingPosition(instance.Region, instance.Order, spec.Width, spec.Height);
                spec.X = x;
                spec.Y = y;
            }
        }

        return spec;
    }

    private void TrimOrphanedTiles()
    {
        var validIds = new HashSet<string>(Blocks.Select(b => b.TileId), StringComparer.OrdinalIgnoreCase);
        Grid.Tiles.RemoveAll(tile => !validIds.Contains(tile.Id));
    }

    private void TrimOrphanedFloatingSpecs()
    {
        var validIds = new HashSet<Guid>(FloatingBlocks.Select(b => b.Instance.InstanceId.Value));
        Grid.FloatingBlocks.RemoveAll(block => !validIds.Contains(block.InstanceId));
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

        ApplyLockStateToFloating();
        RefreshLauncherStates();
    }

    private void RefreshLauncherStates()
    {
        foreach (var category in LauncherCategories)
        {
            category.RefreshStates();
        }
    }

    private void RebuildLauncher()
    {
        LauncherCategories.Clear();

        var canonical = _instances
            .Select(instance => (instance, definition: _catalog.Get(instance.DefinitionId)))
            .Where(pair => pair.definition is not null)
            .Where(pair => pair.instance.Origin == BlockOrigin.Canonical)
            .Where(pair => pair.definition!.Placement == PlacementPolicy.MenuScoped)
            .ToList();

        var categories = new Dictionary<string, BlockLauncherCategoryViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var container in canonical
                     .Where(pair => pair.definition!.Kind is BlockKind.Container or BlockKind.Menu)
                     .OrderBy(pair => pair.instance.Order))
        {
            var key = ResolveContainerKey(container.instance, container.definition!);
            var title = string.IsNullOrWhiteSpace(container.definition!.Label)
                ? key
                : container.definition!.Label;
            var category = new BlockLauncherCategoryViewModel(key, title, container.instance, this);
            categories[key] = category;
            LauncherCategories.Add(category);
        }

        foreach (var group in canonical
                     .Where(pair => pair.definition!.Kind is not (BlockKind.Container or BlockKind.Menu))
                     .GroupBy(pair => ResolveContainerKey(pair.instance, pair.definition!), StringComparer.OrdinalIgnoreCase))
        {
            if (!categories.TryGetValue(group.Key, out var category))
            {
                var first = group.First();
                var title = string.IsNullOrWhiteSpace(first.definition!.Label)
                    ? group.Key
                    : first.definition!.Label;
                category = new BlockLauncherCategoryViewModel(group.Key, title, first.instance, this);
                categories[group.Key] = category;
                LauncherCategories.Add(category);
            }

            foreach (var item in group.OrderBy(pair => pair.instance.Order))
            {
                category.Blocks.Add(new BlockLauncherItemViewModel(item.instance, item.definition!, group.Key, this));
            }
        }

        RefreshLauncherStates();
        UpdateLauncherDropIndicator();
    }

    private List<(BlockInstance instance, BlockDefinition definition)> GetLauncherEntries(string containerId)
    {
        if (string.IsNullOrWhiteSpace(containerId))
        {
            return new List<(BlockInstance, BlockDefinition)>();
        }

        return _instances
            .Select(instance => (instance, definition: _catalog.Get(instance.DefinitionId)))
            .Where(pair => pair.definition is not null)
            .Where(pair => pair.instance.Origin == BlockOrigin.Canonical)
            .Where(pair => pair.definition!.Placement == PlacementPolicy.MenuScoped)
            .Where(pair => pair.definition!.Kind is not (BlockKind.Container or BlockKind.Menu))
            .Where(pair => string.Equals(
                ResolveContainerKey(pair.instance, pair.definition!),
                containerId,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.instance.Order)
            .Select(pair => (pair.instance, pair.definition!))
            .ToList();
    }

    private static string ResolveContainerKey(BlockInstance instance, BlockDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(instance.ContainerId))
        {
            return instance.ContainerId!;
        }

        if (!string.IsNullOrWhiteSpace(definition.ContainerId))
        {
            return definition.ContainerId!;
        }

        return definition.Id.Value;
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

    private void ApplyLockStateToFloating()
    {
        foreach (var panel in FloatingPanels)
        {
            panel.SetLockState(_isLocked);
        }

        foreach (var block in FloatingBlocks)
        {
            block.SetLockState(_isLocked);
        }
    }

    private void UpdateCollisionStates()
    {
        for (var i = 0; i < FloatingPanels.Count; i++)
        {
            var panel = FloatingPanels[i];
            var colliding = false;

            for (var j = 0; j < FloatingPanels.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if (FloatingPanels[j].Bounds.Intersects(panel.Bounds))
                {
                    colliding = true;
                    break;
                }
            }

            if (!colliding)
            {
                colliding = FloatingBlocks.Any(block => block.Bounds.Intersects(panel.Bounds));
            }

            panel.SetCollision(colliding);
        }

        for (var i = 0; i < FloatingBlocks.Count; i++)
        {
            var block = FloatingBlocks[i];
            var colliding = false;

            for (var j = 0; j < FloatingBlocks.Count; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if (FloatingBlocks[j].Bounds.Intersects(block.Bounds))
                {
                    colliding = true;
                    break;
                }
            }

            if (!colliding)
            {
                colliding = FloatingPanels.Any(panel => panel.Bounds.Intersects(block.Bounds));
            }

            block.SetCollision(colliding);
        }
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
        layout.IsLayoutLocked = _isLocked;
        layout.IsFieldDockPinned = _isFieldDockPinned;
        layout.Grid = Grid;
        layout.Grid.FloatingBlocks ??= new List<FloatingBlockSpec>();
        layout.Grid.FloatingPanels ??= new List<FloatingPanelSpec>();
        layout.Workspace = WorkspaceLayout.Clone();
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

    private static (double x, double y) GetDefaultFloatingPosition(BlockRegion region, int order, double width, double height)
    {
        var index = Math.Max(0, order);
        var column = index % 3;
        var row = index / 3;
        var spacing = 32d;
        var tileWidth = Math.Max(width, 160d);
        var tileHeight = Math.Max(height, 128d);
        var baseX = 48d + column * (tileWidth + spacing);
        var baseY = 48d + row * (tileHeight + spacing);

        return region switch
        {
            BlockRegion.Overlay => (baseX, baseY),
            _ => (baseX, baseY),
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

    public void UpdateFloatingBlock(FloatingBlockViewModel block, Rect bounds)
    {
        if (block is null)
        {
            return;
        }

        var normalized = ClampFloatingBounds(bounds, 96, 96);
        block.UpdateBounds(normalized);
        UpdateCollisionStates();
        Save();
    }

    public void UpdateFloatingPanel(FloatingPanelViewModel panel, Rect bounds)
    {
        if (panel is null)
        {
            return;
        }

        var normalized = ClampFloatingBounds(bounds, 160, 160);
        panel.UpdateBounds(normalized);
        UpdateCollisionStates();
        Save();
    }

    public void SetFloatingPanelLock(FloatingPanelViewModel panel, bool isLocked)
    {
        if (panel is null)
        {
            return;
        }

        panel.UpdatePanelLock(isLocked);
        Save();
    }

    internal void RequestFloatingPanelSettings(FloatingPanelViewModel panel)
    {
        if (panel is null)
        {
            return;
        }

        FloatingPanelSettingsRequested?.Invoke(this, panel);
    }

    internal void RequestFloatingBlockSettings(FloatingBlockViewModel block)
    {
        if (block is null)
        {
            return;
        }

        FloatingBlockSettingsRequested?.Invoke(this, block);
    }

    private int GetNextCloneOrder(BlockRegion region)
    {
        return _instances
            .Where(instance => instance is not null && instance.Region == region && instance.Origin == BlockOrigin.Clone)
            .Select(instance => instance.Order)
            .DefaultIfEmpty(-1)
            .Max() + 1;
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

    private (double width, double height) CalculateFloatingBlockSize(BlockDefinition definition)
    {
        var baseTile = WorkspaceLayout.BlockSize <= 0 ? 112d : WorkspaceLayout.BlockSize;
        var cell = Grid.CellPx <= 0 ? baseTile : Grid.CellPx;
        var widthUnits = GetWidthUnits(definition.PreferredSize);
        var heightUnits = GetHeightUnits(definition.PreferredSize);
        var width = Math.Max(cell, widthUnits * cell);
        var height = Math.Max(cell, heightUnits * cell);
        return (width, height);
    }

    private static double GetWidthUnits(BlockSize size)
    {
        return size switch
        {
            BlockSize.Tile2x1 or BlockSize.Tile2x2 or BlockSize.Tile2xHalf => 2d,
            BlockSize.TileHalfx1 or BlockSize.TileHalfx2 => 0.5d,
            _ => 1d,
        };
    }

    private static double GetHeightUnits(BlockSize size)
    {
        return size switch
        {
            BlockSize.Tile1x2 or BlockSize.Tile2x2 => 2d,
            BlockSize.Tile1xHalf or BlockSize.Tile2xHalf => 0.5d,
            BlockSize.TileHalfx2 => 2d,
            _ => 1d,
        };
    }

    public void ApplyFloatingPanelSettings(
        FloatingPanelViewModel panel,
        string? title,
        string? contentId,
        bool isPanelLocked,
        double width,
        double height)
    {
        if (panel is null)
        {
            return;
        }

        var sanitizedTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        var sanitizedContent = string.IsNullOrWhiteSpace(contentId) ? null : contentId.Trim();
        panel.ApplySettings(sanitizedTitle, sanitizedContent);
        panel.UpdatePanelLock(isPanelLocked);

        var current = panel.Bounds;
        var resized = new Rect(current.Position, new Size(Math.Max(0, width), Math.Max(0, height)));
        var normalized = ClampFloatingBounds(resized, 160, 160);
        panel.UpdateBounds(normalized);
        UpdateCollisionStates();
        Save();
    }

    public void ApplyFloatingBlockSettings(
        FloatingBlockViewModel block,
        double width,
        double height)
    {
        if (block is null)
        {
            return;
        }

        var current = block.Bounds;
        var resized = new Rect(current.Position, new Size(Math.Max(0, width), Math.Max(0, height)));
        var normalized = ClampFloatingBounds(resized, 96, 96);
        block.UpdateBounds(normalized);
        UpdateCollisionStates();
        Save();
    }

    internal Rect ClampFloatingBounds(Rect bounds, double minWidth, double minHeight)
    {
        var viewport = Viewport;
        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            var widthFallback = Math.Max(minWidth, bounds.Width);
            var heightFallback = Math.Max(minHeight, bounds.Height);
            return new Rect(bounds.Position, new Size(widthFallback, heightFallback));
        }

        var width = Math.Max(minWidth, Math.Min(bounds.Width, viewport.Width));
        var height = Math.Max(minHeight, Math.Min(bounds.Height, viewport.Height));

        var maxX = Math.Max(0, viewport.Width - width);
        var maxY = Math.Max(0, viewport.Height - height);
        var x = Math.Clamp(bounds.X, 0, maxX);
        var y = Math.Clamp(bounds.Y, 0, maxY);
        return new Rect(x, y, width, height);
    }

    private void UpdateLauncherDropIndicator()
    {
        IsLauncherDropIndicatorVisible = !_isLocked && _activeLauncherDrags > 0;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

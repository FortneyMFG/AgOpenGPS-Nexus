using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.ViewModels.Shell;

/// <summary>
/// Provides a view-model facade over persisted block layout instances, supporting reordering and command dispatch.
/// </summary>
public sealed class BlockLayoutViewModel : INotifyPropertyChanged
{
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
    private Action<string> _statusReporter;
    private readonly Dictionary<BlockRegion, ObservableCollection<BlockItemViewModel>> _regions;
    private readonly Dictionary<BlockInstanceId, BlockItemViewModel> _itemLookup;
    private readonly List<BlockInstance> _instances;
    private Func<BlockDefinition, bool>? _commandInterceptor;

    private bool _isLocked = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public BlockLayoutViewModel(
        IBlockLayoutStore layoutStore,
        IBlockCatalog catalog,
        IShellCommandDispatcher commandDispatcher,
        Action<string>? statusReporter = null)
    {
        _layoutStore = layoutStore ?? throw new ArgumentNullException(nameof(layoutStore));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
        _statusReporter = statusReporter ?? (_ => { });

        _regions = new Dictionary<BlockRegion, ObservableCollection<BlockItemViewModel>>
        {
            [BlockRegion.Left] = new ObservableCollection<BlockItemViewModel>(),
            [BlockRegion.Right] = new ObservableCollection<BlockItemViewModel>(),
            [BlockRegion.Bottom] = new ObservableCollection<BlockItemViewModel>(),
            [BlockRegion.Top] = new ObservableCollection<BlockItemViewModel>(),
        };
        _itemLookup = new Dictionary<BlockInstanceId, BlockItemViewModel>();
        _instances = _layoutStore.Load().ToList();

        BuildInitialCollections();
    }

    /// <summary>Gets the blocks rendered along the left side-bar.</summary>
    public ObservableCollection<BlockItemViewModel> LeftBlocks => _regions[BlockRegion.Left];

    /// <summary>Gets the blocks rendered along the right side-bar.</summary>
    public ObservableCollection<BlockItemViewModel> RightBlocks => _regions[BlockRegion.Right];

    /// <summary>Gets the blocks rendered along the bottom strip.</summary>
    public ObservableCollection<BlockItemViewModel> BottomBlocks => _regions[BlockRegion.Bottom];

    /// <summary>Gets the blocks rendered in the top telemetry strip.</summary>
    public ObservableCollection<BlockItemViewModel> TopBlocks => _regions[BlockRegion.Top];

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

    /// <summary>Assigns the callback used to surface user-facing status text.</summary>
    public void SetStatusReporter(Action<string> reporter)
    {
        _statusReporter = reporter ?? (_ => { });
    }

    /// <summary>Assigns a callback that can handle block activations before they are dispatched.</summary>
    public void SetCommandInterceptor(Func<BlockDefinition, bool>? interceptor)
    {
        _commandInterceptor = interceptor;
    }

    internal bool CanInteract(BlockItemViewModel item) => item is not null;

    internal bool CanDrag(BlockItemViewModel item) =>
        item is not null && !IsLocked;

    internal bool CanDrop(BlockItemViewModel item, BlockRegion targetRegion)
    {
        if (item is null || IsLocked)
        {
            return false;
        }

        return item.Instance.Region == targetRegion
            ? CanReorder(item)
            : CanMoveTo(item, targetRegion);
    }

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

    internal bool CanReorder(BlockItemViewModel item)
    {
        if (item is null)
        {
            return false;
        }

        if (IsLocked)
        {
            return false;
        }

        return GetRegionCollection(item.Instance.Region).Count > 1;
    }

    internal void MoveWithinRegionByOffset(BlockItemViewModel item, int delta)
    {
        if (item is null || delta == 0 || IsLocked)
        {
            return;
        }

        var region = item.Instance.Region;
        var collection = GetRegionCollection(region);
        var index = collection.IndexOf(item);
        var targetIndex = Math.Clamp(index + delta, 0, collection.Count - 1);
        if (targetIndex == index)
        {
            return;
        }

        MoveWithinRegion(item, targetIndex);
    }

    internal bool MoveWithinRegion(BlockItemViewModel item, int newIndex)
    {
        if (item is null || IsLocked)
        {
            return false;
        }

        var region = item.Instance.Region;
        var collection = GetRegionCollection(region);
        var currentIndex = collection.IndexOf(item);
        if (currentIndex < 0)
        {
            return false;
        }

        newIndex = Math.Clamp(newIndex, 0, collection.Count);
        if (currentIndex == newIndex)
        {
            return false;
        }

        collection.RemoveAt(currentIndex);
        if (currentIndex < newIndex)
        {
            newIndex--;
        }
        if (newIndex > collection.Count)
        {
            newIndex = collection.Count;
        }
        if (newIndex < 0)
        {
            newIndex = 0;
        }

        collection.Insert(newIndex, item);
        ReindexRegion(region);
        Persist();
        RefreshCommandStates();
        return true;
    }

    internal bool CanMoveTo(BlockItemViewModel item, BlockRegion region)
    {
        if (item is null)
        {
            return false;
        }

        if (IsLocked)
        {
            return false;
        }

        if (item.Instance.Region == region)
        {
            return false;
        }

        if (item.IsCanonical)
        {
            // Keep canonical instances anchored to their home regions.
            return false;
        }

        return _regions.ContainsKey(region);
    }

    internal void MoveToRegion(BlockItemViewModel item, BlockRegion targetRegion)
    {
        if (!CanMoveTo(item, targetRegion))
        {
            return;
        }

        var targetCollection = GetRegionCollection(targetRegion);
        MoveToRegion(item, targetRegion, targetCollection.Count);
    }

    internal bool MoveToRegion(BlockItemViewModel item, BlockRegion targetRegion, int targetIndex)
    {
        if (!CanMoveTo(item, targetRegion))
        {
            return false;
        }

        var sourceRegion = item.Instance.Region;
        var sourceCollection = GetRegionCollection(sourceRegion);
        var targetCollection = GetRegionCollection(targetRegion);

        if (!sourceCollection.Remove(item))
        {
            return false;
        }

        targetIndex = Math.Clamp(targetIndex, 0, targetCollection.Count);

        item.Instance.Region = targetRegion;

        if (targetIndex >= targetCollection.Count)
        {
            targetCollection.Add(item);
        }
        else
        {
            targetCollection.Insert(targetIndex, item);
        }

        ReindexRegion(sourceRegion);
        ReindexRegion(targetRegion);
        Persist();
        RefreshCommandStates();
        return true;
    }

    internal bool HandleDrop(BlockItemViewModel item, BlockRegion targetRegion, int targetIndex)
    {
        if (!CanDrop(item, targetRegion))
        {
            return false;
        }

        if (item.Instance.Region == targetRegion)
        {
            return MoveWithinRegion(item, targetIndex);
        }

        return MoveToRegion(item, targetRegion, targetIndex);
    }

    internal bool CanDelete(BlockItemViewModel item)
    {
        if (item is null)
        {
            return false;
        }

        if (IsLocked)
        {
            return false;
        }

        return item.IsClone;
    }

    internal void Delete(BlockItemViewModel item)
    {
        if (!CanDelete(item))
        {
            return;
        }

        var region = item.Instance.Region;
        var collection = GetRegionCollection(region);
        if (!collection.Remove(item))
        {
            return;
        }

        _itemLookup.Remove(item.Instance.InstanceId);
        _instances.RemoveAll(instance => instance.InstanceId.Value == item.Instance.InstanceId.Value);
        ReindexRegion(region);
        Persist();
        RefreshCommandStates();
    }

    private void BuildInitialCollections()
    {
        foreach (var pair in _regions)
        {
            pair.Value.Clear();
        }

        _itemLookup.Clear();

        foreach (var instance in _instances.OrderBy(i => i.Order))
        {
            if (!_regions.ContainsKey(instance.Region))
            {
                continue;
            }

            if (_catalog.Get(instance.DefinitionId) is not BlockDefinition definition)
            {
                continue;
            }

            var viewModel = new BlockItemViewModel(instance, definition, this);
            _itemLookup[instance.InstanceId] = viewModel;
            _regions[instance.Region].Add(viewModel);
        }

        RefreshCommandStates();
    }

    private ObservableCollection<BlockItemViewModel> GetRegionCollection(BlockRegion region)
    {
        if (!_regions.TryGetValue(region, out var collection))
        {
            throw new InvalidOperationException($"Region '{region}' is not supported by the current shell host.");
        }

        return collection;
    }

    private static string GetStatusMessage(BlockDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.CommandKey)
            && DefaultStatusMessages.TryGetValue(definition.CommandKey, out var message))
        {
            return message;
        }

        return string.IsNullOrWhiteSpace(definition.Label)
            ? "Command executed."
            : $"{definition.Label} executed.";
    }

    private async Task DispatchCommandAsync(string commandKey)
    {
        try
        {
            await _commandDispatcher.DispatchAsync("shell.blocks", commandKey).ConfigureAwait(false);
        }
        catch
        {
            // Ignore dispatch failures for now – logging occurs inside the dispatcher.
        }
    }

    private void Persist()
    {
        foreach (var instance in _instances)
        {
            if (_regions.TryGetValue(instance.Region, out var collection))
            {
                var index = collection.FindIndex(vm => vm.Instance.InstanceId.Value == instance.InstanceId.Value);
                if (index >= 0)
                {
                    instance.Order = index;
                }
            }
        }

        _layoutStore.Save(_instances);
    }

    private void ReindexRegion(BlockRegion region)
    {
        if (!_regions.TryGetValue(region, out var collection))
        {
            return;
        }

        for (var i = 0; i < collection.Count; i++)
        {
            collection[i].Instance.Order = i;
        }
    }

    private void RefreshCommandStates()
    {
        foreach (var item in _itemLookup.Values)
        {
            item.RefreshCommandStates();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal static class ObservableCollectionExtensions
{
    public static int FindIndex<T>(this ObservableCollection<T> source, Func<T, bool> predicate)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        for (var i = 0; i < source.Count; i++)
        {
            if (predicate(source[i]))
            {
                return i;
            }
        }

        return -1;
    }
}


using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Aog.UI.Avalonia.Blocks;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.ViewModels.Shell;

public sealed class BlockSizeOptionViewModel : INotifyPropertyChanged
{
    private readonly BlockItemViewModel _block;
    private readonly Action<BlockItemViewModel, BlockSize> _apply;
    private readonly DelegateCommand _applyCommand;

    public BlockSizeOptionViewModel(BlockItemViewModel block, BlockSize size, string label, Action<BlockItemViewModel, BlockSize> apply)
    {
        _block = block ?? throw new ArgumentNullException(nameof(block));
        Size = size;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        _applyCommand = new DelegateCommand(_ => _apply(_block, Size));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public BlockSize Size { get; }

    public string Label { get; }

    public ICommand ApplyCommand => _applyCommand;

    public bool IsSelected
    {
        get
        {
            var instance = _block.Instance;
            var current = instance.SizeOverride ?? _block.Definition.PreferredSize;
            return current == Size;
        }
    }

    internal void Refresh()
    {
        OnPropertyChanged(nameof(IsSelected));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System;
using System.Threading;
using System.Windows.Input;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a button or toggle within the top toolbar.
/// </summary>
public sealed class TopToolbarItemViewModel : ObservableObject
{
    private readonly IShellCommandDispatcher _dispatcher;
    private readonly string _injectionPoint;
    private readonly string _commandId;
    private readonly bool _isToggle;
    private readonly bool _togglesDispatchOn;
    private bool _isChecked;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopToolbarItemViewModel"/> class.
    /// </summary>
    public TopToolbarItemViewModel(
        string id,
        string displayName,
        string injectionPoint,
        string commandId,
        IShellCommandDispatcher dispatcher,
        bool isToggle,
        bool initialState,
        string? description = null,
        bool togglesDispatchOn = true)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        _injectionPoint = injectionPoint ?? throw new ArgumentNullException(nameof(injectionPoint));
        _commandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _isToggle = isToggle;
        _togglesDispatchOn = togglesDispatchOn;
        _isChecked = initialState;
        Description = description;
        Command = new DelegateCommand(_ => Execute());
    }

    /// <summary>Gets the unique identifier for the toolbar item.</summary>
    public string Id { get; }

    /// <summary>Gets the display label shown in the toolbar.</summary>
    public string DisplayName { get; }

    /// <summary>Gets an optional descriptive tooltip.</summary>
    public string? Description { get; }

    /// <summary>Gets the tooltip text displayed for the toolbar item.</summary>
    public string Tooltip => Description ?? string.Empty;

    /// <summary>Gets the command invoked when the toolbar item is activated.</summary>
    public ICommand Command { get; }

    /// <summary>Gets a value indicating whether the item behaves as a toggle.</summary>
    public bool IsToggle => _isToggle;

    /// <summary>Gets or sets a value indicating whether the toggle is active.</summary>
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (SetProperty(ref _isChecked, value) && _isToggle && !_togglesDispatchOn)
            {
                _ = _dispatcher.DispatchAsync(_injectionPoint, _commandId, CancellationToken.None);
            }
        }
    }

    private async void Execute()
    {
        if (_isToggle && _togglesDispatchOn)
        {
            IsChecked = !IsChecked;
        }

        await _dispatcher.DispatchAsync(_injectionPoint, _commandId, CancellationToken.None).ConfigureAwait(false);
    }
}

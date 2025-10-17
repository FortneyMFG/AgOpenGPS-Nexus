using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using Aog.UI.Avalonia.Hosting;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Represents a single menu entry in the shell menu bar.
/// </summary>
public sealed class ShellMenuItemViewModel : ObservableObject
{
    private readonly IShellCommandDispatcher? _dispatcher;
    private readonly string? _injectionPoint;
    private readonly string? _commandId;
    private readonly ReadOnlyObservableCollection<ShellMenuItemViewModel> _children;
    private bool _lastInvocationHandled;
    private DateTimeOffset? _lastInvocationTimestamp;

    private ShellMenuItemViewModel(string header, IEnumerable<ShellMenuItemViewModel> children)
    {
        Header = header;
        var childList = new ObservableCollection<ShellMenuItemViewModel>(children.ToList());
        _children = new ReadOnlyObservableCollection<ShellMenuItemViewModel>(childList);
        Command = null;
    }

    private ShellMenuItemViewModel(
        string header,
        string injectionPoint,
        string commandId,
        IShellCommandDispatcher dispatcher,
        bool isEnabled)
    {
        Header = header;
        _injectionPoint = injectionPoint;
        _commandId = commandId;
        _dispatcher = dispatcher;
        IsEnabled = isEnabled;
        _children = new ReadOnlyObservableCollection<ShellMenuItemViewModel>(new ObservableCollection<ShellMenuItemViewModel>());
        Command = new DelegateCommand(_ => ExecuteCommand(), _ => IsEnabled);
    }

    /// <summary>Gets the display header for the menu item.</summary>
    public string Header { get; }

    /// <summary>Gets a tooltip or descriptive text for the command.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the tooltip text including last invocation metadata when available.</summary>
    public string Tooltip
    {
        get
        {
            var baseDescription = string.IsNullOrWhiteSpace(Description) ? string.Empty : Description.Trim();

            if (LastInvocationTimestamp is not { } timestamp)
            {
                return baseDescription;
            }

            var localTimestamp = timestamp.ToLocalTime();
            var formattedTimestamp = localTimestamp.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
            var status = LastInvocationHandled ? "handled" : "failed";

            if (baseDescription.Length == 0)
            {
                return FormattableString.Invariant($"Last invoked {formattedTimestamp} ({status}).");
            }

            return FormattableString.Invariant($"{baseDescription}\nLast invoked {formattedTimestamp} ({status}).");
        }
    }

    /// <summary>Gets a shortcut text to display next to the command.</summary>
    public string? GestureText { get; init; }

    /// <summary>Gets the command bound to the menu item. May be <c>null</c> for container entries.</summary>
    public ICommand? Command { get; }

    /// <summary>Gets a value indicating whether the command is currently enabled.</summary>
    public bool IsEnabled { get; }

    /// <summary>Gets the menu item's children.</summary>
    public ReadOnlyObservableCollection<ShellMenuItemViewModel> Children => _children;

    /// <summary>Gets a value indicating whether the menu item has children.</summary>
    public bool HasChildren => _children.Count > 0;

    /// <summary>Gets a value indicating whether the last invocation was handled by a dispatcher.</summary>
    public bool LastInvocationHandled
    {
        get => _lastInvocationHandled;
        private set
        {
            if (SetProperty(ref _lastInvocationHandled, value))
            {
                OnPropertyChanged(nameof(Tooltip));
            }
        }
    }

    /// <summary>Gets the timestamp of the last invocation if any.</summary>
    public DateTimeOffset? LastInvocationTimestamp
    {
        get => _lastInvocationTimestamp;
        private set
        {
            if (SetProperty(ref _lastInvocationTimestamp, value))
            {
                OnPropertyChanged(nameof(Tooltip));
            }
        }
    }

    /// <summary>
    /// Creates a menu container entry.
    /// </summary>
    public static ShellMenuItemViewModel CreateContainer(string header, params ShellMenuItemViewModel[] children)
    {
        return new ShellMenuItemViewModel(header, children);
    }

    /// <summary>
    /// Creates a command entry.
    /// </summary>
    public static ShellMenuItemViewModel CreateCommand(
        string header,
        string injectionPoint,
        string commandId,
        IShellCommandDispatcher dispatcher,
        bool isEnabled = true,
        string? description = null,
        string? gestureText = null)
    {
        var item = new ShellMenuItemViewModel(header, injectionPoint, commandId, dispatcher, isEnabled)
        {
            Description = description,
            GestureText = gestureText,
        };
        return item;
    }

    private async void ExecuteCommand()
    {
        if (_dispatcher is null || _injectionPoint is null || _commandId is null)
        {
            return;
        }

        try
        {
            var handled = await _dispatcher.DispatchAsync(_injectionPoint, _commandId, CancellationToken.None).ConfigureAwait(false);
            LastInvocationHandled = handled;
            LastInvocationTimestamp = DateTimeOffset.UtcNow;
        }
        catch
        {
            LastInvocationHandled = false;
            LastInvocationTimestamp = DateTimeOffset.UtcNow;
            throw;
        }
    }
}

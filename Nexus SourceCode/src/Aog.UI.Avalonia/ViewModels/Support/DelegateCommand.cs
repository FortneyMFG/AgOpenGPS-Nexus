using System;
using System.Windows.Input;

namespace Aog.UI.Avalonia.ViewModels.Support;

/// <summary>
/// A minimal <see cref="ICommand"/> implementation used by design-time view-models.
/// </summary>
public sealed class DelegateCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
    /// </summary>
    /// <param name="execute">The delegate invoked when the command executes.</param>
    /// <param name="canExecute">Optional predicate that controls whether the command can execute.</param>
    public DelegateCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        _execute(parameter);
    }

    /// <summary>
    /// Notifies listeners that the execution status has changed.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

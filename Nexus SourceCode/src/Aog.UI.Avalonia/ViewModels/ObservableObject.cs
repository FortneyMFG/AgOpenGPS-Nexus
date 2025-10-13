using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Minimal helper base class for view-models requiring property change notifications.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises a property changed notification for the specified property name.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            return;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets the backing field to the provided value if it differs from the current value.
    /// </summary>
    /// <typeparam name="T">Type of the backing field.</typeparam>
    /// <param name="field">Backing field reference.</param>
    /// <param name="value">Value to assign.</param>
    /// <param name="propertyName">Name of the property being updated.</param>
    /// <returns><c>true</c> when the field changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

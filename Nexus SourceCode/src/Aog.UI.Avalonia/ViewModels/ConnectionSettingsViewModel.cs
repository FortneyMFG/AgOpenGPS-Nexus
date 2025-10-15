using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model backing the connection settings panel in the shell.
/// </summary>
public class ConnectionSettingsViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IConnectionSettingsStore _store;
    private readonly AsyncCommand _saveCommand;
    private readonly IAvaloniaRunModeService _runModeService;

    private ConnectionSettings _persistedSettings;
    private string _agioEndpoint = string.Empty;
    private AgioBackendKind _selectedBackend;
    private GpsSourcePolicy _selectedGpsSourcePolicy;
    private AvaloniaRunMode _selectedRunMode;
    private string? _statusMessage;
    private bool _hasError;
    private bool _isSaving;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionSettingsViewModel"/> class.
    /// </summary>
    /// <param name="store">The persistence store used to load and save settings.</param>
    public ConnectionSettingsViewModel(IConnectionSettingsStore store, IAvaloniaRunModeService runModeService)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(runModeService);
        _store = store;
        _runModeService = runModeService;

        _persistedSettings = _store.Load();
        _agioEndpoint = _persistedSettings.AgioEndpoint;
        _selectedBackend = _persistedSettings.Backend;
        _selectedGpsSourcePolicy = _persistedSettings.GpsSourcePolicy;
        _selectedRunMode = _runModeService.CurrentMode;

        _saveCommand = new AsyncCommand(SaveAsync, CanSave);
        _runModeService.ModeChanged += OnRunModeChanged;
    }

    /// <summary>
    /// Raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the list of available AGiO backends.
    /// </summary>
    public IReadOnlyList<AgioBackendKind> AvailableBackends { get; } = Enum.GetValues<AgioBackendKind>();

    /// <summary>
    /// Gets the list of available GPS source policies.
    /// </summary>
    public IReadOnlyList<GpsSourcePolicy> AvailableGpsSourcePolicies { get; } = Enum.GetValues<GpsSourcePolicy>();

    /// <summary>
    /// Gets the list of supported run modes.
    /// </summary>
    public IReadOnlyList<AvaloniaRunMode> AvailableRunModes => _runModeService.SupportedModes;

    /// <summary>
    /// Gets or sets the AGiO endpoint the UI should connect to.
    /// </summary>
    public string AgioEndpoint
    {
        get => _agioEndpoint;
        set
        {
            if (value == _agioEndpoint)
            {
                return;
            }

            _agioEndpoint = value;
            OnPropertyChanged();
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets the selected AGiO backend.
    /// </summary>
    public AgioBackendKind SelectedBackend
    {
        get => _selectedBackend;
        set
        {
            if (value == _selectedBackend)
            {
                return;
            }

            _selectedBackend = value;
            OnPropertyChanged();
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets the selected GPS source policy.
    /// </summary>
    public GpsSourcePolicy SelectedGpsSourcePolicy
    {
        get => _selectedGpsSourcePolicy;
        set
        {
            if (value == _selectedGpsSourcePolicy)
            {
                return;
            }

            _selectedGpsSourcePolicy = value;
            OnPropertyChanged();
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets the selected run mode for the UI shell.
    /// </summary>
    public AvaloniaRunMode SelectedRunMode
    {
        get => _selectedRunMode;
        set
        {
            if (value == _selectedRunMode)
            {
                return;
            }

            _selectedRunMode = value;
            OnPropertyChanged();
            _ = ApplyRunModeAsync(value);
        }
    }

    /// <summary>
    /// Gets a value indicating whether the view-model is currently saving.
    /// </summary>
    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            if (value == _isSaving)
            {
                return;
            }

            _isSaving = value;
            OnPropertyChanged();
            NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets a message describing the outcome of the last save attempt.
    /// </summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (value == _statusMessage)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current status represents an error.
    /// </summary>
    public bool HasError
    {
        get => _hasError;
        private set
        {
            if (value == _hasError)
            {
                return;
            }

            _hasError = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the command used to persist the connection settings.
    /// </summary>
    public ICommand SaveCommand => _saveCommand;

    private bool CanSave()
    {
        if (IsSaving)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(AgioEndpoint))
        {
            return false;
        }

        return _persistedSettings.AgioEndpoint != AgioEndpoint
            || _persistedSettings.Backend != SelectedBackend
            || _persistedSettings.GpsSourcePolicy != SelectedGpsSourcePolicy;
    }

    private async Task SaveAsync()
    {
        try
        {
            IsSaving = true;
            HasError = false;
            StatusMessage = "Saving settings...";

            var current = new ConnectionSettings
            {
                AgioEndpoint = AgioEndpoint.Trim(),
                Backend = SelectedBackend,
                GpsSourcePolicy = SelectedGpsSourcePolicy,
            };

            await Task.Run(() => _store.Save(current)).ConfigureAwait(true);

            _persistedSettings = current.Clone();
            _agioEndpoint = _persistedSettings.AgioEndpoint;
            OnPropertyChanged(nameof(AgioEndpoint));
            NotifyCanExecuteChanged();
            StatusMessage = $"Settings saved at {DateTime.Now:T}.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task ApplyRunModeAsync(AvaloniaRunMode mode)
    {
        try
        {
            var result = await _runModeService.SetModeAsync(mode).ConfigureAwait(true);
            HasError = false;
            StatusMessage = result.RequiresRestart
                ? $"Run mode set to {mode}. Restart required to finalise transport wiring."
                : $"Run mode set to {mode}.";
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusMessage = $"Failed to apply run mode: {ex.Message}";
        }
    }

    private void OnRunModeChanged(object? sender, AvaloniaRunModeChangedEventArgs e)
    {
        _selectedRunMode = e.Mode;
        OnPropertyChanged(nameof(SelectedRunMode));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _runModeService.ModeChanged -= OnRunModeChanged;
    }

    private void NotifyCanExecuteChanged() => _saveCommand.NotifyCanExecuteChanged();

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed class AsyncCommand : ICommand
    {
        private readonly Func<Task> _executeAsync;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            _isExecuting = true;
            NotifyCanExecuteChanged();

            try
            {
                await _executeAsync().ConfigureAwait(true);
            }
            finally
            {
                _isExecuting = false;
                NotifyCanExecuteChanged();
            }
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

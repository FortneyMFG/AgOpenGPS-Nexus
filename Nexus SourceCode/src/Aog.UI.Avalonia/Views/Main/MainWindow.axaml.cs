using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels;
using Aog.UI.Avalonia.ViewModels.Shell;
using Aog.UI.Avalonia.Views.Shell;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Aog.UI.Avalonia.Views.Main
{
    /// <summary>
    /// Main application window.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly IUiPreferencesService? _preferencesService;
        private Size _lastNormalSize;
        private PixelPoint? _lastNormalPosition;
        private IDisposable? _clientSizeSubscription;
        private IDisposable? _windowStateSubscription;
        private AppShellViewModel? _shell;

        // DI-friendly default ctor
        public MainWindow()
            : this(
                AvaloniaServiceProviderAccessor.GetRequiredService<MainWindowViewModel>(),
                AvaloniaServiceProviderAccessor.GetRequiredService<IUiPreferencesService>())
        {
        }

        // Primary ctor: initialize and wire everything up
        public MainWindow(MainWindowViewModel viewModel, IUiPreferencesService preferencesService)
        {
            ArgumentNullException.ThrowIfNull(viewModel);
            ArgumentNullException.ThrowIfNull(preferencesService);

            InitializeComponent();

            _preferencesService = preferencesService;

            var preferences = _preferencesService.GetPreferences();
            ApplyPlacement(preferences.Window);

            DataContext = viewModel;
            AttachShell(viewModel.Shell);

            _clientSizeSubscription = this.GetObservable(ClientSizeProperty).Subscribe(size =>
            {
                if (WindowState == WindowState.Normal)
                {
                    _lastNormalSize = size;
                }
            });

            PositionChanged += OnPositionChanged;

            _windowStateSubscription = this.GetObservable(WindowStateProperty).Subscribe(state =>
            {
                if (state == WindowState.Normal)
                {
                    _lastNormalSize = ClientSize;
                    _lastNormalPosition = Position;
                }
            });

            Closing += OnClosing;
            Closed += OnClosed;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ApplyPlacement(WindowPlacement placement)
        {
            if (placement.Width > 0)
            {
                Width = placement.Width;
            }

            if (placement.Height > 0)
            {
                Height = placement.Height;
            }

            _lastNormalSize = new Size(Width, Height);

            if (placement.X.HasValue && placement.Y.HasValue)
            {
                Position = new PixelPoint(placement.X.Value, placement.Y.Value);
                WindowStartupLocation = WindowStartupLocation.Manual;
                _lastNormalPosition = new PixelPoint(placement.X.Value, placement.Y.Value);
            }

            WindowState = placement.WindowState;
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (_preferencesService is null)
            {
                return;
            }

            var placement = new WindowPlacement
            {
                WindowState = WindowState,
                Width = _lastNormalSize.Width > 0 ? _lastNormalSize.Width : ClientSize.Width,
                Height = _lastNormalSize.Height > 0 ? _lastNormalSize.Height : ClientSize.Height,
            };

            var position = _lastNormalPosition ?? Position;
            placement.X = position.X;
            placement.Y = position.Y;

            _preferencesService.UpdateWindowPlacement(placement);
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _clientSizeSubscription?.Dispose();
            _windowStateSubscription?.Dispose();
            Closing -= OnClosing;
            Closed -= OnClosed;
            PositionChanged -= OnPositionChanged;

            DetachShell();

            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }

            DataContext = null;
        }

        private void OnPositionChanged(object? sender, PixelPointEventArgs e)
        {
            if (WindowState == WindowState.Normal)
            {
                _lastNormalPosition = e.Point;
            }
        }

        private void AttachShell(AppShellViewModel shell)
        {
            ArgumentNullException.ThrowIfNull(shell);
            _shell = shell;
            _shell.LayoutSettingsRequested += OnLayoutSettingsRequested;
            _shell.FloatingPanelSettingsRequested += OnFloatingPanelSettingsRequested;
            _shell.FloatingBlockSettingsRequested += OnFloatingBlockSettingsRequested;
            _shell.BlockSettingsRequested += OnBlockSettingsRequested;
        }

        private void DetachShell()
        {
            if (_shell is null)
            {
                return;
            }

            _shell.LayoutSettingsRequested -= OnLayoutSettingsRequested;
            _shell.FloatingPanelSettingsRequested -= OnFloatingPanelSettingsRequested;
            _shell.FloatingBlockSettingsRequested -= OnFloatingBlockSettingsRequested;
            _shell.BlockSettingsRequested -= OnBlockSettingsRequested;
            _shell = null;
        }

        private async void OnLayoutSettingsRequested(object? sender, EventArgs e)
        {
            if (_shell is null)
            {
                return;
            }

            if (_shell.Layout.IsLocked)
            {
                return;
            }

            var dialog = new LayoutSettingsWindow
            {
                DataContext = new LayoutSettingsDialogViewModel(_shell.Layout),
                Icon = Icon,
            };

            await dialog.ShowDialog<bool?>(this);
        }

        private async void OnFloatingPanelSettingsRequested(object? sender, FloatingPanelViewModel panel)
        {
            if (_shell is null || panel is null)
            {
                return;
            }

            var layout = _shell.Layout;
            if (layout.IsLocked || panel.IsLocked)
            {
                return;
            }

            var dialog = new FloatingPanelSettingsWindow
            {
                DataContext = new FloatingPanelSettingsDialogViewModel(layout, panel),
                Icon = Icon,
            };

            await dialog.ShowDialog<bool?>(this);
        }

        private async void OnFloatingBlockSettingsRequested(object? sender, FloatingBlockViewModel block)
        {
            if (_shell is null || block is null)
            {
                return;
            }

            var layout = _shell.Layout;
            if (layout.IsLocked || block.IsLocked)
            {
                return;
            }

            var dialog = new FloatingBlockSettingsWindow
            {
                DataContext = new FloatingBlockSettingsDialogViewModel(layout, block),
                Icon = Icon,
            };

            await dialog.ShowDialog<bool?>(this);
        }

        private async void OnBlockSettingsRequested(object? sender, BlockItemViewModel block)
        {
            if (_shell is null || block is null)
            {
                return;
            }

            var layout = _shell.Layout;
            if (layout.IsLocked)
            {
                return;
            }

            var dialog = new BlockSettingsWindow
            {
                DataContext = new BlockSettingsDialogViewModel(layout, block),
                Icon = Icon,
            };

            await dialog.ShowDialog<bool?>(this);
        }
    }
}

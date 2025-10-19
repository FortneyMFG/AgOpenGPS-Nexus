using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels;
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
    }
}

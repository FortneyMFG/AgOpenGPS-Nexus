using System;
using System.Reactive;
using Aog.UI.Avalonia.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Aog.UI.Avalonia.ViewModels.Shell
{
    public class AppShellViewModel : ReactiveObject, IDisposable
    {
        public PluginSurfaceDescriptor SurfaceDescriptor => ShellPluginSurfaces.AppShell;

        [Reactive]
        public string StatusText { get; set; } = "Ready";

        public ReactiveCommand<Unit, Unit> CenterViewCommand { get; }
        public ReactiveCommand<Unit, Unit> PanToolCommand { get; }
        public ReactiveCommand<Unit, Unit> MeasureToolCommand { get; }
        public ReactiveCommand<Unit, Unit> ToggleLayoutLockCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenLayoutSettingsCommand { get; }
        [Reactive]
        public object? CurrentView { get; set; }
        [Reactive]
        public StatusStripViewModel StatusStrip { get; set; }
        [Reactive]
        public MainWindowViewModel? Host { get; set; }
        [Reactive]
        public object? MainContent { get; set; }
        public BlockLayoutViewModel Layout { get; }

        public event EventHandler? LayoutSettingsRequested;

        public event EventHandler<FloatingPanelViewModel>? FloatingPanelSettingsRequested;

        public event EventHandler<FloatingBlockViewModel>? FloatingBlockSettingsRequested;

        public AppShellViewModel(BlockLayoutViewModel layout)
        {
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Layout.SetStatusReporter(UpdateStatus);
            Layout.LayoutSettingsRequested += OnLayoutSettingsRequested;
            Layout.FloatingPanelSettingsRequested += OnFloatingPanelSettingsRequested;
            Layout.FloatingBlockSettingsRequested += OnFloatingBlockSettingsRequested;

            CenterViewCommand = ReactiveCommand.Create(() => { /* TODO: Implement center view */ });
            PanToolCommand = ReactiveCommand.Create(() => { /* TODO: Implement pan tool */ });
            MeasureToolCommand = ReactiveCommand.Create(() => { /* TODO: Implement measure tool */ });
            ToggleLayoutLockCommand = ReactiveCommand.Create(() =>
            {
                Layout.IsLocked = !Layout.IsLocked;
            });
            OpenLayoutSettingsCommand = ReactiveCommand.Create(() =>
            {
                Layout.RequestLayoutSettings();
            });
            StatusStrip = new StatusStripViewModel(Array.Empty<ShellStatusIndicatorViewModel>());
            CurrentView = "Select a tool or open a field operation to begin.";
        }

        private void UpdateStatus(string message)
        {
            StatusText = string.IsNullOrWhiteSpace(message) ? "Command executed." : message;
        }

        private void OnLayoutSettingsRequested(object? sender, EventArgs e)
        {
            LayoutSettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnFloatingPanelSettingsRequested(object? sender, FloatingPanelViewModel panel)
        {
            if (panel is null)
            {
                return;
            }

            FloatingPanelSettingsRequested?.Invoke(this, panel);
        }

        private void OnFloatingBlockSettingsRequested(object? sender, FloatingBlockViewModel block)
        {
            if (block is null)
            {
                return;
            }

            FloatingBlockSettingsRequested?.Invoke(this, block);
        }

        public void Dispose()
        {
            Layout.LayoutSettingsRequested -= OnLayoutSettingsRequested;
            Layout.FloatingPanelSettingsRequested -= OnFloatingPanelSettingsRequested;
            Layout.FloatingBlockSettingsRequested -= OnFloatingBlockSettingsRequested;
        }
    }
}

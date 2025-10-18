using System;
using System.Reactive;
using Aog.UI.Avalonia.ViewModels;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Aog.UI.Avalonia.ViewModels.Shell
{
    public class AppShellViewModel : ReactiveObject
    {
        [Reactive]
        public string StatusText { get; set; } = "Ready";

        public ReactiveCommand<Unit, Unit> CenterViewCommand { get; }
        public ReactiveCommand<Unit, Unit> PanToolCommand { get; }
        public ReactiveCommand<Unit, Unit> MeasureToolCommand { get; }
        [Reactive]
        public object? CurrentView { get; set; }
        [Reactive]
        public StatusStripViewModel StatusStrip { get; set; }
        [Reactive]
        public MainWindowViewModel? Host { get; set; }
        [Reactive]
        public object? MainContent { get; set; }

        public AppShellViewModel()
        {
            CenterViewCommand = ReactiveCommand.Create(() => { /* TODO: Implement center view */ });
            PanToolCommand = ReactiveCommand.Create(() => { /* TODO: Implement pan tool */ });
            MeasureToolCommand = ReactiveCommand.Create(() => { /* TODO: Implement measure tool */ });
            StatusStrip = new StatusStripViewModel(Array.Empty<ShellStatusIndicatorViewModel>());
            CurrentView = "Select a tool or open a field operation to begin.";
        }
    }
}

using System.Reactive;
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
        public object? CurrentView { get; private set; }

        public AppShellViewModel()
        {
            CenterViewCommand = ReactiveCommand.Create(() => { /* TODO: Implement center view */ });
            PanToolCommand = ReactiveCommand.Create(() => { /* TODO: Implement pan tool */ });
            MeasureToolCommand = ReactiveCommand.Create(() => { /* TODO: Implement measure tool */ });
        }
    }
}
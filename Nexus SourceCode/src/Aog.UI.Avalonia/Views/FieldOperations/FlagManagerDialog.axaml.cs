using Avalonia.Controls;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia.Views.FieldOperations
{
    public partial class FlagManagerDialog : Window
    {
        // Default: resolve or synthesize a VM, then delegate.
        public FlagManagerDialog()
            : this(CreateDefaultViewModel())
        {
        }

        // Main ctor: initialize and bind the VM.
        public FlagManagerDialog(FlagManagerDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private static FlagManagerDialogViewModel CreateDefaultViewModel()
        {
            if (AvaloniaServiceProviderAccessor.TryGetServiceProvider(out var services))
            {
                var host = services.GetService<MainWindowViewModel>();
                if (host is not null)
                {
                    return host.CreateFlagManagerDialogViewModel();
                }
            }

            // Fallback for design-time or missing DI.
            return FlagManagerDialogViewModel.CreateSample();
        }
    }
}

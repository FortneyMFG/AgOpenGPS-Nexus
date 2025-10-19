using Avalonia.Controls;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia.Views.FieldOperations
{
    public partial class FlagManagerDialog : Window
    {
        public FlagManagerDialog()
            : this(CreateDefaultViewModel())
        {
        }

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

            return FlagManagerDialogViewModel.CreateSample();
        }
    }
}

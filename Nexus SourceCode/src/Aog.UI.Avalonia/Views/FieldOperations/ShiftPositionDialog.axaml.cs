using Avalonia.Controls;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia.Views.FieldOperations
{
    public partial class ShiftPositionDialog : Window
    {
        public ShiftPositionDialog()
            : this(CreateDefaultViewModel())
        {
        }

        public ShiftPositionDialog(ShiftPositionDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private static ShiftPositionDialogViewModel CreateDefaultViewModel()
        {
            if (AvaloniaServiceProviderAccessor.TryGetServiceProvider(out var services))
            {
                var host = services.GetService<MainWindowViewModel>();
                if (host is not null)
                {
                    return host.CreateShiftPositionDialogViewModel();
                }
            }

            return ShiftPositionDialogViewModel.CreateSample();
        }
    }
}

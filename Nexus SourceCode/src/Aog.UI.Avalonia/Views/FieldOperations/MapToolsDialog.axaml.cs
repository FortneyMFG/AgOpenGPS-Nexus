using Avalonia.Controls;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia.Views.FieldOperations;

public partial class MapToolsDialog : Window
{
    public MapToolsDialog()
        : this(CreateDefaultViewModel())
    {
    }

    public MapToolsDialog(MapToolsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private static MapToolsDialogViewModel CreateDefaultViewModel()
    {
        if (AvaloniaServiceProviderAccessor.TryGetServiceProvider(out var services))
        {
            var host = services.GetService<IFieldOperationsDialogHost>() ??
                       services.GetService<MainWindowViewModel>();
            if (host is IFieldOperationsDialogHost coordinator)
            {
                return new MapToolsDialogViewModel(coordinator, () => { });
            }
        }

        return MapToolsDialogViewModel.CreateDesignSample();
    }
}

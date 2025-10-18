using Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.FieldOperations;

public partial class MapToolsDialog : Window
{
    public MapToolsDialog(MapToolsDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}

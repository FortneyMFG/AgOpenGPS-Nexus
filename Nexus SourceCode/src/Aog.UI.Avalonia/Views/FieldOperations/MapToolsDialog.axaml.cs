using Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.FieldOperations;

public partial class MapToolsDialog : Window
{
    public MapToolsDialog()
    {
        InitializeComponent();
    }

    public MapToolsDialog(MapToolsDialogViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}

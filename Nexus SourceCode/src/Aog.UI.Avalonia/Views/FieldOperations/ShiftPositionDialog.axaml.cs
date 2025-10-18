using Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.FieldOperations
{
    public partial class ShiftPositionDialog : Window
    {
        public ShiftPositionDialog(ShiftPositionDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
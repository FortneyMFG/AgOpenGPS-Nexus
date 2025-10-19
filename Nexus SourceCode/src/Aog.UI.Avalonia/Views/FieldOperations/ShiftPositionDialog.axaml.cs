using Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.FieldOperations
{
    public partial class ShiftPositionDialog : Window
    {
        public ShiftPositionDialog()
        {
            InitializeComponent();
        }

        public ShiftPositionDialog(ShiftPositionDialogViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }
    }
}

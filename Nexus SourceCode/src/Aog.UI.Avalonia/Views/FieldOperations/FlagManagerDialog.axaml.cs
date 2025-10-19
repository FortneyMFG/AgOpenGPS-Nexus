using Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.FieldOperations
{
    public partial class FlagManagerDialog : Window
    {
        public FlagManagerDialog()
        {
            InitializeComponent();
        }

        public FlagManagerDialog(FlagManagerDialogViewModel viewModel)
            : this()
        {
            DataContext = viewModel;
        }
    }
}

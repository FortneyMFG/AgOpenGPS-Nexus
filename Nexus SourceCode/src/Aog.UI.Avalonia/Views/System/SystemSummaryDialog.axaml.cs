using Avalonia.Controls;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.System;

public partial class SystemSummaryDialog : Window
{
    public SystemSummaryDialog()
    {
        InitializeComponent();
    }

    public SystemSummaryDialog(MainWindowViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}

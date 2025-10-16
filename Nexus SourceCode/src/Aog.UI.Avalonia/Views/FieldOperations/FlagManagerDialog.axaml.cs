using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.FieldOperations;

public partial class FlagManagerDialog : Window
{
    public FlagManagerDialog(FlagManagerDialogViewModel viewModel)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

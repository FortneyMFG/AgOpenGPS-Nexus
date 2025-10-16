using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class FieldMenuView : MenuItem
{
    public FieldMenuView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

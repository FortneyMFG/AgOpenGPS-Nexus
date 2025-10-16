using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class ToolsMenuView : MenuItem
{
    public ToolsMenuView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

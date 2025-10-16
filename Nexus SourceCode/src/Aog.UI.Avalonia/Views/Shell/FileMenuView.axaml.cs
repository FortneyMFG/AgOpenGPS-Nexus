using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class FileMenuView : MenuItem
{
    public FileMenuView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

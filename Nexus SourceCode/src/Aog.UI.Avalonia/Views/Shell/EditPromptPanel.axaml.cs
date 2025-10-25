using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Aog.UI.Avalonia.Views.Shell;

public partial class EditPromptPanel : UserControl
{
    public EditPromptPanel()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

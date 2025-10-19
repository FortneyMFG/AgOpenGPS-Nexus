using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MappingPlugin.Views;

public partial class MapSurfaceView : UserControl
{
    public MapSurfaceView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

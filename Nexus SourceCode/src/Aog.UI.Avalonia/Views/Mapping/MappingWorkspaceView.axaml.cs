using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Views.Mapping;

public partial class MappingWorkspaceView : UserControl
{
    private const double ForwardStepMeters = 0.75;
    private const double TurnStepDegrees = 4.5;

    public MappingWorkspaceView()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        AttachedToVisualTree += (_, _) => Focus();
        PointerPressed += (_, _) => Focus();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MappingWorkspaceViewModel workspace)
        {
            return;
        }

        var boost = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 2.0 : 1.0;
        switch (e.Key)
        {
            case Key.W:
                workspace.ApplyMovement(ForwardStepMeters * boost, 0);
                e.Handled = true;
                break;
            case Key.S:
                workspace.ApplyMovement(-ForwardStepMeters * boost, 0);
                e.Handled = true;
                break;
            case Key.A:
                workspace.ApplyMovement(0, -TurnStepDegrees * boost);
                e.Handled = true;
                break;
            case Key.D:
                workspace.ApplyMovement(0, TurnStepDegrees * boost);
                e.Handled = true;
                break;
            case Key.R:
                workspace.ResetPose();
                e.Handled = true;
                break;
        }
    }
}

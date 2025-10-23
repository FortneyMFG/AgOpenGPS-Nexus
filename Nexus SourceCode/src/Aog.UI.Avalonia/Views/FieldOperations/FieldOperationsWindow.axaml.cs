using System.Numerics;
using Aog.UI.Avalonia.Mapping.Core;
using Aog.UI.Avalonia.Mapping.HUD;
using Aog.UI.Avalonia.Mapping.Input;
using Aog.UI.Avalonia.Mapping.Rendering;
using Aog.UI.Avalonia.Mapping.Vector;
using Aog.UI.Avalonia.Mapping.Vector.Styles;
using Avalonia.Controls;

namespace Aog.UI.Avalonia.Views.FieldOperations
{
    public partial class FieldOperationsWindow : Window
    {
        public FieldOperationsWindow()
        {
            InitializeComponent();
            InitializeMap();
        }

        private void InitializeMap()
        {
            if (this.FindControl<MapView>("MapViewHost") is not { } mapView)
            {
                return;
            }

            var scene = new MapScene();
            var camera = new CameraRig();
            var localizer = new Localizer();
            mapView.Attach(scene, camera, localizer);

            mapView.AddLayer(new GridPassLayer
            {
                ZIndex = 10
            });

            var demoBoundary = new[]
            {
                new Double3(-50, -50, 0),
                new Double3(50, -50, 0),
                new Double3(50, 50, 0),
                new Double3(-50, 50, 0),
                new Double3(-50, -50, 0)
            };

            var boundarySource = new EnuFeatureSource(new[] { demoBoundary });
            mapView.AddLayer(new VectorLayer(
                boundarySource,
                fill: new FillSymbolizer(0.18f, new Vector4(0.92f, 0.82f, 0.12f, 0.9f)),
                line: new LineSymbolizer(1.2f, 1f, new Vector4(0.92f, 0.82f, 0.12f, 1f)))
            {
                ZIndex = 200
            });

            mapView.AddLayer(new DebugHudLayer
            {
                ZIndex = 1000
            });
        }
    }
}

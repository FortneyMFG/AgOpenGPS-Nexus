using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Aog.UI.Avalonia.Hosting;
using Aog.UI.Avalonia.Models;
using Aog.UI.Avalonia.Settings;
using Aog.UI.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Aog.UI.Avalonia.Views.System;

public partial class SystemSummaryDialog : Window
{
    public SystemSummaryDialog()
        : this(ResolveViewModel())
    {
    }

    public SystemSummaryDialog(ISystemSummaryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private static ISystemSummaryViewModel ResolveViewModel()
    {
        if (AvaloniaServiceProviderAccessor.TryGetServiceProvider(out var services))
        {
            var summary = services.GetService<ISystemSummaryViewModel>() ??
                          services.GetService<MainWindowViewModel>() as ISystemSummaryViewModel;
            if (summary is not null)
            {
                return summary;
            }
        }

        return DesignSystemSummaryViewModel.Instance;
    }

    private sealed class DesignSystemSummaryViewModel : ISystemSummaryViewModel
    {
        public static ISystemSummaryViewModel Instance { get; } = new DesignSystemSummaryViewModel();

        private DesignSystemSummaryViewModel()
        {
            PlatformDescription = "AgOpenGPS Nexus (design preview)";
            MapLayers = CreateSampleLayers();
            GuidanceTracks = CreateSampleTracks();
            AvailableThemes = Enum.GetValues<UiTheme>();
            SelectedTheme = UiTheme.Light;
        }

        public string PlatformDescription { get; }

        public IReadOnlyList<MapLayer> MapLayers { get; }

        public IReadOnlyList<GuidanceTrack> GuidanceTracks { get; }

        public IReadOnlyList<UiTheme> AvailableThemes { get; }

        public UiTheme SelectedTheme { get; set; }

        private static IReadOnlyList<MapLayer> CreateSampleLayers()
        {
            var coverageStyle = new LayerVisualizationStyle(Colors.LightSkyBlue, Colors.SteelBlue, 0, 1, "% Coverage");
            var plannedStyle = new LayerVisualizationStyle(Colors.LightGray, Colors.DimGray, 0, 1, "% Planned", isPlanned: true);

            var coverageCells = new[]
            {
                new MapLayerCell(new Point(0, 0), 24, 0.72),
                new MapLayerCell(new Point(24, 8), 24, 0.94),
                new MapLayerCell(new Point(-16, -18), 24, 0.58),
            };

            var plannedCells = new[]
            {
                new MapLayerCell(new Point(0, 0), 24, 0.9),
                new MapLayerCell(new Point(24, 8), 24, 0.9),
                new MapLayerCell(new Point(-16, -18), 24, 0.9),
            };

            return new[]
            {
                new MapLayer("coverage.actual", "Coverage (Actual)", coverageStyle, coverageCells),
                new MapLayer("coverage.planned", "Coverage (Planned)", plannedStyle, plannedCells),
            };
        }

        private static IReadOnlyList<GuidanceTrack> CreateSampleTracks()
        {
            return new[]
            {
                new GuidanceTrack(
                    "AB Line North",
                    new[]
                    {
                        new Point(-40, -40),
                        new Point(40, 40),
                    },
                    Colors.Orange,
                    2),
                new GuidanceTrack(
                    "Headland Loop",
                    new[]
                    {
                        new Point(-60, -30),
                        new Point(-60, 30),
                        new Point(60, 30),
                        new Point(60, -30),
                        new Point(-60, -30),
                    },
                    Colors.MediumSeaGreen,
                    1.5),
            };
        }
    }
}

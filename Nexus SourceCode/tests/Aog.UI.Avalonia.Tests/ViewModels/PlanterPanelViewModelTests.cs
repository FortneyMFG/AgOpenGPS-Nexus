using System.Linq;
using Aog.Core.V1;
using Aog.UI.Avalonia.ViewModels;
using FluentAssertions;
using Xunit;

namespace Aog.UI.Avalonia.Tests.ViewModels;

public sealed class PlanterPanelViewModelTests
{
    [Fact]
    public void ApplyRowStatuses_RemovesMissingRowsAndUpdatesSummary()
    {
        var panel = new PlanterPanelViewModel();

        panel.ApplyRowStatuses(new[]
        {
            new PlanterRowStatus
            {
                RowIndex = 0,
                TargetPopulationPerMeter = 10,
                ActualPopulationPerMeter = 10,
                SkipRate = 0,
                DoubleRate = 0,
                Quality = PlanterRowQuality.Ok
            },
            new PlanterRowStatus
            {
                RowIndex = 1,
                TargetPopulationPerMeter = 10,
                ActualPopulationPerMeter = 9,
                SkipRate = 0.1,
                DoubleRate = 0,
                Quality = PlanterRowQuality.Skip
            }
        });

        panel.Rows.Select(row => row.RowIndex).Should().Equal(0, 1);
        panel.Summary.Should().Contain("Rows: 2");

        panel.ApplyRowStatuses(new[]
        {
            new PlanterRowStatus
            {
                RowIndex = 1,
                TargetPopulationPerMeter = 12,
                ActualPopulationPerMeter = 12.5,
                SkipRate = 0,
                DoubleRate = 0.02,
                Quality = PlanterRowQuality.Double
            }
        });

        panel.Rows.Should().HaveCount(1);
        panel.Rows.Single().RowIndex.Should().Be(1);
        panel.Summary.Should().Be("Rows: 1 • OK 0 • Skips 0 • Doubles 1 • Unknown 0");
    }
}

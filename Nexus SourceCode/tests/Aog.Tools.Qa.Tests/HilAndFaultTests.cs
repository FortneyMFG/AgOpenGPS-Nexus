using System;
using System.IO;
using System.Linq;
using Aog.Tools.Qa.Faults;
using Aog.Tools.Qa.Hil;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.Qa.Tests;

public static class HilAndFaultTests
{
    [Fact]
    public static void Hil_runner_evaluates_sample_metrics()
    {
        var configPath = Resolve("hil/config.json");
        var runner = new HilRigRunner();
        var result = runner.Run(configPath);

        result.RigName.Should().Be("BenchRig-01");
        result.Passed.Should().BeTrue();
        result.Scenarios.Should().HaveCount(2);
        result.Scenarios.Should().OnlyContain(s => s.Assertions.All(a => a.Passed));
    }

    [Fact]
    public static void Fault_runner_generates_schedule_with_jitter()
    {
        var scenarioPath = Resolve("faults/power-network.json");
        var runner = new FaultInjectionRunner();
        var schedule = runner.BuildSchedule(scenarioPath);

        schedule.ScenarioName.Should().Be("Power and network");
        schedule.Events.Should().HaveCount(3);
        schedule.Events.Should().BeInAscendingOrder(e => e.StartOffset);

        schedule.Events[0].Type.Should().Be("imu_noise");
        schedule.Events[0].StartOffset.TotalSeconds.Should().BeInRange(25, 35);

        schedule.Events[1].Type.Should().Be("power_cycle");
        schedule.Events[1].StartOffset.TotalSeconds.Should().BeInRange(40, 50);
        schedule.Events[1].Severity.Should().BeApproximately(1.0, 1e-6);

        schedule.Events[2].Type.Should().Be("network_drop");
        schedule.Events[2].StartOffset.TotalSeconds.Should().BeInRange(85, 100);
        schedule.Events[2].Severity.Should().BeApproximately(0.6, 1e-6);
    }

    private static string Resolve(string relativePath)
    {
        return Path.Combine(AppContext.BaseDirectory, "Data", relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}

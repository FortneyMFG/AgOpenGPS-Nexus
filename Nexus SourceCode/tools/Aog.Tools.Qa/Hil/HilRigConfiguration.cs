using System;
using System.Collections.Generic;

namespace Aog.Tools.Qa.Hil;

public sealed class HilRigConfiguration
{
    public string RigName { get; set; } = string.Empty;
    public string ControllerEndpoint { get; set; } = string.Empty;
    public IReadOnlyList<string> StreamBindings { get; set; } = Array.Empty<string>();
    public IReadOnlyList<HilScenario> Scenarios { get; set; } = Array.Empty<HilScenario>();
}

public sealed class HilScenario
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }

    public string SampleData { get; set; } = string.Empty;
    public IReadOnlyList<HilAssertion> Assertions { get; set; } = Array.Empty<HilAssertion>();
}

public sealed class HilAssertion
{
    public string Metric { get; set; } = string.Empty;
    public double? Min { get; set; }

    public double? Max { get; set; }

    public string? Unit { get; set; }

}

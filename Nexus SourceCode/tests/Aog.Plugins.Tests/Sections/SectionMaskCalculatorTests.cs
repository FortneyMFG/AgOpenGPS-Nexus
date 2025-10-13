using System;
using Aog.Plugins.Sections;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.Sections;

public sealed class SectionMaskCalculatorTests
{
    [Fact]
    public void ComputeMask_WithCoverage_ReturnsExpectedMask()
    {
        var calculator = new SectionMaskCalculator(sectionCount: 4, minimumSpeedMps: 0.1, lookAheadSeconds: 1.0);
        var sections = new[]
        {
            new SectionObservation(hasCoverage: true, distanceToCoverageStartMeters: null),
            new SectionObservation(hasCoverage: false, distanceToCoverageStartMeters: null),
            new SectionObservation(hasCoverage: true, distanceToCoverageStartMeters: null),
            new SectionObservation(hasCoverage: false, distanceToCoverageStartMeters: null)
        };

        var mask = calculator.ComputeMask(speedMps: 5, sections);

        mask.Should().Be(0b0101u);
    }

    [Fact]
    public void ComputeMask_BelowSpeedGate_DisablesAllSections()
    {
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 2.0, lookAheadSeconds: 0.5);
        var sections = new[]
        {
            new SectionObservation(hasCoverage: true, distanceToCoverageStartMeters: null),
            new SectionObservation(hasCoverage: true, distanceToCoverageStartMeters: null)
        };

        var mask = calculator.ComputeMask(speedMps: 1.5, sections);

        mask.Should().Be(0u);
    }

    [Fact]
    public void ComputeMask_UsesLookAhead_WhenCoverageAhead()
    {
        var calculator = new SectionMaskCalculator(sectionCount: 3, minimumSpeedMps: 0.1, lookAheadSeconds: 2.0);
        var sections = new[]
        {
            new SectionObservation(hasCoverage: false, distanceToCoverageStartMeters: 5),
            new SectionObservation(hasCoverage: false, distanceToCoverageStartMeters: 12),
            new SectionObservation(hasCoverage: false, distanceToCoverageStartMeters: null)
        };

        var mask = calculator.ComputeMask(speedMps: 3, sections);

        mask.Should().Be(0b001u);
    }

    [Fact]
    public void ComputeMask_SuppressedSection_RemainsOff()
    {
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 0.0, lookAheadSeconds: 1.0);
        var sections = new[]
        {
            new SectionObservation(hasCoverage: true, distanceToCoverageStartMeters: null, isSuppressed: true),
            new SectionObservation(hasCoverage: false, distanceToCoverageStartMeters: 1)
        };

        var mask = calculator.ComputeMask(speedMps: 4, sections);

        mask.Should().Be(0b10u);
    }

    [Fact]
    public void ComputeMask_InvalidSectionCount_Throws()
    {
        var calculator = new SectionMaskCalculator(sectionCount: 2, minimumSpeedMps: 0.0, lookAheadSeconds: 0.0);
        var sections = new[]
        {
            new SectionObservation(hasCoverage: true, distanceToCoverageStartMeters: null)
        };

        var act = () => calculator.ComputeMask(speedMps: 3, sections);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidConfiguration_Throws()
    {
        var actCount = () => new SectionMaskCalculator(0, 0, 0);
        var actSpeed = () => new SectionMaskCalculator(2, -1, 0);
        var actLookAhead = () => new SectionMaskCalculator(2, 0, -1);

        actCount.Should().Throw<ArgumentOutOfRangeException>();
        actSpeed.Should().Throw<ArgumentOutOfRangeException>();
        actLookAhead.Should().Throw<ArgumentOutOfRangeException>();
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Aog.Plugins.Sections;
using FluentAssertions;
using Xunit;

namespace Aog.Plugins.Tests.Sections;

public sealed class RateControlParityValidationTests
{
    private static readonly string DataPath = Path.Combine("Sections", "Data", "LegacyRateControlScenarios.csv");

    [Fact]
    public void SectionMaskCalculator_MatchesLegacyScenarios()
    {
        var path = Path.Combine(AppContext.BaseDirectory, DataPath);
        var scenarios = LoadScenarios(path);

        foreach (var scenario in scenarios)
        {
            var calculator = new SectionMaskCalculator(scenario.SectionCount, scenario.MinimumSpeedMps, scenario.LookAheadSeconds);
            var mask = calculator.ComputeMask(scenario.SpeedMps, scenario.Sections);

            mask.Should().Be(scenario.ExpectedMask, $"Scenario {scenario.Id} should match legacy output.");
        }
    }

    private static IReadOnlyList<Scenario> LoadScenarios(string path)
    {
        var lines = File.ReadAllLines(path);
        var scenarios = new List<Scenario>();

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length != 7)
            {
                throw new InvalidDataException($"Unexpected column count in row: {line}");
            }

            var id = parts[0];
            var sectionCount = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var minSpeed = double.Parse(parts[2], CultureInfo.InvariantCulture);
            var lookAhead = double.Parse(parts[3], CultureInfo.InvariantCulture);
            var speed = double.Parse(parts[4], CultureInfo.InvariantCulture);
            var states = ParseStates(parts[5], sectionCount);
            var expectedMask = uint.Parse(parts[6], CultureInfo.InvariantCulture);

            scenarios.Add(new Scenario(id, sectionCount, minSpeed, lookAhead, speed, states, expectedMask));
        }

        return scenarios;
    }

    private static IReadOnlyList<SectionObservation> ParseStates(string input, int sectionCount)
    {
        var segments = input.Split(';', StringSplitOptions.TrimEntries);
        if (segments.Length != sectionCount)
        {
            throw new InvalidDataException($"Expected {sectionCount} section states but received {segments.Length}.");
        }

        var observations = new SectionObservation[sectionCount];
        for (var i = 0; i < sectionCount; i++)
        {
            var parts = segments[i].Split('|');
            if (parts.Length < 3)
            {
                throw new InvalidDataException($"Invalid section encoding '{segments[i]}'.");
            }

            var hasCoverage = parts[0] == "1";
            double? distance = null;
            if (!string.IsNullOrWhiteSpace(parts[1]))
            {
                distance = double.Parse(parts[1], CultureInfo.InvariantCulture);
            }

            var isSuppressed = parts[2] == "1";
            observations[i] = new SectionObservation(hasCoverage, distance, isSuppressed);
        }

        return observations;
    }

    private sealed record Scenario(
        string Id,
        int SectionCount,
        double MinimumSpeedMps,
        double LookAheadSeconds,
        double SpeedMps,
        IReadOnlyList<SectionObservation> Sections,
        uint ExpectedMask);
}

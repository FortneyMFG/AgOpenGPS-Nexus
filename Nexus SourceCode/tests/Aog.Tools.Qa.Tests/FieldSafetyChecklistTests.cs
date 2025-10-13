using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aog.Tools.Qa.Checklist;
using FluentAssertions;
using Xunit;

namespace Aog.Tools.Qa.Tests;

public static class FieldSafetyChecklistTests
{
    [Fact]
    public static void Template_contains_expected_sections()
    {
        var template = FieldSafetyChecklistTemplate.Create();

        template.Sections.Should().HaveCount(4);
        template.Sections.Select(s => s.Name).Should().Contain(new[]
        {
            "Pre-run readiness",
            "Hardware validation",
            "Software & failsafes",
            "Approvals"
        });

        foreach (var section in template.Sections)
        {
            section.Items.Should().NotBeEmpty("each section requires actionable items");
            section.Items.Should().OnlyContain(i => i.Status == ChecklistStatus.Pending);
        }
    }

    [Fact]
    public static void Validator_accepts_completed_checklist()
    {
        var checklist = LoadChecklist("checklists/completed.json");
        var result = FieldSafetyChecklistValidator.Validate(checklist);

        result.IsValid.Should().BeTrue(result.Errors.Count > 0
            ? string.Join(Environment.NewLine, result.Errors)
            : "expected no validation errors");
    }

    [Fact]
    public static void Validator_flags_pending_items()
    {
        var checklist = FieldSafetyChecklistTemplate.Create();
        var result = FieldSafetyChecklistValidator.Validate(checklist);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("pending", StringComparison.OrdinalIgnoreCase));
    }

    private static FieldSafetyChecklist LoadChecklist(string relativePath)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", relativePath.Replace('/', Path.DirectorySeparatorChar));
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<FieldSafetyChecklist>(stream, CreateOptions())
            ?? throw new InvalidOperationException("Checklist data could not be read.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}

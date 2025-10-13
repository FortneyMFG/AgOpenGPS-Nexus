using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aog.Tools.Qa.Checklist;

public sealed class FieldSafetyChecklist
{
    public string SiteName { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public string Approver { get; set; } = string.Empty;
    public IReadOnlyList<ChecklistSection> Sections { get; init; } = Array.Empty<ChecklistSection>();
}

public sealed class ChecklistSection
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<ChecklistItem> Items { get; init; } = Array.Empty<ChecklistItem>();
}

public sealed class ChecklistItem
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Guidance { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChecklistStatus Status { get; set; } = ChecklistStatus.Pending;
    public string? Notes { get; set; }

    public string? VerifiedBy { get; set; }

    public DateTimeOffset? VerifiedAtUtc { get; set; }

}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChecklistStatus
{
    Pending,
    Completed,
    NotApplicable
}

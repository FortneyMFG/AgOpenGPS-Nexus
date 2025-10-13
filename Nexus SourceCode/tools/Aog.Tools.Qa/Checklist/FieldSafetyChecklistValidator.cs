using System;
using System.Collections.Generic;

namespace Aog.Tools.Qa.Checklist;

public static class FieldSafetyChecklistValidator
{
    public static ChecklistValidationResult Validate(FieldSafetyChecklist checklist)
    {
        if (checklist is null)
        {
            throw new ArgumentNullException(nameof(checklist));
        }

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(checklist.SiteName))
        {
            errors.Add("Site name is required.");
        }

        if (string.IsNullOrWhiteSpace(checklist.Operator))
        {
            errors.Add("Operator name is required.");
        }

        if (string.IsNullOrWhiteSpace(checklist.Approver))
        {
            errors.Add("Approver name is required.");
        }

        if (checklist.Sections.Count == 0)
        {
            errors.Add("Checklist must contain at least one section.");
        }

        foreach (var section in checklist.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Name))
            {
                errors.Add("Checklist section name is required.");
            }

            if (section.Items.Count == 0)
            {
                errors.Add($"Section '{section.Name}' must include at least one item.");
            }

            foreach (var item in section.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Id))
                {
                    errors.Add($"Section '{section.Name}' contains an item without an id.");
                }

                if (string.IsNullOrWhiteSpace(item.Description))
                {
                    errors.Add($"Item '{item.Id}' requires a description.");
                }

                if (item.Status == ChecklistStatus.Pending)
                {
                    errors.Add($"Item '{item.Id}' is still pending.");
                }

                if (item.Status == ChecklistStatus.Completed)
                {
                    if (string.IsNullOrWhiteSpace(item.VerifiedBy))
                    {
                        errors.Add($"Item '{item.Id}' is completed but missing verifier.");
                    }

                    if (item.VerifiedAtUtc is null)
                    {
                        errors.Add($"Item '{item.Id}' is completed but missing verification timestamp.");
                    }
                }

                if (item.Status == ChecklistStatus.NotApplicable && string.IsNullOrWhiteSpace(item.Notes))
                {
                    errors.Add($"Item '{item.Id}' marked not applicable requires notes.");
                }
            }
        }

        return new ChecklistValidationResult(errors);
    }
}

public sealed class ChecklistValidationResult
{
    public ChecklistValidationResult(IReadOnlyList<string> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<string> Errors { get; }

    public bool IsValid => Errors.Count == 0;
}

using System;
using System.Collections.Generic;
using System.Linq;
using Aog.UI.Avalonia.ViewModels;

namespace Aog.UI.Avalonia.Blocks;

/// <summary>
/// Converts persisted block layout instances into sidebar button view-models.
/// </summary>
public sealed class BlockSidebarBuilder
{
    private static readonly IReadOnlyDictionary<string, string> CommandStatusMessages =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Host.ToggleAutosteer"] = "Autosteer toggled (mock).",
            ["Host.CycleAbLine"] = "Cycled AB line (mock).",
            ["Host.ToggleUTurn"] = "U-Turn toggled (mock).",
            ["Host.ToggleSections"] = "Section master toggled (mock).",
            ["Host.StartGuidance"] = "Starting autoguidance sequence (mock).",
            ["Host.PauseGuidance"] = "Autoguidance paused.",
            ["Host.StopGuidance"] = "Stopped autoguidance.",
            ["Host.NudgeLeft"] = "Nudged guidance line left by 2 cm.",
            ["Host.NudgeRight"] = "Nudged guidance line right by 2 cm.",
        };

    private readonly IBlockCatalog _catalog;
    private readonly Action<string> _statusReporter;

    public BlockSidebarBuilder(IBlockCatalog catalog, Action<string> statusReporter)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _statusReporter = statusReporter ?? throw new ArgumentNullException(nameof(statusReporter));
    }

    /// <summary>
    /// Builds sidebar buttons for the supplied region from persisted block instances.
    /// </summary>
    /// <param name="region">The target region to filter instances by.</param>
    /// <param name="instances">The persisted block instances.</param>
    /// <returns>A read-only list of sidebar buttons generated from the layout.</returns>
    public IReadOnlyList<SidebarButtonViewModel> Build(BlockRegion region, IEnumerable<BlockInstance> instances)
    {
        ArgumentNullException.ThrowIfNull(instances);

        var buttons = new List<SidebarButtonViewModel>();
        foreach (var instance in instances
                     .Where(instance => instance.Region == region)
                     .OrderBy(instance => instance.Order))
        {
            if (_catalog.Get(instance.DefinitionId) is not BlockDefinition definition)
            {
                continue;
            }

            if (definition.Kind != BlockKind.CommandButton)
            {
                continue;
            }

            var label = GetLabel(definition);
            var statusMessage = GetStatusMessage(definition);
            buttons.Add(new SidebarButtonViewModel(
                label,
                new DelegateCommand(_ => _statusReporter(statusMessage)),
                statusMessage));
        }

        return buttons;
    }

    private static string GetLabel(BlockDefinition definition)
    {
        return string.IsNullOrWhiteSpace(definition.Label) ? definition.Id.Value : definition.Label;
    }

    private static string GetStatusMessage(BlockDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(definition.CommandKey)
            && CommandStatusMessages.TryGetValue(definition.CommandKey, out var status))
        {
            return status;
        }

        return string.IsNullOrWhiteSpace(definition.Label)
            ? "Command executed."
            : $"{definition.Label} activated.";
    }
}

using Aog.Plugins;
using Aog.Plugins.Compatibility;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model for a single dependency issue surfaced in the Device Manager compatibility card.
/// </summary>
public sealed class DeviceManagerCompatibilityIssueViewModel
{
    public DeviceManagerCompatibilityIssueViewModel(PluginCompatibilityDependencyStatus status)
    {
        Kind = status.Kind;
        Identifier = status.Identifier;
        Classification = status.Classification;
        State = status.State;
        Message = status.Message;
    }

    public PluginDependencyKind Kind { get; }

    public string Identifier { get; }

    public PluginDependencyClassification Classification { get; }

    public PluginCompatibilityState State { get; }

    public string Message { get; }

    public string ClassificationDisplay => Classification switch
    {
        PluginDependencyClassification.Hard => "Hard",
        PluginDependencyClassification.Soft => "Soft",
        PluginDependencyClassification.Suggest => "Suggest",
        _ => Classification.ToString()
    };

    public string KindDisplay => Kind switch
    {
        PluginDependencyKind.Plugin => "Plugin",
        PluginDependencyKind.RuntimeApi => "API",
        PluginDependencyKind.Transport => "Transport",
        PluginDependencyKind.RuntimeVersion => "Runtime",
        PluginDependencyKind.Capability => "Capability",
        PluginDependencyKind.Profile => "Profile",
        PluginDependencyKind.Relationship => "Relationship",
        _ => Kind.ToString()
    };
}

using System.Collections.Generic;
using System.Linq;
using Aog.Plugins.Compatibility;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Presentation model representing a plugin row in the Device Manager compatibility dashboard.
/// </summary>
public sealed class DeviceManagerCompatibilityPluginViewModel
{
    public DeviceManagerCompatibilityPluginViewModel(PluginCompatibilityResult result)
    {
        PluginId = result.PluginId;
        Name = result.Name;
        Version = result.Version;
        IsOfficialBundleMember = result.IsOfficialBundleMember;
        State = result.State;
        Capabilities = result.Capabilities;
        Issues = result.Issues.Select(status => new DeviceManagerCompatibilityIssueViewModel(status)).ToList();
    }

    public string PluginId { get; }

    public string Name { get; }

    public string Version { get; }

    public bool IsOfficialBundleMember { get; }

    public PluginCompatibilityState State { get; }

    public IReadOnlyList<string> Capabilities { get; }

    public IReadOnlyList<DeviceManagerCompatibilityIssueViewModel> Issues { get; }

    public bool HasIssues => Issues.Count > 0;

    public string StateDisplay => State switch
    {
        PluginCompatibilityState.Blocked => "Blocked",
        PluginCompatibilityState.Warning => "Warnings",
        _ => "Healthy",
    };
}

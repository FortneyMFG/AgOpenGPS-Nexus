using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// View-model powering the mesh share/subscribe panel in the main window.
/// </summary>
public sealed class MeshSharePanelViewModel
{
    public MeshSharePanelViewModel(IReadOnlyList<MeshDeviceAccessViewModel> devices)
    {
        Devices = devices ?? throw new ArgumentNullException(nameof(devices));
        Summary = devices.Count == 0
            ? "No mesh devices are connected."
            : $"{devices.Count} mesh device(s) connected";
    }

    /// <summary>Gets the devices displayed in the panel.</summary>
    public IReadOnlyList<MeshDeviceAccessViewModel> Devices { get; }

    /// <summary>Gets a text summary describing the connected devices.</summary>
    public string Summary { get; }

    /// <summary>Creates a sample view-model with representative mesh devices.</summary>
    public static MeshSharePanelViewModel CreateSample()
    {
        var devices = new List<MeshDeviceAccessViewModel>
        {
            new(
                "combine.alpha",
                "Combine Alpha",
                "Online • Field 12",
                "Updated 45 s ago",
                "Presence, Trails, Coverage",
                new List<MeshAccessGrantViewModel>
                {
                    new("Season 2025 • Job Harvest AM", "Presence, Trails", "presence, trail"),
                    new("Season 2025 • Job Harvest AM", "Coverage", "coverage", "Full-resolution tiles")
                },
                new List<MeshAccessGrantViewModel>
                {
                    new("Season 2025 • Job Harvest AM", "Presence", "presence"),
                    new("Season 2025 • Job Harvest PM", "Coverage", "coverage", "Staged for offline sync")
                }),
            new(
                "sprayer.bravo",
                "Sprayer Bravo",
                "Offline • expected tonight",
                "Went offline 18 min ago",
                "Presence only",
                new List<MeshAccessGrantViewModel>
                {
                    new("Season 2025 • Any job", "Presence", "presence", "Operators approve coverage before sharing")
                },
                new List<MeshAccessGrantViewModel>
                {
                    new("Season 2025 • Job Fertilize East", "Coverage", "coverage"),
                }),
            new(
                "scout.delta",
                "Scout Tablet Delta",
                "Online • Barn Wi-Fi",
                "Updated 2 m ago",
                "Presence, Layers",
                new List<MeshAccessGrantViewModel>
                {
                    new("All seasons", "Presence", "presence"),
                    new("Season 2024 • Job Soil Sampling", "Layers", "soil.ph, soil.matter")
                },
                new List<MeshAccessGrantViewModel>
                {
                    new("All seasons", "Presence, Trails", "presence, trail"),
                    new("Season 2025 • Job Harvest AM", "Coverage", "coverage"),
                    new("Season 2025 • Job Harvest AM", "Layers", "yield.actual")
                })
        };

        return new MeshSharePanelViewModel(devices);
    }
}

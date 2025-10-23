using Avalonia.Controls;

namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Describes a top-level window contributed by a plugin.
/// </summary>
/// <param name="Id">Unique identifier for the window.</param>
/// <param name="Title">Display name presented to the operator.</param>
/// <param name="Factory">Factory that creates a configured <see cref="Window"/> instance.</param>
/// <param name="ShowOnStartup">Indicates whether the window should be shown immediately after creation.</param>
public sealed record WindowDescriptor(
    string Id,
    string Title,
    Func<IServiceProvider, Window> Factory,
    bool ShowOnStartup = false);

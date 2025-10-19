using Avalonia.Controls;

namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Describes a dashboard block contribution.
/// </summary>
/// <param name="Id">Unique identifier for the block.</param>
/// <param name="Title">Display name presented to the operator.</param>
/// <param name="Factory">Factory that builds the block control.</param>
/// <param name="Region">Optional layout region hint.</param>
public sealed record BlockDescriptor(
    string Id,
    string Title,
    Func<IServiceProvider, Control> Factory,
    string? Region = null);

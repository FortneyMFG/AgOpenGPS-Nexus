namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Describes an interactive tool contribution.
/// </summary>
/// <param name="Id">Unique identifier for the tool.</param>
/// <param name="Title">Display name presented to the operator.</param>
/// <param name="Factory">Factory that builds the tool instance.</param>
public sealed record ToolDescriptor(
    string Id,
    string Title,
    Func<IServiceProvider, IMapTool> Factory);

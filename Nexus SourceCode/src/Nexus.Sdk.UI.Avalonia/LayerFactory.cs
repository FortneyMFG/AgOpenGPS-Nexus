namespace Nexus.Sdk.UI.Avalonia;

/// <summary>
/// Describes how to build a map layer instance.
/// </summary>
/// <param name="Id">Unique identifier for the layer.</param>
/// <param name="Title">Display name presented to the operator.</param>
/// <param name="Factory">Factory that creates the layer.</param>
public sealed record LayerFactory(
    string Id,
    string Title,
    Func<IServiceProvider, IMapLayer> Factory);

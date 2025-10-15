namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Enumerates the operating modes supported by the Avalonia shell.
/// </summary>
public enum AvaloniaRunMode
{
    /// <summary>
    /// UI runs remotely and connects to Core/AgIO over the network.
    /// </summary>
    CompanionRemote = 0,

    /// <summary>
    /// Core and AgIO execute inside the UI process.
    /// </summary>
    LocalInProc = 1,

    /// <summary>
    /// Core and AgIO run in separate child processes launched by the UI.
    /// </summary>
    LocalOutOfProc = 2,
}

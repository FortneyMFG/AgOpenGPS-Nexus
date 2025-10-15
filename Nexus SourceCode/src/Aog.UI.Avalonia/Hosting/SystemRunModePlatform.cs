using System;

namespace Aog.UI.Avalonia.Hosting;

/// <summary>
/// Default platform detector that maps runtime OS to the preferred run mode.
/// </summary>
public sealed class SystemRunModePlatform : IRunModePlatform
{
    public AvaloniaRunMode GetDefaultMode()
    {
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            return AvaloniaRunMode.CompanionRemote;
        }

        return AvaloniaRunMode.LocalInProc;
    }
}

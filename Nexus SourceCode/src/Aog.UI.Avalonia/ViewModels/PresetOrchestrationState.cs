namespace Aog.UI.Avalonia.ViewModels;

/// <summary>
/// Describes the orchestration readiness of a preset application.
/// </summary>
public enum PresetOrchestrationState
{
    /// <summary>The preset is ready and all dependencies are satisfied.</summary>
    Ready,

    /// <summary>Background orchestration is still running for the preset.</summary>
    Running,

    /// <summary>The preset is blocked due to missing dependencies or health checks.</summary>
    Blocked
}

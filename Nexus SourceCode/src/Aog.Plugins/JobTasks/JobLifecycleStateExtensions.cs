using System;
using System.IO;
using Aog.Core.Jobs;

namespace Aog.Plugins.JobTasks;

internal static class JobLifecycleStateExtensions
{
    public static string ToSchemaValue(this JobLifecycleState state) => state switch
    {
        JobLifecycleState.Planned => "planned",
        JobLifecycleState.Mounted => "mounted",
        JobLifecycleState.Active => "active",
        JobLifecycleState.Paused => "paused",
        JobLifecycleState.Closed => "closed",
        JobLifecycleState.Completed => "completed",
        _ => throw new InvalidDataException($"Unsupported job lifecycle state '{state}'.")
    };

    public static JobLifecycleState ToLifecycleState(string value) => value switch
    {
        "planned" => JobLifecycleState.Planned,
        "mounted" => JobLifecycleState.Mounted,
        "active" => JobLifecycleState.Active,
        "paused" => JobLifecycleState.Paused,
        "closed" => JobLifecycleState.Closed,
        "completed" => JobLifecycleState.Completed,
        _ => throw new InvalidDataException($"Unsupported job lifecycle state '{value}'.")
    };
}

internal static class JobSessionStateExtensions
{
    public static string ToSchemaValue(this JobSessionState state) => state switch
    {
        JobSessionState.Active => "active",
        JobSessionState.Paused => "paused",
        JobSessionState.Completed => "completed",
        _ => throw new InvalidDataException($"Unsupported session state '{state}'.")
    };

    public static JobSessionState ToSessionState(string value) => value switch
    {
        "active" => JobSessionState.Active,
        "paused" => JobSessionState.Paused,
        "completed" => JobSessionState.Completed,
        _ => throw new InvalidDataException($"Unsupported session state '{value}'.")
    };
}

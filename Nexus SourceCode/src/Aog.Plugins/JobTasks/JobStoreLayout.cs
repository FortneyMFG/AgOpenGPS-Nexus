using System;

namespace Aog.Plugins.JobTasks;

/// <summary>
/// Describes the filesystem layout for a Nexus job bundle.
/// </summary>
/// <param name="JobRoot">Root directory that contains <c>job.json</c> and session data.</param>
/// <param name="DataDirectory">Directory that stores structured data such as layers and sessions.</param>
/// <param name="ResumeFile">Path to the legacy <c>Resume.txt</c> marker maintained for compatibility.</param>
/// <param name="AttachmentsDirectory">Optional directory that stores operator supplied attachments.</param>
public sealed record class JobStoreLayout(
    string JobRoot,
    string DataDirectory,
    string ResumeFile,
    string? AttachmentsDirectory = null)
{
    /// <summary>
    /// Gets the path to the canonical job manifest document.
    /// </summary>
    public string JobManifestPath => System.IO.Path.Combine(JobRoot ?? throw new InvalidOperationException("Job root not set."), "job.json");
}

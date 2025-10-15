using System;
using System.ComponentModel.DataAnnotations;

namespace Aog.Core.Jobs;

/// <summary>
/// Configuration options for the JobsService host.
/// </summary>
public sealed class JobsServiceOptions
{
    private string _rootDirectory = "jobs";
    private string _activeStateFileName = "active.json";
    private string _defaultSessionName = "Session 1";

    /// <summary>
    /// Gets or sets the root directory that stores all job folders.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string RootDirectory
    {
        get => _rootDirectory;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A root directory path is required.", nameof(value));
            }

            _rootDirectory = value.Trim();
        }
    }

    /// <summary>
    /// Gets or sets the file name that tracks the currently active job.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string ActiveStateFileName
    {
        get => _activeStateFileName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An active state file name is required.", nameof(value));
            }

            _activeStateFileName = value.Trim();
        }
    }

    /// <summary>
    /// Gets or sets the default session name used when a new job is created.
    /// </summary>
    [Required]
    [MinLength(1)]
    public string DefaultSessionName
    {
        get => _defaultSessionName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A default session name is required.", nameof(value));
            }

            _defaultSessionName = value.Trim();
        }
    }
}

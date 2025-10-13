using System;

namespace Aog.Agio.Safety;

/// <summary>
/// Persists structured safety log entries and manages retention/export.
/// </summary>
public interface ISafetyLog
{
    /// <summary>
    /// Appends a new entry to the safety log.
    /// </summary>
    /// <param name="entry">Entry to record.</param>
    void Record(SafetyLogEntry entry);

    /// <summary>
    /// Creates an archive containing the currently retained log files.
    /// </summary>
    /// <param name="destinationDirectory">Directory where the archive should be created.</param>
    /// <returns>Absolute path to the created archive.</returns>
    string Export(string destinationDirectory);
}

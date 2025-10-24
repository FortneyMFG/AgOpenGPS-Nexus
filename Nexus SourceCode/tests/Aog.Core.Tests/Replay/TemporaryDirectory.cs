using System;
using System.IO;

namespace Aog.Core.Tests.Replay;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix must be provided.", nameof(prefix));
        }

        var basePath = Path.Combine(Path.GetTempPath(), prefix);
        Directory.CreateDirectory(basePath);

        DirectoryPath = System.IO.Path.Combine(basePath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }

    public string DirectoryPath { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
        catch
        {
            // Swallow cleanup failures to avoid test flakiness on file locking.
        }
    }
}

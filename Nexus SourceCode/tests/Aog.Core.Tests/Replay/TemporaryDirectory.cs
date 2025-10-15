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

        Path = System.IO.Path.Combine(basePath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Swallow cleanup failures to avoid test flakiness on file locking.
        }
    }
}

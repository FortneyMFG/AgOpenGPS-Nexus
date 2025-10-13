using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Aog.Agio.Safety;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class FileSafetyLogTests : IDisposable
{
    private readonly string _root;

    public FileSafetyLogTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "safety-log-tests", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void Record_Enforces_Retention()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new SafetyLogOptions
        {
            Directory = _root,
            RetentionDays = 2,
            MaxFiles = 2,
        });

        var log = new FileSafetyLog(options, timeProvider);

        log.Record(SafetyLogEntry.HeartbeatReceived(timeProvider.GetUtcNow(), TimeSpan.FromMilliseconds(100), resumed: true));

        timeProvider.Advance(TimeSpan.FromDays(1));
        log.Record(SafetyLogEntry.HeartbeatReceived(timeProvider.GetUtcNow(), TimeSpan.FromMilliseconds(100), resumed: false));

        timeProvider.Advance(TimeSpan.FromDays(1));
        log.Record(SafetyLogEntry.HeartbeatExpired(timeProvider.GetUtcNow(), TimeSpan.FromMilliseconds(100), SafetyLogEvents.HeartbeatReasons.Timeout));

        var files = Directory.Exists(_root) ? Directory.GetFiles(_root, "*.jsonl") : Array.Empty<string>();
        Assert.Equal(2, files.Length);

        var minDate = new DateOnly(2024, 1, 2);
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var parts = name.Split('-', StringSplitOptions.RemoveEmptyEntries);
            var dateText = parts[^1];
            var date = DateOnly.ParseExact(dateText, "yyyyMMdd", CultureInfo.InvariantCulture);
            Assert.True(date >= minDate, $"Expected {file} to be newer than {minDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        }
    }

    [Fact]
    public void Export_Creates_Zip_With_Current_Logs()
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new SafetyLogOptions
        {
            Directory = _root,
            RetentionDays = 7,
            MaxFiles = 10,
        });

        var log = new FileSafetyLog(options, timeProvider);
        log.Record(SafetyLogEntry.FailsafeApplied(timeProvider.GetUtcNow(), SafetyLogEvents.Actuators.Steer, "disable"));

        var exportRoot = Path.Combine(_root, "exports");
        var archive = log.Export(exportRoot);

        Assert.True(File.Exists(archive));

        using var zip = ZipFile.OpenRead(archive);
        Assert.Single(zip.Entries);
        Assert.EndsWith(".jsonl", zip.Entries.First().FullName, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}

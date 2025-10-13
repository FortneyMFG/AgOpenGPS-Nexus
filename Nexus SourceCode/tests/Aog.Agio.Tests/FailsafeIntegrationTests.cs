using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aog.Agio.Safety;
using Aog.Core.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Aog.Agio.Tests;

public sealed class FailsafeIntegrationTests : IAsyncLifetime
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly string _logRoot = Path.Combine(Path.GetTempPath(), "failsafe-int-tests", Guid.NewGuid().ToString("N"));
    private IHost? _host;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_logRoot);

        var builder = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AgioHost:Safety:HeartbeatMs"] = "100",
                    ["AgioHost:SafetyLogs:Directory"] = _logRoot,
                    ["AgioHost:SafetyLogs:RetentionDays"] = "7",
                    ["AgioHost:SafetyLogs:MaxFiles"] = "30",
                });
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<TimeProvider>(_timeProvider);
            });

        _host = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await _host.StartAsync(cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _host.StopAsync(cts.Token);
            _host.Dispose();
        }

        if (Directory.Exists(_logRoot))
        {
            try
            {
                Directory.Delete(_logRoot, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    [Fact]
    public void Failsafe_Flow_Produces_Safety_Log()
    {
        Assert.NotNull(_host);
        var service = _host!.Services.GetRequiredService<IActuatorFailsafeService>();

        service.ReportHeartbeat();

        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));
        service.FilterSteerCommand(new SteerCmd { Enable = true, TargetWheelAngleDeg = 2 });

        var files = Directory.GetFiles(_logRoot, "*.jsonl");
        Assert.Single(files);

        var lines = File.ReadAllLines(files[0]).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        Assert.True(lines.Length >= 3);

        using var doc = JsonDocument.Parse(lines[0]);
        Assert.Equal(SafetyLogEvents.HeartbeatReceived, doc.RootElement.GetProperty("eventType").GetString());

        using var doc2 = JsonDocument.Parse(lines[1]);
        Assert.Equal(SafetyLogEvents.HeartbeatExpired, doc2.RootElement.GetProperty("eventType").GetString());

        using var doc3 = JsonDocument.Parse(lines[2]);
        Assert.Equal(SafetyLogEvents.FailsafeApplied, doc3.RootElement.GetProperty("eventType").GetString());
    }
}

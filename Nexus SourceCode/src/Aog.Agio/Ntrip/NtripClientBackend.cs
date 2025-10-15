using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Agio.Ntrip;

/// <summary>
/// Registers services that provide the official NTRIP client backend.
/// </summary>
public sealed class NtripClientBackend : IAgioBackend
{
    /// <inheritdoc />
    public string Name => "NTRIP Client";

    /// <inheritdoc />
    public void ConfigureServices(IServiceCollection services)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services
            .AddOptions<NtripClientOptions>()
            .BindConfiguration("AgioHost:NtripClient")
            .PostConfigure(options => options.Validate());

        services.AddSingleton<INtripCorrectionSink, AogLinkNtripCorrectionSink>();
        services.AddSingleton<NtripClient>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<NtripClientOptions>>().Value;
            var sinks = provider.GetServices<INtripCorrectionSink>();
            var logger = provider.GetRequiredService<ILogger<NtripClient>>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();
            return new NtripClient(options, sinks, logger, timeProvider);
        });

        services.AddHostedService<NtripCorrectionService>();
    }
}

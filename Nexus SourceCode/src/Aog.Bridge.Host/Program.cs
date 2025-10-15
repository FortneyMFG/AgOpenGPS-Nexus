using Aog.Agio.Legacy;
using Aog.Bridge.Host.AogLink;
using Aog.Bridge.Host.AogLink.Legacy;
using Aog.Bridge.Host.AogLink.Transports;
using Aog.Link.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using GenericHost = Microsoft.Extensions.Hosting.Host;

namespace Aog.Bridge.Host;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();

        try
        {
            using var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Bridge host terminated unexpectedly.");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[]? args = null) =>
        GenericHost.CreateDefaultBuilder(args ?? Array.Empty<string>())
            .ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                configurationBuilder.AddEnvironmentVariables(prefix: "NEXUS_");
            })
            .UseSerilog((context, services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .WriteTo.Console();
            })
            .ConfigureServices((context, services) =>
            {
                services
                    .AddOptions<BridgeHostOptions>()
                    .BindConfiguration("BridgeHost")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddSingleton(TimeProvider.System);

                services.AddSingleton(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<BridgeHostOptions>>().Value;
                    return new AogLinkNodeIdentity(
                        options.NodeId,
                        options.FirmwareVersion,
                        NodeRole.NodeRoleHost,
                        NodePriority.NodePriorityHigh);
                });

                services.AddSingleton<AogLinkTranslator>();
                services.AddSingleton<LegacyPoseCodec>();
                services.AddSingleton<LegacySteerCodec>();
                services.AddSingleton<LegacyDiscoveryCodec>();
                services.AddSingleton<LegacyCompatibilityBridge>();
                services.AddSingleton<IAogLinkTransport, UdpAogLinkTransport>();
                services.AddSingleton<IAogLinkGateway, AogLinkGateway>();
                services.AddHostedService<BridgeHostedService>();

                services.AddLogging(builder => builder.AddSerilog());
            });
}

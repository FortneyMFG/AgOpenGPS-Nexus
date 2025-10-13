using System;
using System.Collections.Generic;
using System.Net.Http;
using Aog.Core.Capabilities;
using Aog.Core.Host.Capabilities;
using Aog.Protos.Capabilities.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using GenericHost = Microsoft.Extensions.Hosting.Host;

namespace Aog.Core.Host;

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
            Log.Fatal(ex, "Core host terminated unexpectedly.");
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
                // Core health options
                services
                    .AddOptions<CoreHealthOptions>()
                    .BindConfiguration("CoreHost:Health")
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                // AGiO connection options
                services
                    .AddOptions<AgioConnectionOptions>()
                    .BindConfiguration("CoreHost:Agio")
                    .ValidateDataAnnotations()
                    .Validate(static options => Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _),
                        "CoreHost:Agio:Endpoint must be an absolute URI.")
                    .ValidateOnStart();

                // Core capabilities options
                services
                    .AddOptions<CoreCapabilitiesOptions>()
                    .BindConfiguration("CoreHost:Capabilities")
                    .ValidateDataAnnotations()
                    .Validate(static options => !string.IsNullOrWhiteSpace(options.NodeId),
                        "CoreHost:Capabilities:NodeId is required.")
                    .Validate(static options => !string.IsNullOrWhiteSpace(options.SessionPrefix),
                        "CoreHost:Capabilities:SessionPrefix is required.")
                    .ValidateOnStart();

                // Capability descriptor factory (defaults + attributes)
                services.AddSingleton(provider =>
                {
                    var options = provider.GetRequiredService<IOptions<CoreCapabilitiesOptions>>().Value;
                    IDictionary<string, string>? attributes = null;

                    if (options.DefaultCapabilityAttributes.Count > 0)
                    {
                        attributes = new Dictionary<string, string>(options.DefaultCapabilityAttributes, StringComparer.OrdinalIgnoreCase);
                    }

                    return new CapabilityDescriptorFactory(
                        options.DefaultCapabilityVersion,
                        options.DefaultCapabilitySummary,
                        attributes);
                });

                // Capabilities client + gRPC wiring
                services.AddSingleton<CoreCapabilitiesClient>();

                services.AddGrpcClient<CapabilitiesService.CapabilitiesServiceClient>((provider, clientOptions) =>
                {
                    var agioOptions = provider.GetRequiredService<IOptions<AgioConnectionOptions>>().Value;
                    clientOptions.Address = new Uri(agioOptions.Endpoint, UriKind.Absolute);
                })
                .ConfigurePrimaryHttpMessageHandler(provider =>
                {
                    var agioOptions = provider.GetRequiredService<IOptions<AgioConnectionOptions>>().Value;

                    if (agioOptions.AllowUnencryptedHttp2)
                    {
                        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
                    }

                    return new SocketsHttpHandler();
                });

                services.AddSingleton<ICapabilitiesHandshakeClient, GrpcCapabilitiesHandshakeClient>();

                // Also register system time provider (from develop)
                services.AddSingleton(TimeProvider.System);

                // Hosted services
                services.AddHostedService<CoreHealthService>();
                services.AddHostedService<CapabilitiesHandshakeService>();
            });
}

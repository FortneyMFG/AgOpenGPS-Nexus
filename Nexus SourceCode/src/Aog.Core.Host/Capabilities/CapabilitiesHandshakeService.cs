using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aog.Core.Capabilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aog.Core.Host.Capabilities;

/// <summary>
/// Performs the Core ↔ AGiO capabilities handshake when the host starts.
/// </summary>
public sealed class CapabilitiesHandshakeService : BackgroundService
{
    private readonly ILogger<CapabilitiesHandshakeService> _logger;
    private readonly IOptions<CoreCapabilitiesOptions> _capabilitiesOptions;
    private readonly IOptions<AgioConnectionOptions> _connectionOptions;
    private readonly CoreCapabilitiesClient _requestFactory;
    private readonly ICapabilitiesHandshakeClient _handshakeClient;

    public CapabilitiesHandshakeService(
        ILogger<CapabilitiesHandshakeService> logger,
        IOptions<CoreCapabilitiesOptions> capabilitiesOptions,
        IOptions<AgioConnectionOptions> connectionOptions,
        CoreCapabilitiesClient requestFactory,
        ICapabilitiesHandshakeClient handshakeClient)
    {
        _logger = logger;
        _capabilitiesOptions = capabilitiesOptions;
        _connectionOptions = connectionOptions;
        _requestFactory = requestFactory;
        _handshakeClient = handshakeClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var capabilities = _capabilitiesOptions.Value;
        var connection = _connectionOptions.Value;

        var sessionId = capabilities.SessionPrefix + Guid.NewGuid().ToString("N");
        var request = _requestFactory.BuildHandshake(
            capabilities.NodeId,
            capabilities.AdvertisedCapabilities,
            sessionId);

        _logger.LogInformation(
            "Initiating capabilities handshake with AGiO at {Endpoint} for node {NodeId}.",
            connection.Endpoint,
            capabilities.NodeId);

        try
        {
            var response = await _handshakeClient.HandshakeAsync(request, stoppingToken).ConfigureAwait(false);

            if (response.AcceptedCapabilities.Count > 0)
            {
                var acceptedNames = response.AcceptedCapabilities
                    .Select(descriptor => descriptor.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToArray();

                _logger.LogInformation(
                    "AGiO accepted {Count} capabilities: {Capabilities}.",
                    acceptedNames.Length,
                    string.Join(", ", acceptedNames));
            }
            else
            {
                _logger.LogWarning("AGiO did not accept any advertised capabilities.");
            }

            foreach (var rejection in response.Rejections)
            {
                var capabilityName = rejection.Capability?.Name;
                var reason = string.IsNullOrWhiteSpace(rejection.Reason)
                    ? "No reason provided."
                    : rejection.Reason;

                _logger.LogWarning(
                    "Capability {Capability} was rejected by AGiO: {Reason}.",
                    string.IsNullOrWhiteSpace(capabilityName) ? "<unknown>" : capabilityName,
                    reason);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Capabilities handshake cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Capabilities handshake failed.");
        }
    }
}

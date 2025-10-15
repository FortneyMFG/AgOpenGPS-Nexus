using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Aog.Agio.Ntrip;

/// <summary>
/// Hosted service that runs the NTRIP client for the lifetime of the AGiO host.
/// </summary>
public sealed class NtripCorrectionService : BackgroundService
{
    private readonly NtripClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="NtripCorrectionService"/> class.
    /// </summary>
    public NtripCorrectionService(NtripClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _client.RunAsync(stoppingToken);
    }
}

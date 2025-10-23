using System.Linq;
using Grpc.Net.Client;
using Nexus.Cli.Reflection.V1;

namespace Nexus.Cli.Host.Plugins.Reflection;

/// <summary>
/// Default implementation of <see cref="IPluginReflectionClient"/> backed by the gRPC reflection contract.
/// </summary>
public sealed class PluginReflectionClient : IPluginReflectionClient
{
    private readonly IHttpClientFactory? _httpClientFactory;

    public PluginReflectionClient(IHttpClientFactory? httpClientFactory = null)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PluginVerb>> ListVerbsAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        if (endpoint is null)
        {
            throw new ArgumentNullException(nameof(endpoint));
        }

        using var channel = CreateChannel(endpoint);
        var client = new PluginReflection.PluginReflectionClient(channel);
        var response = await client.ListVerbsAsync(new ListVerbsRequest(), cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.Verbs
            .Select(ToVerb)
            .Where(static verb => verb is not null)
            .Select(static verb => verb!)
            .ToList();
    }

    private GrpcChannel CreateChannel(Uri endpoint)
    {
        if (_httpClientFactory is not null)
        {
            var httpClient = _httpClientFactory.CreateClient("nx-cli-reflection");
            return GrpcChannel.ForAddress(endpoint, new GrpcChannelOptions { HttpClient = httpClient });
        }

        return GrpcChannel.ForAddress(endpoint);
    }

    private static PluginVerb? ToVerb(VerbDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Name))
        {
            return null;
        }

        var options = descriptor.Options
            .Select(ToOption)
            .Where(static option => option is not null)
            .Select(static option => option!)
            .ToList();

        return new PluginVerb(
            descriptor.Name,
            string.IsNullOrWhiteSpace(descriptor.Description) ? descriptor.Name : descriptor.Description,
            options);
    }

    private static PluginVerbOption? ToOption(OptionDescriptor option)
    {
        if (string.IsNullOrWhiteSpace(option.Name))
        {
            return null;
        }

        var kind = option.ValueKind switch
        {
            OptionValueKind.Bool => PluginVerbOptionKind.Bool,
            OptionValueKind.Double => PluginVerbOptionKind.Double,
            OptionValueKind.Int32 => PluginVerbOptionKind.Int32,
            _ => PluginVerbOptionKind.String,
        };

        return new PluginVerbOption(
            option.Name,
            string.IsNullOrWhiteSpace(option.Description) ? option.Name : option.Description,
            option.Required,
            kind,
            string.IsNullOrWhiteSpace(option.DefaultValue) ? null : option.DefaultValue);
    }
}

using System.Linq;
using System.Net.Http;
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
        var options = new GrpcChannelOptions();

        switch (endpoint.Scheme)
        {
            case "http":
                options.HttpHandler = new SocketsHttpHandler
                {
                    Http2UnencryptedSupport = true,
                };
                options.Credentials = Grpc.Core.ChannelCredentials.Insecure;
                break;
            case "https":
                if (_httpClientFactory is not null)
                {
                    options.HttpClient = _httpClientFactory.CreateClient("nx-cli-reflection");
                }

                break;
            default:
                throw new NotSupportedException($"Unsupported URI scheme '{endpoint.Scheme}'.");
        }

        return GrpcChannel.ForAddress(endpoint, options);
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
            OptionValueKind.OptionValueKindBool => PluginVerbOptionKind.Bool,
            OptionValueKind.OptionValueKindDouble => PluginVerbOptionKind.Double,
            OptionValueKind.OptionValueKindInt32 => PluginVerbOptionKind.Int32,
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

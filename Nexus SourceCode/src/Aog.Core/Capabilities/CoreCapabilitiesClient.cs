using System;
using System.Collections.Generic;
using Aog.Protos.Capabilities.V1;

namespace Aog.Core.Capabilities;

/// <summary>
/// Builds <see cref="HandshakeRequest"/> messages for the Core host.
/// </summary>
public sealed class CoreCapabilitiesClient
{
    private readonly CapabilityDescriptorFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CoreCapabilitiesClient"/> class.
    /// </summary>
    /// <param name="factory">Factory used to materialize capability descriptors for handshake messages.</param>
    public CoreCapabilitiesClient(CapabilityDescriptorFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Creates a handshake request that advertises the provided capabilities.
    /// </summary>
    /// <param name="nodeId">Identifier assigned to the node emitting the handshake.</param>
    /// <param name="capabilityNames">Canonical capability names that should be advertised to the Core host.</param>
    /// <param name="sessionId">Identifier tying the handshake to the active session.</param>
    /// <returns>A populated handshake request ready to dispatch to the Core host.</returns>
    public HandshakeRequest BuildHandshake(string nodeId, IEnumerable<string> capabilityNames, string sessionId)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            throw new ArgumentException("Node identifier is required.", nameof(nodeId));
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("Session identifier is required.", nameof(sessionId));
        }

        if (capabilityNames is null)
        {
            throw new ArgumentNullException(nameof(capabilityNames));
        }

        var request = new HandshakeRequest
        {
            SessionId = sessionId,
            NodeId = nodeId,
            Role = CapabilityRole.Core
        };

        var descriptors = _factory.Create(capabilityNames);
        request.Capabilities.Add(descriptors);

        return request;
    }
}

/// <summary>
/// Produces <see cref="CapabilityDescriptor"/> instances with consistent metadata.
/// </summary>
public sealed class CapabilityDescriptorFactory
{
    private readonly string? _defaultVersion;
    private readonly string? _defaultSummary;
    private readonly IReadOnlyDictionary<string, string> _defaultAttributes;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityDescriptorFactory"/> class.
    /// </summary>
    /// <param name="defaultVersion">Fallback version applied when a capability definition omits one.</param>
    /// <param name="defaultSummary">Fallback summary applied when a capability definition omits one.</param>
    /// <param name="defaultAttributes">Fallback attribute set merged into each generated descriptor.</param>
    public CapabilityDescriptorFactory(
        string? defaultVersion = null,
        string? defaultSummary = null,
        IReadOnlyDictionary<string, string>? defaultAttributes = null)
    {
        _defaultVersion = defaultVersion;
        _defaultSummary = defaultSummary;
        _defaultAttributes = defaultAttributes ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Generates descriptors for the provided capability names, skipping empty entries
    /// and de-duplicating on the canonical name.
    /// </summary>
    /// <param name="capabilityNames">Capability names requested by the caller.</param>
    /// <returns>An ordered sequence of capability descriptors suitable for publishing in a handshake.</returns>
    public IEnumerable<CapabilityDescriptor> Create(IEnumerable<string> capabilityNames)
    {
        if (capabilityNames is null)
        {
            throw new ArgumentNullException(nameof(capabilityNames));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in capabilityNames)
        {
            var trimmed = name?.Trim();
            if (string.IsNullOrEmpty(trimmed) || !seen.Add(trimmed))
            {
                continue;
            }

            var descriptor = new CapabilityDescriptor
            {
                Name = trimmed,
            };

            if (CapabilityRegistry.TryGetDefinition(trimmed, out var definition))
            {
                if (!string.IsNullOrWhiteSpace(definition.DefaultVersion))
                {
                    descriptor.Version = definition.DefaultVersion;
                }

                if (!string.IsNullOrWhiteSpace(definition.Summary))
                {
                    descriptor.Summary = definition.Summary;
                }

                foreach (var pair in definition.Attributes)
                {
                    descriptor.Attributes[pair.Key] = pair.Value;
                }
            }

            if (!string.IsNullOrWhiteSpace(_defaultVersion) && string.IsNullOrWhiteSpace(descriptor.Version))
            {
                descriptor.Version = _defaultVersion;
            }

            if (!string.IsNullOrWhiteSpace(_defaultSummary) && string.IsNullOrWhiteSpace(descriptor.Summary))
            {
                descriptor.Summary = _defaultSummary;
            }

            foreach (var kvp in _defaultAttributes)
            {
                if (!descriptor.Attributes.ContainsKey(kvp.Key))
                {
                    descriptor.Attributes[kvp.Key] = kvp.Value;
                }
            }

            yield return descriptor;
        }
    }
}

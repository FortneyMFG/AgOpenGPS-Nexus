using System;
using System.Collections.Generic;
using System.Linq;

namespace Aog.Core.Provenance;

/// <summary>
/// Incrementally constructs a provenance directed acyclic graph and validates structural rules.
/// </summary>
public sealed class ProvenanceDagBuilder
{
    private readonly Dictionary<string, ProvenanceNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<(string From, string To, string Relationship)> _edges = new();

    /// <summary>
    /// Adds or replaces the provenance node with the supplied metadata.
    /// </summary>
    public void AddOrUpdateNode(string id, string label, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Node identifier is required.", nameof(id));
        }

        label ??= string.Empty;
        metadata ??= new Dictionary<string, string>();
        _nodes[id] = new ProvenanceNode(id, label, metadata);
    }

    /// <summary>
    /// Adds a directed edge to the graph.
    /// </summary>
    public void AddEdge(string from, string to, string relationship)
    {
        if (!_nodes.ContainsKey(from))
        {
            throw new InvalidOperationException($"Edge references unknown source node '{from}'.");
        }

        if (!_nodes.ContainsKey(to))
        {
            throw new InvalidOperationException($"Edge references unknown target node '{to}'.");
        }

        relationship ??= string.Empty;
        if (!_edges.Add((from, to, relationship)))
        {
            throw new InvalidOperationException($"Duplicate edge '{from}' -> '{to}' with relationship '{relationship}'.");
        }
    }

    /// <summary>
    /// Validates the constructed graph and returns an immutable representation.
    /// </summary>
    /// <exception cref="ProvenanceDagValidationException">Thrown when structural rules are violated.</exception>
    public ProvenanceDag Build()
    {
        var errors = new List<string>();
        if (_nodes.Count == 0)
        {
            errors.Add("At least one provenance node must be defined.");
        }

        if (_edges.Count == 0 && _nodes.Count > 1)
        {
            errors.Add("Multiple nodes require edges to describe their lineage.");
        }

        var adjacency = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in _nodes.Values)
        {
            indegree[node.Id] = 0;
            adjacency[node.Id] = new List<string>();
        }

        foreach (var (from, to, _) in _edges)
        {
            adjacency[from].Add(to);
            indegree[to]++;
        }

        var queue = new Queue<string>(indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        if (queue.Count == 0 && _nodes.Count > 0)
        {
            errors.Add("Provenance graph must contain at least one root node with no incoming edges.");
        }

        var processed = new List<string>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            processed.Add(current);

            foreach (var next in adjacency[current])
            {
                indegree[next]--;
                if (indegree[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        if (processed.Count != _nodes.Count && _nodes.Count > 0)
        {
            errors.Add("Provenance graph contains cycles or disconnected components.");
        }

        if (errors.Count > 0)
        {
            throw new ProvenanceDagValidationException(errors);
        }

        var nodes = _nodes.Values
            .OrderBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var edges = _edges
            .OrderBy(edge => edge.From, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.To, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.Relationship, StringComparer.OrdinalIgnoreCase)
            .Select(edge => new ProvenanceEdge(edge.From, edge.To, edge.Relationship))
            .ToArray();

        return new ProvenanceDag(nodes, edges);
    }
}

/// <summary>
/// Immutable provenance graph produced by <see cref="ProvenanceDagBuilder"/>.
/// </summary>
/// <param name="Nodes">Nodes participating in the provenance DAG.</param>
/// <param name="Edges">Directed edges describing lineage relationships.</param>
public sealed record ProvenanceDag(IReadOnlyList<ProvenanceNode> Nodes, IReadOnlyList<ProvenanceEdge> Edges)
{
    /// <summary>
    /// Gets the node identifiers that do not have incoming edges.
    /// </summary>
    public IReadOnlyList<string> GetRootNodeIds()
    {
        var children = new HashSet<string>(Edges.Select(edge => edge.Target), StringComparer.OrdinalIgnoreCase);
        return Nodes.Select(node => node.Id).Where(id => !children.Contains(id)).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}

/// <summary>
/// Describes a provenance node.
/// </summary>
/// <param name="Id">Unique identifier for the node.</param>
/// <param name="Label">Human-readable label describing the node.</param>
/// <param name="Metadata">Arbitrary metadata associated with the node.</param>
public sealed record ProvenanceNode(string Id, string Label, IReadOnlyDictionary<string, string> Metadata);

/// <summary>
/// Directed edge between two provenance nodes.
/// </summary>
/// <param name="Source">Identifier of the source node.</param>
/// <param name="Target">Identifier of the target node.</param>
/// <param name="Relationship">Description of the relationship.</param>
public sealed record ProvenanceEdge(string Source, string Target, string Relationship)
{
    /// <summary>
    /// Gets the edge source identifier.
    /// </summary>
    public string From => Source;

    /// <summary>
    /// Gets the edge target identifier.
    /// </summary>
    public string To => Target;
}

/// <summary>
/// Raised when provenance graph validation fails.
/// </summary>
public sealed class ProvenanceDagValidationException : InvalidOperationException
{
    public ProvenanceDagValidationException(IReadOnlyList<string> errors)
        : base("Provenance DAG validation failed.")
    {
        Errors = errors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets the validation errors produced during graph construction.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
}

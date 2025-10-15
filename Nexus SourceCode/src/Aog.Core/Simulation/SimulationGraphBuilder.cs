using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aog.Core.Simulation;

/// <summary>
/// Validates provider dependencies and produces a deterministic execution order.
/// </summary>
internal static class SimulationGraphBuilder
{
    public static SimulationGraph Build(IEnumerable<SimulationProviderDescriptor> providers)
    {
        if (providers is null)
        {
            throw new ArgumentNullException(nameof(providers));
        }

        var providerList = providers.ToList();
        var topicOwners = BuildTopicOwners(providerList);
        var dependencies = BuildDependencyMap(providerList, topicOwners);
        var dependents = BuildDependentsMap(providerList, dependencies);
        var sorted = TopologicalSort(providerList, dependencies, dependents);

        return new SimulationGraph(sorted);
    }

    private static Dictionary<string, SimulationProviderDescriptor> BuildTopicOwners(
        IReadOnlyCollection<SimulationProviderDescriptor> providers)
    {
        var topicOwners = new Dictionary<string, SimulationProviderDescriptor>(StringComparer.Ordinal);
        foreach (var provider in providers)
        {
            foreach (var topic in provider.Outputs)
            {
                if (topicOwners.TryGetValue(topic, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Providers '{existing.ProviderId}' and '{provider.ProviderId}' both declare output topic '{topic}'.");
                }

                topicOwners[topic] = provider;
            }
        }

        return topicOwners;
    }

    private static Dictionary<SimulationProviderDescriptor, HashSet<SimulationProviderDescriptor>> BuildDependencyMap(
        IReadOnlyCollection<SimulationProviderDescriptor> providers,
        IReadOnlyDictionary<string, SimulationProviderDescriptor> topicOwners)
    {
        var dependencies = new Dictionary<SimulationProviderDescriptor, HashSet<SimulationProviderDescriptor>>();

        foreach (var provider in providers)
        {
            var deps = new HashSet<SimulationProviderDescriptor>();
            foreach (var input in provider.Inputs)
            {
                if (topicOwners.TryGetValue(input, out var owner))
                {
                    if (!ReferenceEquals(owner, provider))
                    {
                        deps.Add(owner);
                    }

                    continue;
                }

                throw new InvalidOperationException(
                    $"Provider '{provider.ProviderId}' requires input topic '{input}' but no registered provider produces it.");
            }

            dependencies[provider] = deps;
        }

        return dependencies;
    }

    private static Dictionary<SimulationProviderDescriptor, HashSet<SimulationProviderDescriptor>> BuildDependentsMap(
        IReadOnlyCollection<SimulationProviderDescriptor> providers,
        IReadOnlyDictionary<SimulationProviderDescriptor, HashSet<SimulationProviderDescriptor>> dependencies)
    {
        var dependents = new Dictionary<SimulationProviderDescriptor, HashSet<SimulationProviderDescriptor>>();
        foreach (var provider in providers)
        {
            dependents[provider] = new HashSet<SimulationProviderDescriptor>();
        }

        foreach (var (provider, deps) in dependencies)
        {
            foreach (var dependency in deps)
            {
                dependents[dependency].Add(provider);
            }
        }

        return dependents;
    }

    private static IReadOnlyList<SimulationProviderDescriptor> TopologicalSort(
        IReadOnlyCollection<SimulationProviderDescriptor> providers,
        IReadOnlyDictionary<SimulationProviderDescriptor, HashSet<SimulationProviderDescriptor>> dependencies,
        IReadOnlyDictionary<SimulationProviderDescriptor, HashSet<SimulationProviderDescriptor>> dependents)
    {
        var inDegree = providers.ToDictionary(p => p, p => dependencies[p].Count);
        var queue = new Queue<SimulationProviderDescriptor>(inDegree.Where(pair => pair.Value == 0).Select(pair => pair.Key));
        var ordered = new List<SimulationProviderDescriptor>();

        while (queue.Count > 0)
        {
            var provider = queue.Dequeue();
            ordered.Add(provider);

            foreach (var dependent in dependents[provider])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0)
                {
                    queue.Enqueue(dependent);
                }
            }
        }

        if (ordered.Count != providers.Count)
        {
            var cyclicProviders = inDegree
                .Where(pair => pair.Value > 0)
                .Select(pair => pair.Key.ProviderId)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();

            var message = new StringBuilder()
                .Append("Cycle detected in simulation provider graph involving: ")
                .Append(string.Join(", ", cyclicProviders))
                .ToString();

            throw new InvalidOperationException(message);
        }

        return ordered;
    }
}

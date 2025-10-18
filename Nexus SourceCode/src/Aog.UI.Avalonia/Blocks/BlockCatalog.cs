using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Aog.UI.Avalonia.Blocks;

/// <summary>
/// Provides block definitions contributed by core shell and plugins.
/// </summary>
public interface IBlockProvider
{
    IEnumerable<BlockDefinition> GetBlocks();
}

/// <summary>
/// Exposes the aggregated set of registered block definitions.
/// </summary>
public interface IBlockCatalog
{
    IEnumerable<BlockDefinition> All();

    BlockDefinition? Get(BlockDefinitionId id);
}

/// <summary>
/// Aggregates block definitions from registered providers into a lookup catalog.
/// </summary>
public sealed class BlockCatalog : IBlockCatalog
{
    private readonly IReadOnlyDictionary<BlockDefinitionId, BlockDefinition> _definitions;

    public BlockCatalog(IEnumerable<IBlockProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var definitions = new Dictionary<BlockDefinitionId, BlockDefinition>();
        foreach (var provider in providers)
        {
            if (provider is null)
            {
                continue;
            }

            foreach (var definition in provider.GetBlocks() ?? Enumerable.Empty<BlockDefinition>())
            {
                if (definition?.Id is null)
                {
                    continue;
                }

                if (!definitions.TryAdd(definition.Id, definition))
                {
                    throw new InvalidOperationException($"Duplicate block definition id '{definition.Id.Value}' detected.");
                }
            }
        }

        _definitions = new ReadOnlyDictionary<BlockDefinitionId, BlockDefinition>(definitions);
    }

    public IEnumerable<BlockDefinition> All() => _definitions.Values;

    public BlockDefinition? Get(BlockDefinitionId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        return _definitions.TryGetValue(id, out var definition) ? definition : null;
    }
}

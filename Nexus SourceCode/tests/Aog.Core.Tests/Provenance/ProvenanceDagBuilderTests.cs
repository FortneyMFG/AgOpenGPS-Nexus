using System.Collections.Generic;
using Aog.Core.Provenance;
using FluentAssertions;
using Xunit;

namespace Aog.Core.Tests.Provenance;

public sealed class ProvenanceDagBuilderTests
{
    [Fact]
    public void Build_WithValidGraph_ReturnsDag()
    {
        var builder = new ProvenanceDagBuilder();
        builder.AddOrUpdateNode("import", "Import", new Dictionary<string, string> { ["hash"] = "abc" });
        builder.AddOrUpdateNode("normalize", "Normalize", null);
        builder.AddOrUpdateNode("publish", "Publish", null);

        builder.AddEdge("import", "normalize", "transform");
        builder.AddEdge("normalize", "publish", "export");

        var dag = builder.Build();

        dag.Nodes.Should().HaveCount(3);
        dag.Edges.Should().HaveCount(2);
        dag.GetRootNodeIds().Should().ContainSingle().Which.Should().Be("import");
    }

    [Fact]
    public void Build_WithCycle_ThrowsValidationException()
    {
        var builder = new ProvenanceDagBuilder();
        builder.AddOrUpdateNode("a", "A");
        builder.AddOrUpdateNode("b", "B");

        builder.AddEdge("a", "b", "step");
        builder.AddEdge("b", "a", "cycle");

        var act = () => builder.Build();

        act.Should().Throw<ProvenanceDagValidationException>()
            .Which.Errors.Should().Contain(error => error.Contains("cycles"));
    }

    [Fact]
    public void AddEdge_WhenNodeMissing_Throws()
    {
        var builder = new ProvenanceDagBuilder();
        builder.AddOrUpdateNode("existing", "Existing");

        var act = () => builder.AddEdge("existing", "missing", "link");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*unknown target node 'missing'*");
    }
}

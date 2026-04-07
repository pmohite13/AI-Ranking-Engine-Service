using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.UnitTests.Phase0;

public sealed class DomainAssemblyTests
{
    [Fact]
    public void Domain_assembly_resolves_and_has_expected_name()
    {
        var assembly = typeof(AssemblyMarker).Assembly;

        Assert.Equal("AI.Ranking.Engine.Domain", assembly.GetName().Name);
    }
}

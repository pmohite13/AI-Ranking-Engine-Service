namespace AI.Ranking.Engine.UnitTests.Phase8;

/// <summary>
/// Ensures Phase 8 documentation artifacts remain present and minimally consistent with the implementation plan.
/// Tests resolve the repository root by locating <c>AI.Ranking.Engine.sln</c> (runs from any test working directory).
/// </summary>
public sealed class RepositoryDocumentationContractTests
{
    private static readonly string[] RequiredAdrFiles =
    [
        "0001-record-architecture-decisions.md",
        "0002-hybrid-ranking-baseline.md",
        "0003-openai-embeddings-sync-in-process-batching.md",
        "0004-pure-dotnet-vector-recall.md",
        "0005-hash-keyed-memory-caching.md",
        "0006-llm-structured-extraction-with-heuristic-fallback.md",
        "0007-fluentvalidation-for-api-commands.md",
        "0008-offline-ranking-evaluation-deferred.md",
    ];

    [Fact]
    public void Repository_root_contains_design_md_with_failure_modes_section()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);

        var designPath = Path.Combine(root, "docs", "DESIGN.md");
        Assert.True(File.Exists(designPath), $"Expected {designPath}.");

        var text = File.ReadAllText(designPath);
        Assert.Contains("Known failure modes", text, StringComparison.Ordinal);
        Assert.Contains("```mermaid", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Repository_root_contains_expected_architecture_decision_records()
    {
        var root = FindRepositoryRoot();
        Assert.NotNull(root);

        var adrDir = Path.Combine(root, "docs", "adr");
        Assert.True(Directory.Exists(adrDir), $"Expected directory {adrDir}.");

        foreach (var file in RequiredAdrFiles)
        {
            var path = Path.Combine(adrDir, file);
            Assert.True(File.Exists(path), $"Missing ADR file: {path}");
        }
    }

    private static string? FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var sln = Path.Combine(dir.FullName, "AI.Ranking.Engine.sln");
            var design = Path.Combine(dir.FullName, "docs", "DESIGN.md");
            if (File.Exists(sln) && File.Exists(design))
                return dir.FullName;

            dir = dir.Parent;
        }

        return null;
    }
}

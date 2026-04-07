using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Infrastructure.Ingestion;

namespace AI.Ranking.Engine.UnitTests.Phase7;

public sealed class IngestionIdempotencyStoreTests
{
    [Fact]
    public void Complete_removes_entry_so_same_key_can_start_again()
    {
        var store = new InMemoryIngestionIdempotencyStore();
        const string key = "Candidate:1:ABCD:model";

        Assert.True(store.TryStart(key, out _));

        store.Complete(
            key,
            new IngestionProcessResult(
                EntityId: "1",
                EntityKind: IngestionEntityKind.Candidate,
                FileName: "a.pdf",
                ContentType: "Pdf",
                EmbeddingDimensions: 2,
                SkillCount: 1,
                Deduplicated: false));

        Assert.True(store.TryStart(key, out _));
    }

    [Fact]
    public async Task Concurrent_waiters_receive_same_result()
    {
        var store = new InMemoryIngestionIdempotencyStore();
        const string key = "Job:9:FFFF:model";

        Assert.True(store.TryStart(key, out var existingA));
        Assert.Null(existingA);

        Assert.False(store.TryStart(key, out var existingB));
        Assert.NotNull(existingB);

        var expected = new IngestionProcessResult(
            EntityId: "9",
            EntityKind: IngestionEntityKind.Job,
            FileName: "j.pdf",
            ContentType: "Pdf",
            EmbeddingDimensions: 3,
            SkillCount: 2,
            Deduplicated: false);

        store.Complete(key, expected);

        var b = await existingB!;
        Assert.Equal(expected.EntityId, b.EntityId);
        Assert.Equal(expected.EmbeddingDimensions, b.EmbeddingDimensions);
    }
}

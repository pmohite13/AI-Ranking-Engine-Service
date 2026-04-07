using AI.Ranking.Engine.Infrastructure.VectorRecall;

namespace AI.Ranking.Engine.UnitTests.Phase4;

public sealed class InMemoryVectorRecallTests
{
    [Fact]
    public async Task SearchTopK_empty_corpus_returns_empty()
    {
        var recall = new InMemoryVectorRecall();
        var hits = await recall.SearchTopKAsync(new float[] { 1f, 0f, 0f }, k: 5);
        Assert.Empty(hits);
    }

    [Fact]
    public async Task SearchTopK_k_zero_returns_empty()
    {
        var recall = new InMemoryVectorRecall();
        await recall.UpsertCandidateAsync("a", new float[] { 1f, 0f, 0f });
        var hits = await recall.SearchTopKAsync(new float[] { 1f, 0f, 0f }, k: 0);
        Assert.Empty(hits);
    }

    [Fact]
    public async Task SearchTopK_orders_by_similarity_then_id()
    {
        var recall = new InMemoryVectorRecall();
        await recall.UpsertCandidateAsync("low", new float[] { 0f, 1f, 0f });
        await recall.UpsertCandidateAsync("high", new float[] { 1f, 0f, 0f });
        await recall.UpsertCandidateAsync("mid", new float[] { 1f, 1f, 0f });

        var hits = await recall.SearchTopKAsync(new float[] { 1f, 0f, 0f }, k: 10);

        Assert.Equal(3, hits.Count);
        Assert.Equal("high", hits[0].CandidateId);
        Assert.True(hits[0].Similarity > hits[1].Similarity);
        Assert.True(hits[1].Similarity > hits[2].Similarity);
    }

    [Fact]
    public async Task SearchTopK_tie_breaks_by_candidate_id_ascending()
    {
        var recall = new InMemoryVectorRecall();
        await recall.UpsertCandidateAsync("b", new float[] { 1f, 0f, 0f });
        await recall.UpsertCandidateAsync("a", new float[] { 1f, 0f, 0f });

        var hits = await recall.SearchTopKAsync(new float[] { 1f, 0f, 0f }, k: 2);

        Assert.Equal(2, hits.Count);
        Assert.Equal("a", hits[0].CandidateId);
        Assert.Equal("b", hits[1].CandidateId);
        Assert.Equal(hits[0].Similarity, hits[1].Similarity);
    }

    [Fact]
    public async Task Upsert_replaces_vector()
    {
        var recall = new InMemoryVectorRecall();
        await recall.UpsertCandidateAsync("x", new float[] { 0f, 1f, 0f });
        await recall.UpsertCandidateAsync("x", new float[] { 1f, 0f, 0f });

        var hits = await recall.SearchTopKAsync(new float[] { 1f, 0f, 0f }, k: 1);
        Assert.Single(hits);
        Assert.Equal("x", hits[0].CandidateId);
        Assert.InRange(hits[0].Similarity, 0.99f, 1.01f);
    }

    [Fact]
    public async Task Upsert_dimension_mismatch_throws()
    {
        var recall = new InMemoryVectorRecall();
        await recall.UpsertCandidateAsync("a", new float[] { 1f, 0f, 0f });

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await recall.UpsertCandidateAsync("b", new float[] { 1f, 0f }));
    }

    [Fact]
    public async Task SearchTopK_query_dimension_mismatch_throws()
    {
        var recall = new InMemoryVectorRecall();
        await recall.UpsertCandidateAsync("a", new float[] { 1f, 0f, 0f });

        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await recall.SearchTopKAsync(new float[] { 1f, 0f }, k: 1));
    }
}

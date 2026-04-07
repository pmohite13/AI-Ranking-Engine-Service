using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Application.Validation;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.UnitTests.Phase1;

public sealed class RankRequestValidatorTests
{
    private static RankRequestValidator CreateValidator(int maxK = 500, int maxTop = 100)
    {
        var options = Options.Create(new RankingConstraintsOptions
        {
            MaxVectorRecallTopK = maxK,
            MaxFinalTopN = maxTop,
        });
        return new RankRequestValidator(options);
    }

    [Fact]
    public void Valid_request_passes()
    {
        var v = CreateValidator();
        var request = new RankRequest("job-1", VectorRecallTopK: 50, FinalTopN: 10);
        var result = v.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void FinalTopN_greater_than_recall_fails()
    {
        var v = CreateValidator();
        var request = new RankRequest("job-1", VectorRecallTopK: 10, FinalTopN: 20);
        var result = v.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Empty_job_id_fails()
    {
        var v = CreateValidator();
        var request = new RankRequest("", 50, 10);
        var result = v.Validate(request);
        Assert.False(result.IsValid);
    }
}

using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Application.Options;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.Application.Validation;

public sealed class RankRequestValidator : AbstractValidator<RankRequest>
{
    public RankRequestValidator(IOptions<RankingConstraintsOptions> rankingOptions)
    {
        ArgumentNullException.ThrowIfNull(rankingOptions);

        var limits = rankingOptions.Value;

        RuleFor(x => x.JobId)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.VectorRecallTopK)
            .InclusiveBetween(1, limits.MaxVectorRecallTopK);

        RuleFor(x => x.FinalTopN)
            .InclusiveBetween(1, limits.MaxFinalTopN);

        RuleFor(x => x)
            .Must(x => x.FinalTopN <= x.VectorRecallTopK)
            .WithMessage("FinalTopN must be less than or equal to VectorRecallTopK (two-stage recall then re-rank).");
    }
}

using AI.Ranking.Engine.Domain;
using AI.Ranking.Engine.Domain.Exceptions;

namespace AI.Ranking.Engine.Domain.Scoring;

/// <summary>
/// Pure hybrid scoring: semantic similarity plus deterministic overlaps. Hot paths avoid LINQ allocations.
/// </summary>
public static class HybridRankingMath
{
    /// <param name="semanticSimilarity">Cosine or related similarity in [0, 1] (clamped).</param>
    public static RankingScore Compute(
        StructuredFeatures candidate,
        StructuredFeatures job,
        double semanticSimilarity,
        RankingWeights weights)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(weights);

        weights.EnsureValid();

        var sem = Clamp01(semanticSimilarity);
        var skill = SkillOverlap(candidate.Skills, job.Skills);
        var exp = ExperienceFit(candidate.YearsExperience, job.MinimumYears, job.MaximumYears);
        var key = KeywordOverlap(candidate.NormalizedRoleTitle, job.NormalizedRoleTitle);

        var ws = sem * weights.Semantic;
        var wk = skill * weights.SkillOverlap;
        var we = exp * weights.ExperienceFit;
        var wn = key * weights.Keyword;
        var total = ws + wk + we + wn;

        var breakdown = new ScoreBreakdown(
            SemanticRaw: sem,
            SkillOverlapRaw: skill,
            ExperienceFitRaw: exp,
            KeywordRaw: key,
            WeightedSemantic: ws,
            WeightedSkillOverlap: wk,
            WeightedExperienceFit: we,
            WeightedKeyword: wn,
            TotalScore: total);

        return new RankingScore(TotalScore: total, Breakdown: breakdown);
    }

    /// <summary>Jaccard similarity on distinct skill tokens.</summary>
    public static double SkillOverlap(IReadOnlyList<string> candidateSkills, IReadOnlyList<string> jobSkills)
    {
        if (candidateSkills.Count == 0 && jobSkills.Count == 0)
            return 1.0;

        // Small-N sets: two HashSets are acceptable; for very large lists use sorting + merge (future ADR).
        var a = ToSet(candidateSkills);
        var b = ToSet(jobSkills);
        if (a.Count == 0 && b.Count == 0)
            return 1.0;

        var intersection = 0;
        foreach (var x in a)
        {
            if (b.Contains(x))
                intersection++;
        }

        var union = a.Count + b.Count - intersection;
        return union == 0 ? 0.0 : intersection / (double)union;
    }

    /// <summary>
    /// Maps candidate experience to job requirements. Full score when within [min, max] when specified.
    /// </summary>
    public static double ExperienceFit(double candidateYears, double? jobMin, double? jobMax)
    {
        if (double.IsNaN(candidateYears) || double.IsInfinity(candidateYears))
            throw new DomainException("Candidate years must be a finite number.");

        if (jobMin is null && jobMax is null)
            return 1.0;

        var min = jobMin ?? 0.0;
        var max = jobMax ?? double.PositiveInfinity;

        if (min > max)
            throw new DomainException("Job minimum years cannot exceed maximum years.");

        if (candidateYears < min)
        {
            if (min <= 0)
                return 1.0;

            return Clamp01(candidateYears / min);
        }

        if (candidateYears > max && !double.IsPositiveInfinity(max))
        {
            if (max <= 0)
                return 0.0;

            // Softer penalty for over-experience than under-experience.
            return Clamp01(max / candidateYears);
        }

        return 1.0;
    }

    /// <summary>Token overlap on role titles (simple word split, culture-invariant).</summary>
    public static double KeywordOverlap(string? candidateTitle, string? jobTitle)
    {
        if (string.IsNullOrWhiteSpace(candidateTitle) && string.IsNullOrWhiteSpace(jobTitle))
            return 1.0;

        if (string.IsNullOrWhiteSpace(candidateTitle) || string.IsNullOrWhiteSpace(jobTitle))
            return 0.0;

        // Allocation-free path could use Span split later; keep straightforward for Phase 1.
        var a = ToWordSet(candidateTitle);
        var b = ToWordSet(jobTitle);
        if (a.Count == 0 && b.Count == 0)
            return 1.0;

        var intersection = 0;
        foreach (var x in a)
        {
            if (b.Contains(x))
                intersection++;
        }

        var union = a.Count + b.Count - intersection;
        return union == 0 ? 0.0 : intersection / (double)union;
    }

    private static HashSet<string> ToSet(IReadOnlyList<string> skills)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < skills.Count; i++)
        {
            var s = skills[i];
            if (!string.IsNullOrWhiteSpace(s))
                set.Add(s.Trim());
        }

        return set;
    }

    private static HashSet<string> ToWordSet(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var word in text.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var w = word.Trim();
            if (w.Length > 0)
                set.Add(w.ToLowerInvariant());
        }

        return set;
    }

    private static readonly char[] Separators = { ' ', '\t', '\r', '\n', ',', ';', '.', '/', '|', '-', '_' };

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0.0;

        if (value < 0)
            return 0.0;

        if (value > 1)
            return 1.0;

        return value;
    }
}

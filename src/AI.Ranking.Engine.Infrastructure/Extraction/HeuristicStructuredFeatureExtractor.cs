using System.Text.RegularExpressions;
using AI.Ranking.Engine.Domain;

namespace AI.Ranking.Engine.Infrastructure.Extraction;

public sealed partial class HeuristicStructuredFeatureExtractor
{
    private static readonly HashSet<string> KnownSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        "c#", "dotnet", ".net", "asp.net", "java", "python", "javascript", "typescript", "sql",
        "postgresql", "mysql", "azure", "aws", "docker", "kubernetes", "react", "angular",
        "node.js", "redis", "microservices", "rest", "graphql", "git", "ci/cd",
    };

    public StructuredFeatures Extract(string normalizedText, ExtractionDocumentKind kind)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
            return StructuredFeatures.Empty;

        var skills = ExtractSkills(normalizedText);
        var roleTitle = ExtractRoleTitle(normalizedText, kind);
        var years = ExtractYears(normalizedText, kind);
        var range = ExtractExperienceRange(normalizedText, kind);

        return new StructuredFeatures(
            Skills: skills,
            YearsExperience: years,
            NormalizedRoleTitle: roleTitle,
            MinimumYears: range.Min,
            MaximumYears: range.Max);
    }

    private static IReadOnlyList<string> ExtractSkills(string text)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in SkillTokenRegex().Matches(text))
        {
            var token = NormalizeSkillToken(m.Value);
            if (token.Length == 0)
                continue;

            if (KnownSkills.Contains(token))
                set.Add(token);
        }

        return set.Count == 0 ? Array.Empty<string>() : set.ToArray();
    }

    private static string? ExtractRoleTitle(string text, ExtractionDocumentKind kind)
    {
        var regex = kind == ExtractionDocumentKind.Resume ? ResumeRoleRegex() : JobRoleRegex();
        var m = regex.Match(text);
        if (!m.Success)
            return null;

        var title = m.Groups["title"].Value.Trim();
        return title.Length == 0 ? null : title.ToLowerInvariant();
    }

    private static double ExtractYears(string text, ExtractionDocumentKind kind)
    {
        var range = ExtractExperienceRange(text, kind);
        if (range.Min is not null)
            return range.Min.Value;

        return 0;
    }

    private static (double? Min, double? Max) ExtractExperienceRange(string text, ExtractionDocumentKind kind)
    {
        var regex = kind == ExtractionDocumentKind.Resume ? ResumeYearsRegex() : JobYearsRegex();
        var m = regex.Match(text);
        if (!m.Success)
            return (null, null);

        var minRaw = m.Groups["min"].Value;
        var maxRaw = m.Groups["max"].Value;

        if (!double.TryParse(minRaw, out var min))
            return (null, null);

        if (double.TryParse(maxRaw, out var max))
            return (ClampYears(min), ClampYears(max));

        return (ClampYears(min), null);
    }

    private static string NormalizeSkillToken(string token)
    {
        token = token.Trim().ToLowerInvariant();
        return token switch
        {
            "c sharp" => "c#",
            ".net core" => ".net",
            "dot net" => "dotnet",
            _ => token,
        };
    }

    private static double ClampYears(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0;

        if (value < 0)
            return 0;

        if (value > 60)
            return 60;

        return value;
    }

    [GeneratedRegex(@"\b[a-zA-Z\.\+#/]{2,20}\b", RegexOptions.Compiled)]
    private static partial Regex SkillTokenRegex();

    [GeneratedRegex(@"(?:current|recent|latest)\s+(?:role|title)\s*[:\-]\s*(?<title>[^\r\n,;]{2,80})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ResumeRoleRegex();

    [GeneratedRegex(@"(?:job\s+title|role|position)\s*[:\-]\s*(?<title>[^\r\n,;]{2,80})", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex JobRoleRegex();

    [GeneratedRegex(@"(?<min>\d{1,2})(?:\+|\s*(?:years?|yrs?)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ResumeYearsRegex();

    [GeneratedRegex(@"(?<min>\d{1,2})\s*(?:-|to)\s*(?<max>\d{1,2})\s*(?:years?|yrs?)|(?<min>\d{1,2})\+?\s*(?:years?|yrs?)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex JobYearsRegex();
}

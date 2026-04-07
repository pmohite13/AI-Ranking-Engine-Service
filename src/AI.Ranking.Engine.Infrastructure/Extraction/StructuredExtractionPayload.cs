namespace AI.Ranking.Engine.Infrastructure.Extraction;

public sealed record StructuredExtractionPayload(
    List<string>? Skills,
    double? YearsExperience,
    string? RoleTitle,
    double? MinimumYears,
    double? MaximumYears);

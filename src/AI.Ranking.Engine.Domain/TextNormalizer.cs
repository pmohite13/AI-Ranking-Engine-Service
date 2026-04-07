using System.Text;

namespace AI.Ranking.Engine.Domain;

/// <summary>
/// Shared normalization for parsed document text: Unicode NFKC, line endings, and repeated whitespace collapse.
/// Used by all format parsers so downstream embedding and extraction see consistent canonical text.
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Applies NFKC normalization, converts line breaks to LF, collapses horizontal whitespace runs to a single space,
    /// and limits consecutive newlines to at most two (paragraph boundary hint without unbounded blank runs).
    /// </summary>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        var normalized = trimmed.Normalize(NormalizationForm.FormKC);
        normalized = normalized.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

        var sb = new StringBuilder(normalized.Length);
        var newlineRun = 0;
        var pendingSpace = false;

        for (var i = 0; i < normalized.Length; i++)
        {
            var c = normalized[i];

            if (c == '\n')
            {
                if (pendingSpace)
                {
                    pendingSpace = false;
                }

                newlineRun++;
                if (newlineRun <= 2)
                {
                    sb.Append('\n');
                }

                continue;
            }

            newlineRun = 0;

            if (char.IsWhiteSpace(c))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && sb.Length > 0)
            {
                sb.Append(' ');
                pendingSpace = false;
            }

            sb.Append(c);
        }

        return sb.ToString().Trim();
    }
}

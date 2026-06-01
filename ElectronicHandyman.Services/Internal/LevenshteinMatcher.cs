namespace Services.Internal;

internal class LevenshteinMatcher
{
    private static readonly HashSet<(char, char)> ConfusionPairs = new()
    {
        // Original pairs from spec
        ('6', 'B'), ('8', 'B'), ('0', 'O'), ('1', 'L'), ('5', 'S'), ('2', 'Z'),
        // Additional OCR confusion pairs observed on chip markings
        ('0', 'D'), ('0', 'Q'), ('1', 'I'), ('1', 'T'),
        ('2', 'R'), ('3', 'B'), ('6', 'G'), ('8', '0'),
        ('M', 'H'), ('N', 'H'), ('C', 'G'), ('6', '0'),
    };

    private const double ConfusionSubstitutionCost = 0.5;
    private const double StandardSubstitutionCost = 1.0;
    private const double InsertionCost = 1.0;
    private const double DeletionCost = 1.0;

    /// <summary>
    /// Returns the substitution cost for a character pair.
    /// Returns 0 if characters are equal, 0.5 if they form an OCR confusion pair, 1.0 otherwise.
    /// </summary>
    internal double GetSubstitutionCost(char a, char b)
    {
        if (a == b)
            return 0;

        if (ConfusionPairs.Contains((a, b)) || ConfusionPairs.Contains((b, a)))
            return ConfusionSubstitutionCost;

        return StandardSubstitutionCost;
    }

    /// <summary>
    /// Computes weighted Levenshtein distance between two strings.
    /// OCR confusion pairs have reduced substitution cost (0.5 instead of 1.0).
    /// Uses the Wagner-Fischer dynamic programming algorithm.
    /// </summary>
    public double ComputeDistance(string source, string target)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));

        // Normalize both to uppercase for case-insensitive comparison
        source = source.ToUpperInvariant();
        target = target.ToUpperInvariant();

        var sourceLength = source.Length;
        var targetLength = target.Length;

        if (sourceLength == 0) return targetLength * InsertionCost;
        if (targetLength == 0) return sourceLength * DeletionCost;

        var dp = new double[sourceLength + 1, targetLength + 1];

        // Initialize first column (deletions from source)
        for (var i = 0; i <= sourceLength; i++)
            dp[i, 0] = i * DeletionCost;

        // Initialize first row (insertions into source)
        for (var j = 0; j <= targetLength; j++)
            dp[0, j] = j * InsertionCost;

        // Fill the matrix
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var substitutionCost = GetSubstitutionCost(source[i - 1], target[j - 1]);

                dp[i, j] = Math.Min(
                    Math.Min(
                        dp[i - 1, j] + DeletionCost,       // deletion
                        dp[i, j - 1] + InsertionCost),     // insertion
                    dp[i - 1, j - 1] + substitutionCost);  // substitution
            }
        }

        return dp[sourceLength, targetLength];
    }

    /// <summary>
    /// Finds the best match from a list of candidates.
    /// Returns null if no candidate is within the threshold.
    /// Best match selection: minimum distance, then shortest name on tie.
    /// </summary>
    public MatchResult? FindBestMatch(string ocrText, IEnumerable<string> candidates, double threshold = 6.0)
    {
        if (ocrText is null) throw new ArgumentNullException(nameof(ocrText));
        if (candidates is null) throw new ArgumentNullException(nameof(candidates));

        string? bestName = null;
        var bestDistance = double.MaxValue;

        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate))
                continue;

            var distance = ComputeDistance(ocrText, candidate);

            if (distance < bestDistance ||
                (distance == bestDistance && bestName != null && candidate.Length < bestName.Length))
            {
                bestDistance = distance;
                bestName = candidate;
            }
        }

        if (bestName is null || bestDistance > threshold)
            return null;

        return new MatchResult(bestName, bestDistance);
    }
}

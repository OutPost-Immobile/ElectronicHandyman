using ElectronicHandyman.Services.Tests.OcrValidation.Models;

namespace ElectronicHandyman.Services.Tests.OcrValidation;

/// <summary>
/// Pure computation of all validation metrics from OCR results.
/// </summary>
public static class MetricsCalculator
{
    /// <summary>
    /// Computes all metrics from validation results.
    /// Returns zeroed metrics for empty input.
    /// </summary>
    public static ValidationMetrics Calculate(ValidationResult result)
    {
        var results = result.Results;

        if (results.Count == 0)
        {
            return new ValidationMetrics(
                Classification: new ClassificationMetrics(0, 0, 0, 0.0, 0.0, 0.0),
                CharAccuracy: new CharAccuracyMetrics(0.0, 0.0, 0.0),
                Performance: new PerformanceMetrics(0.0, 0.0, 0.0, 0.0, Array.Empty<string>()),
                Confidence: new ConfidenceMetrics(0.0, Array.Empty<string>()),
                Errors: Array.Empty<ErrorDetail>(),
                MetricsByCategory: new Dictionary<string, ClassificationMetrics>()
            );
        }

        // Classification metrics
        var tp = results.Count(r => r.Classification == Models.Classification.TruePositive);
        var fp = results.Count(r => r.Classification == Models.Classification.FalsePositive);
        var fn = results.Count(r => r.Classification == Models.Classification.FalseNegative);

        var precision = ComputePrecision(tp, fp);
        var recall = ComputeRecall(tp, fn);
        var f1 = ComputeF1(precision, recall);

        var classificationMetrics = new ClassificationMetrics(tp, fp, fn, precision, recall, f1);

        // Character accuracy metrics
        var accuracies = results.Select(r => r.CharAccuracy).ToList();
        var charAccuracy = new CharAccuracyMetrics(
            MeanAccuracy: accuracies.Average(),
            MinAccuracy: accuracies.Min(),
            MaxAccuracy: accuracies.Max()
        );

        // Performance metrics
        var times = results.Select(r => (double)r.ProcessingTimeMs).ToList();
        var avgTime = times.Average();
        var minTime = times.Min();
        var maxTime = times.Max();
        var fps = avgTime > 0 ? 1000.0 / avgTime : 0.0;
        var bottlenecks = results
            .Where(r => r.ProcessingTimeMs > 2 * avgTime)
            .Select(r => r.FileName)
            .ToList();

        var performanceMetrics = new PerformanceMetrics(avgTime, minTime, maxTime, fps, bottlenecks);

        // Confidence metrics
        var confidences = results.Select(r => (double)r.Confidence).ToList();
        var meanConfidence = confidences.Average();
        var lowConfidenceFiles = results
            .Where(r => r.Confidence < 0.6f)
            .Select(r => r.FileName)
            .ToList();

        var confidenceMetrics = new ConfidenceMetrics(meanConfidence, lowConfidenceFiles);

        // Error details (FP and FN only)
        var errors = results
            .Where(r => r.Classification != Models.Classification.TruePositive)
            .Select(r => new ErrorDetail(
                FileName: r.FileName,
                Expected: r.Expected,
                Actual: r.Actual,
                Confidence: r.Confidence,
                Type: CategorizeError(r.Expected, r.Actual)
            ))
            .ToList();

        // Per-category breakdown
        var metricsByCategory = new Dictionary<string, ClassificationMetrics>();
        var categories = results.Select(r => r.Category).Distinct();

        foreach (var category in categories)
        {
            var categoryResults = results.Where(r => r.Category == category).ToList();
            var catTp = categoryResults.Count(r => r.Classification == Models.Classification.TruePositive);
            var catFp = categoryResults.Count(r => r.Classification == Models.Classification.FalsePositive);
            var catFn = categoryResults.Count(r => r.Classification == Models.Classification.FalseNegative);

            var catPrecision = ComputePrecision(catTp, catFp);
            var catRecall = ComputeRecall(catTp, catFn);
            var catF1 = ComputeF1(catPrecision, catRecall);

            metricsByCategory[category] = new ClassificationMetrics(catTp, catFp, catFn, catPrecision, catRecall, catF1);
        }

        return new ValidationMetrics(
            Classification: classificationMetrics,
            CharAccuracy: charAccuracy,
            Performance: performanceMetrics,
            Confidence: confidenceMetrics,
            Errors: errors,
            MetricsByCategory: metricsByCategory
        );
    }

    /// <summary>
    /// Computes character accuracy: 1 - (levenshtein / max(len(expected), len(actual))).
    /// Returns 0.0 if actual is empty and expected is non-empty.
    /// Returns 1.0 if both are empty.
    /// </summary>
    public static double ComputeCharAccuracy(string expected, string actual)
    {
        if (string.IsNullOrEmpty(expected) && string.IsNullOrEmpty(actual))
            return 1.0;

        if (string.IsNullOrEmpty(actual))
            return 0.0;

        if (string.IsNullOrEmpty(expected))
            return 0.0;

        var distance = ComputeLevenshteinDistance(expected, actual);
        var maxLen = Math.Max(expected.Length, actual.Length);

        return 1.0 - ((double)distance / maxLen);
    }

    /// <summary>
    /// Classifies a single result as TP, FP, or FN.
    /// Uses case-insensitive normalized comparison.
    /// </summary>
    public static Classification Classify(string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(actual))
            return Models.Classification.FalseNegative;

        var normalizedExpected = Normalize(expected);
        var normalizedActual = Normalize(actual);

        if (string.Equals(normalizedExpected, normalizedActual, StringComparison.OrdinalIgnoreCase))
            return Models.Classification.TruePositive;

        return Models.Classification.FalsePositive;
    }

    /// <summary>
    /// Categorizes an error by type based on string similarity patterns.
    /// </summary>
    public static ErrorType CategorizeError(string expected, string actual)
    {
        if (string.IsNullOrWhiteSpace(actual))
            return ErrorType.NoResult;

        var distance = ComputeLevenshteinDistance(expected, actual);

        // CharacterSwap: Levenshtein distance ≤ 2
        if (distance <= 2)
            return ErrorType.CharacterSwap;

        // Truncation: actual is a prefix or suffix of expected (or vice versa)
        var expectedLower = expected.ToLowerInvariant();
        var actualLower = actual.ToLowerInvariant();

        if (expectedLower.StartsWith(actualLower) || expectedLower.EndsWith(actualLower) ||
            actualLower.StartsWith(expectedLower) || actualLower.EndsWith(expectedLower))
            return ErrorType.Truncation;

        // CompletelyWrong: Levenshtein distance > 50% of expected length
        if (expected.Length > 0 && distance > expected.Length * 0.5)
            return ErrorType.CompletelyWrong;

        // Default fallback for cases where distance is between 3 and 50% of expected length
        // but not a prefix/suffix — treat as CharacterSwap (multiple swaps)
        return ErrorType.CharacterSwap;
    }

    /// <summary>
    /// Computes Precision from counts. Returns 0.0 if TP+FP == 0.
    /// </summary>
    public static double ComputePrecision(int tp, int fp)
    {
        var denominator = tp + fp;
        return denominator == 0 ? 0.0 : (double)tp / denominator;
    }

    /// <summary>
    /// Computes Recall from counts. Returns 0.0 if TP+FN == 0.
    /// </summary>
    public static double ComputeRecall(int tp, int fn)
    {
        var denominator = tp + fn;
        return denominator == 0 ? 0.0 : (double)tp / denominator;
    }

    /// <summary>
    /// Computes F1 as harmonic mean. Returns 0.0 if P+R == 0.
    /// </summary>
    public static double ComputeF1(double precision, double recall)
    {
        var sum = precision + recall;
        return sum == 0.0 ? 0.0 : 2.0 * precision * recall / sum;
    }

    /// <summary>
    /// Computes standard Levenshtein distance between two strings using iterative DP.
    /// </summary>
    internal static int ComputeLevenshteinDistance(string source, string target)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (target is null) throw new ArgumentNullException(nameof(target));

        var sourceLength = source.Length;
        var targetLength = target.Length;

        if (sourceLength == 0) return targetLength;
        if (targetLength == 0) return sourceLength;

        // Use single-row optimization for memory efficiency
        var previousRow = new int[targetLength + 1];
        var currentRow = new int[targetLength + 1];

        // Initialize first row
        for (var j = 0; j <= targetLength; j++)
            previousRow[j] = j;

        for (var i = 1; i <= sourceLength; i++)
        {
            currentRow[0] = i;

            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                currentRow[j] = Math.Min(
                    Math.Min(
                        currentRow[j - 1] + 1,      // insertion
                        previousRow[j] + 1),        // deletion
                    previousRow[j - 1] + cost);     // substitution
            }

            // Swap rows
            (previousRow, currentRow) = (currentRow, previousRow);
        }

        return previousRow[targetLength];
    }

    /// <summary>
    /// Normalizes a string for comparison: trims whitespace, removes non-alphanumeric chars except hyphen.
    /// </summary>
    private static string Normalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove whitespace, convert to uppercase, strip invalid characters
        var noWhitespace = string.Concat(input.Where(c => !char.IsWhiteSpace(c)));
        var uppercased = noWhitespace.ToUpperInvariant();
        // Keep only A-Z, 0-9, and hyphen
        var normalized = string.Concat(uppercased.Where(c => char.IsLetterOrDigit(c) || c == '-'));
        return normalized;
    }
}

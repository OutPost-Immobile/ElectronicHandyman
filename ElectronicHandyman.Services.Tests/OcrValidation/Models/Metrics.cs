namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Classification metrics computed from confusion matrix counts.
/// </summary>
/// <param name="TruePositives">Number of correct identifications.</param>
/// <param name="FalsePositives">Number of incorrect identifications.</param>
/// <param name="FalseNegatives">Number of missed identifications.</param>
/// <param name="Precision">TP / (TP + FP), or 0.0 if denominator is zero.</param>
/// <param name="Recall">TP / (TP + FN), or 0.0 if denominator is zero.</param>
/// <param name="F1Score">Harmonic mean of Precision and Recall.</param>
public record ClassificationMetrics(
    int TruePositives,
    int FalsePositives,
    int FalseNegatives,
    double Precision,
    double Recall,
    double F1Score
);

/// <summary>
/// Performance timing metrics across the test set.
/// </summary>
/// <param name="AverageTimeMs">Mean processing time in milliseconds.</param>
/// <param name="MinTimeMs">Minimum processing time.</param>
/// <param name="MaxTimeMs">Maximum processing time.</param>
/// <param name="Fps">Equivalent frames per second (1000 / AverageTimeMs).</param>
/// <param name="Bottlenecks">Files with processing time exceeding 2x average.</param>
public record PerformanceMetrics(
    double AverageTimeMs,
    double MinTimeMs,
    double MaxTimeMs,
    double Fps,
    IReadOnlyList<string> Bottlenecks
);

/// <summary>
/// Character-level accuracy metrics based on Levenshtein distance.
/// </summary>
/// <param name="MeanAccuracy">Mean character accuracy across all images.</param>
/// <param name="MinAccuracy">Minimum character accuracy observed.</param>
/// <param name="MaxAccuracy">Maximum character accuracy observed.</param>
public record CharAccuracyMetrics(
    double MeanAccuracy,
    double MinAccuracy,
    double MaxAccuracy
);

/// <summary>
/// Confidence score metrics from the OCR engine.
/// </summary>
/// <param name="MeanConfidence">Arithmetic mean of all confidence scores.</param>
/// <param name="LowConfidenceFiles">Files with confidence below 0.6 threshold.</param>
public record ConfidenceMetrics(
    double MeanConfidence,
    IReadOnlyList<string> LowConfidenceFiles
);

/// <summary>
/// Aggregated validation metrics combining all metric categories.
/// </summary>
/// <param name="Classification">Classification metrics (Precision, Recall, F1).</param>
/// <param name="CharAccuracy">Character-level accuracy metrics.</param>
/// <param name="Performance">Processing time and throughput metrics.</param>
/// <param name="Confidence">Confidence score metrics.</param>
/// <param name="Errors">Detailed error records for FP and FN results.</param>
/// <param name="MetricsByCategory">Per-category classification metrics breakdown.</param>
public record ValidationMetrics(
    ClassificationMetrics Classification,
    CharAccuracyMetrics CharAccuracy,
    PerformanceMetrics Performance,
    ConfidenceMetrics Confidence,
    IReadOnlyList<ErrorDetail> Errors,
    IDictionary<string, ClassificationMetrics> MetricsByCategory
);

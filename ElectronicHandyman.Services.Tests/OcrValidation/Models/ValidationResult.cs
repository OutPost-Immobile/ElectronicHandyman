namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Aggregated result of a full validation run across all test images.
/// </summary>
/// <param name="Results">Per-image results.</param>
/// <param name="SkippedFiles">Files that were skipped (missing images).</param>
/// <param name="PeakMemoryBytes">Peak memory usage during the run.</param>
/// <param name="RunTimestamp">Timestamp when the validation was executed.</param>
public record ValidationResult(
    IReadOnlyList<ImageResult> Results,
    IReadOnlyList<string> SkippedFiles,
    long PeakMemoryBytes,
    DateTime RunTimestamp
);

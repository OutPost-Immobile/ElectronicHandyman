namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Result of processing a single test image through the OCR pipeline.
/// </summary>
/// <param name="FileName">Image filename.</param>
/// <param name="Expected">Expected chip name from ground truth.</param>
/// <param name="Actual">Actual OCR output.</param>
/// <param name="Confidence">Confidence score from the OCR engine.</param>
/// <param name="CharAccuracy">Character-level accuracy (Levenshtein-based).</param>
/// <param name="Classification">TP, FP, or FN classification.</param>
/// <param name="ProcessingTimeMs">Processing time in milliseconds.</param>
/// <param name="Category">Difficulty category of the test image.</param>
public record ImageResult(
    string FileName,
    string Expected,
    string Actual,
    float Confidence,
    double CharAccuracy,
    Classification Classification,
    long ProcessingTimeMs,
    string Category
);

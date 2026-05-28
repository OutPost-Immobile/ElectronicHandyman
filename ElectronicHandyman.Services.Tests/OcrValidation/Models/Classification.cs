namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Classification of an OCR result against ground truth.
/// </summary>
public enum Classification
{
    /// <summary>Recognized name matches expected (case-insensitive, normalized).</summary>
    TruePositive,

    /// <summary>Recognized name is non-empty but does not match expected.</summary>
    FalsePositive,

    /// <summary>No result returned (empty actual) when chip is present.</summary>
    FalseNegative
}

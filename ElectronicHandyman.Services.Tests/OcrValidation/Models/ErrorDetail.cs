namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Type of OCR error based on string similarity analysis.
/// </summary>
public enum ErrorType
{
    /// <summary>1-2 character substitutions between expected and actual.</summary>
    CharacterSwap,

    /// <summary>Actual is a prefix or suffix of expected (truncated result).</summary>
    Truncation,

    /// <summary>Levenshtein distance exceeds 50% of expected length.</summary>
    CompletelyWrong,

    /// <summary>Empty actual result (False Negative).</summary>
    NoResult
}

/// <summary>
/// Detailed information about a single OCR error.
/// </summary>
/// <param name="FileName">Image file that produced the error.</param>
/// <param name="Expected">Expected chip name from ground truth.</param>
/// <param name="Actual">Actual OCR output (may be empty for FN).</param>
/// <param name="Confidence">Confidence score from the OCR engine.</param>
/// <param name="Type">Categorized error type.</param>
public record ErrorDetail(
    string FileName,
    string Expected,
    string Actual,
    float Confidence,
    ErrorType Type
);

using System.Text.RegularExpressions;

namespace Services;

/// <summary>
/// Normalizes raw OCR output into a canonical form suitable for database matching.
/// </summary>
public static partial class OcrTextNormalizer
{
    [GeneratedRegex("[^A-Z0-9-]")]
    private static partial Regex InvalidCharsRegex();

    /// <summary>
    /// Removes whitespace, converts to uppercase, strips invalid characters.
    /// Valid characters: A-Z, 0-9, hyphen.
    /// </summary>
    /// <param name="rawOcrText">Raw text from Tesseract OCR output.</param>
    /// <returns>Normalized string containing only A-Z, 0-9, and hyphen. Returns empty string for null/empty input.</returns>
    public static string Normalize(string? rawOcrText)
    {
        if (string.IsNullOrEmpty(rawOcrText))
            return string.Empty;

        // Remove all whitespace characters (spaces, newlines, tabs)
        var noWhitespace = string.Concat(rawOcrText.Where(c => !char.IsWhiteSpace(c)));

        // Convert to uppercase
        var uppercased = noWhitespace.ToUpperInvariant();

        // Strip characters outside the set A-Z, 0-9, hyphen
        var normalized = InvalidCharsRegex().Replace(uppercased, string.Empty);

        return normalized;
    }
}

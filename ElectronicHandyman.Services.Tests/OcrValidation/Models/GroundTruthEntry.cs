namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Represents a single ground truth entry mapping an image file to its expected chip name.
/// </summary>
/// <param name="FileName">Image filename relative to the test images directory.</param>
/// <param name="ExpectedChipName">Expected OCR output after normalization.</param>
/// <param name="Category">Difficulty category (defaults to "normal" if absent in CSV).</param>
public record GroundTruthEntry(string FileName, string ExpectedChipName, string Category);

namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Contains the loaded test data set including valid entries and skipped files.
/// </summary>
/// <param name="Entries">Valid ground truth entries with existing image files.</param>
/// <param name="SkippedFiles">Files referenced in CSV but missing from disk.</param>
/// <param name="BasePath">Base path to the test data directory.</param>
public record TestDataSet(
    IReadOnlyList<GroundTruthEntry> Entries,
    IReadOnlyList<string> SkippedFiles,
    string BasePath
);

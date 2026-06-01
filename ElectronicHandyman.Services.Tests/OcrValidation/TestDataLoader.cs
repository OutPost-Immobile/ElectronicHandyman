using ElectronicHandyman.Services.Tests.OcrValidation.Models;

namespace ElectronicHandyman.Services.Tests.OcrValidation;

/// <summary>
/// Loads ground truth CSV and discovers test images from the test data directory.
/// </summary>
public class TestDataLoader
{
    private readonly string _basePath;

    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png"];

    public TestDataLoader(string basePath)
    {
        _basePath = basePath;
    }

    /// <summary>
    /// Loads ground truth CSV and validates image existence.
    /// Throws InvalidOperationException if CSV is missing or empty.
    /// </summary>
    public TestDataSet Load()
    {
        var csvPath = Path.Combine(_basePath, "ground_truth.csv");

        if (!File.Exists(csvPath))
        {
            throw new InvalidOperationException(
                $"Ground truth CSV file not found at: {csvPath}");
        }

        var lines = File.ReadAllLines(csvPath);

        if (lines.Length == 0)
        {
            throw new InvalidOperationException(
                $"Ground truth CSV file is empty: {csvPath}");
        }

        var header = lines[0].Split(',');
        var fileNameIndex = Array.FindIndex(header, h => h.Trim().Equals("FileName", StringComparison.OrdinalIgnoreCase));
        var expectedIndex = Array.FindIndex(header, h => h.Trim().Equals("ExpectedChipName", StringComparison.OrdinalIgnoreCase));
        var categoryIndex = Array.FindIndex(header, h => h.Trim().Equals("Category", StringComparison.OrdinalIgnoreCase));

        if (fileNameIndex < 0 || expectedIndex < 0)
        {
            throw new InvalidOperationException(
                $"Ground truth CSV must contain 'FileName' and 'ExpectedChipName' columns. Found headers: {lines[0]}");
        }

        var dataLines = lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        if (dataLines.Count == 0)
        {
            throw new InvalidOperationException(
                $"Ground truth CSV file contains no data rows: {csvPath}");
        }

        var entries = new List<GroundTruthEntry>();
        var skippedFiles = new List<string>();

        foreach (var line in dataLines)
        {
            var columns = line.Split(',');

            var fileName = columns[fileNameIndex].Trim();
            var expectedChipName = columns[expectedIndex].Trim();
            var category = categoryIndex >= 0 && categoryIndex < columns.Length
                ? columns[categoryIndex].Trim()
                : "normal";

            if (string.IsNullOrWhiteSpace(category))
            {
                category = "normal";
            }

            var imagePath = GetImagePathInternal(fileName);

            if (!File.Exists(imagePath))
            {
                skippedFiles.Add(fileName);
                continue;
            }

            entries.Add(new GroundTruthEntry(fileName, expectedChipName, category));
        }

        return new TestDataSet(entries, skippedFiles, _basePath);
    }

    /// <summary>
    /// Returns full path to image file for a given entry.
    /// </summary>
    public string GetImagePath(GroundTruthEntry entry)
    {
        return GetImagePathInternal(entry.FileName);
    }

    private string GetImagePathInternal(string fileName)
    {
        return Path.Combine(_basePath, "Images", fileName);
    }
}

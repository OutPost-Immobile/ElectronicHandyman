using ElectronicHandyman.Services.Tests.OcrValidation.Models;
using Xunit;

namespace ElectronicHandyman.Services.Tests.OcrValidation.Tests;

/// <summary>
/// Integration tests that run the full OCR validation pipeline against test images
/// and assert quality thresholds. All tests skip gracefully when test data is unavailable.
/// </summary>
public class OcrValidationTests
{
    public const double MinF1Threshold = 0.7;
    public const double MaxAvgTimeMs = 5000;
    public const double LowConfidenceThreshold = 0.6;

    private static readonly string TestDataPath = Path.Combine(
        AppContext.BaseDirectory, "TestData", "OcrValidation");

    private static readonly string ReportOutputPath = Path.Combine(
        AppContext.BaseDirectory, "TestResults", "OcrValidation");

    private readonly ITestOutputHelper _output;

    public OcrValidationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void F1Score_ExceedsMinimumThreshold()
    {
        if (!Directory.Exists(TestDataPath))
        {
            Assert.Skip($"Test data directory not available: {TestDataPath}");
            return;
        }

        var (_, metrics, _) = RunValidation();

        _output.WriteLine($"F1-Score: {metrics.Classification.F1Score:F4}");
        _output.WriteLine($"Precision: {metrics.Classification.Precision:F4}");
        _output.WriteLine($"Recall: {metrics.Classification.Recall:F4}");
        _output.WriteLine($"TP: {metrics.Classification.TruePositives}, FP: {metrics.Classification.FalsePositives}, FN: {metrics.Classification.FalseNegatives}");

        Assert.True(
            metrics.Classification.F1Score >= MinF1Threshold,
            $"F1-Score {metrics.Classification.F1Score:F4} is below minimum threshold {MinF1Threshold}");
    }

    [Fact]
    public void AverageProcessingTime_BelowMaxThreshold()
    {
        if (!Directory.Exists(TestDataPath))
        {
            Assert.Skip($"Test data directory not available: {TestDataPath}");
            return;
        }

        var (_, metrics, _) = RunValidation();

        _output.WriteLine($"Average processing time: {metrics.Performance.AverageTimeMs:F2} ms");
        _output.WriteLine($"Min: {metrics.Performance.MinTimeMs:F2} ms, Max: {metrics.Performance.MaxTimeMs:F2} ms");
        _output.WriteLine($"FPS: {metrics.Performance.Fps:F2}");

        if (metrics.Performance.Bottlenecks.Count > 0)
        {
            _output.WriteLine($"Bottlenecks (> 2x avg): {string.Join(", ", metrics.Performance.Bottlenecks)}");
        }

        Assert.True(
            metrics.Performance.AverageTimeMs < MaxAvgTimeMs,
            $"Average processing time {metrics.Performance.AverageTimeMs:F2} ms exceeds maximum threshold {MaxAvgTimeMs} ms");
    }

    [Theory]
    [MemberData(nameof(GetTestImages))]
    public void PerImage_OcrResult(string fileName, string expectedChipName, string category)
    {
        if (!Directory.Exists(TestDataPath))
        {
            Assert.Skip($"Test data directory not available: {TestDataPath}");
            return;
        }

        var loader = new TestDataLoader(TestDataPath);
        var runner = new ValidationRunner(loader);
        var result = runner.Run();

        var imageResult = result.Results.FirstOrDefault(r => r.FileName == fileName);

        if (imageResult == null)
        {
            _output.WriteLine($"[SKIPPED] {fileName} — image not found on disk");
            Assert.Skip($"Image file not available: {fileName}");
            return;
        }

        _output.WriteLine($"File: {imageResult.FileName}");
        _output.WriteLine($"Category: {imageResult.Category}");
        _output.WriteLine($"Expected: {imageResult.Expected}");
        _output.WriteLine($"Actual: {imageResult.Actual}");
        _output.WriteLine($"Classification: {imageResult.Classification}");
        _output.WriteLine($"Char Accuracy: {imageResult.CharAccuracy:P2}");
        _output.WriteLine($"Confidence: {imageResult.Confidence:F2}");
        _output.WriteLine($"Processing Time: {imageResult.ProcessingTimeMs} ms");

        // This test reports results; it does not assert pass/fail per image.
        // The aggregate F1 threshold test handles overall quality gating.
    }

    /// <summary>
    /// Provides test image data from ground truth CSV for the Theory test.
    /// Returns empty when test data directory is not available.
    /// </summary>
    public static IEnumerable<object[]> GetTestImages()
    {
        if (!Directory.Exists(TestDataPath))
        {
            yield break;
        }

        var csvPath = Path.Combine(TestDataPath, "ground_truth.csv");
        if (!File.Exists(csvPath))
        {
            yield break;
        }

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1)
        {
            yield break;
        }

        var header = lines[0].Split(',');
        var fileNameIndex = Array.FindIndex(header, h => h.Trim().Equals("FileName", StringComparison.OrdinalIgnoreCase));
        var expectedIndex = Array.FindIndex(header, h => h.Trim().Equals("ExpectedChipName", StringComparison.OrdinalIgnoreCase));
        var categoryIndex = Array.FindIndex(header, h => h.Trim().Equals("Category", StringComparison.OrdinalIgnoreCase));

        if (fileNameIndex < 0 || expectedIndex < 0)
        {
            yield break;
        }

        foreach (var line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var columns = line.Split(',');
            var fileName = columns[fileNameIndex].Trim();
            var expected = columns[expectedIndex].Trim();
            var category = categoryIndex >= 0 && categoryIndex < columns.Length
                ? columns[categoryIndex].Trim()
                : "normal";

            if (string.IsNullOrWhiteSpace(category))
            {
                category = "normal";
            }

            yield return new object[] { fileName, expected, category };
        }
    }

    /// <summary>
    /// Runs the full validation pipeline and generates reports.
    /// </summary>
    private (ValidationResult Result, ValidationMetrics Metrics, PipelineConfiguration Config) RunValidation()
    {
        var config = new PipelineConfiguration("baseline");
        var loader = new TestDataLoader(TestDataPath);
        var runner = new ValidationRunner(loader);
        var result = runner.Run(config);
        var metrics = MetricsCalculator.Calculate(result);

        // Generate reports
        var reportGenerator = new ReportGenerator(ReportOutputPath);
        var mdPath = reportGenerator.GenerateMarkdownReport(result, metrics, config);
        var csvPath = reportGenerator.GenerateCsvReport(result);

        _output.WriteLine($"Markdown report: {mdPath}");
        _output.WriteLine($"CSV report: {csvPath}");

        return (result, metrics, config);
    }
}

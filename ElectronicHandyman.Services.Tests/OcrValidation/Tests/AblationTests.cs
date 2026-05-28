using ElectronicHandyman.Services.Tests.OcrValidation.Models;
using Xunit;

namespace ElectronicHandyman.Services.Tests.OcrValidation.Tests;

/// <summary>
/// Ablation study tests that compare multiple pipeline configurations and identify
/// the best-performing one. Skips gracefully when test data is unavailable.
/// </summary>
public class AblationTests
{
    private static readonly string TestDataPath = Path.Combine(
        AppContext.BaseDirectory, "TestData", "OcrValidation");

    private static readonly string ReportOutputPath = Path.Combine(
        AppContext.BaseDirectory, "TestResults", "OcrValidation");

    private readonly ITestOutputHelper _output;

    public AblationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Ablation_DefaultConfigurations_ProducesRecommendation()
    {
        if (!Directory.Exists(TestDataPath))
        {
            Assert.Skip($"Test data directory not available: {TestDataPath}");
            return;
        }

        var loader = new TestDataLoader(TestDataPath);
        var ablationRunner = new AblationRunner(loader);

        var ablationResult = ablationRunner.Run(AblationRunner.DefaultConfigurations);

        // Log results for each configuration
        _output.WriteLine($"Configurations tested: {ablationResult.Results.Count}");
        _output.WriteLine(string.Empty);

        foreach (var (config, metrics) in ablationResult.Results)
        {
            _output.WriteLine($"Config: {config.Name}");
            _output.WriteLine($"  F1-Score: {metrics.Classification.F1Score:F4}");
            _output.WriteLine($"  Precision: {metrics.Classification.Precision:F4}");
            _output.WriteLine($"  Recall: {metrics.Classification.Recall:F4}");
            _output.WriteLine($"  Mean Char Accuracy: {metrics.CharAccuracy.MeanAccuracy:F4}");
            _output.WriteLine($"  Avg Processing Time: {metrics.Performance.AverageTimeMs:F2} ms");
            _output.WriteLine(string.Empty);
        }

        _output.WriteLine($"Recommended configuration: {ablationResult.RecommendedConfig.Name}");

        // Generate ablation comparison report
        var reportGenerator = new ReportGenerator(ReportOutputPath);
        var reportPath = reportGenerator.GenerateAblationReport(ablationResult.Results);
        _output.WriteLine($"Ablation report: {reportPath}");

        // Verify we got results for all default configurations
        Assert.Equal(AblationRunner.DefaultConfigurations.Count, ablationResult.Results.Count);

        // Verify a recommended config was identified
        Assert.NotNull(ablationResult.RecommendedConfig);
        Assert.Contains(
            ablationResult.Results,
            r => r.Config.Name == ablationResult.RecommendedConfig.Name);
    }
}

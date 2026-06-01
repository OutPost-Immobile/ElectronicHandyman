using ElectronicHandyman.Services.Tests.OcrValidation.Models;

namespace ElectronicHandyman.Services.Tests.OcrValidation;

/// <summary>
/// Result of an ablation study comparing multiple pipeline configurations.
/// </summary>
/// <param name="Results">Metrics for each configuration tested.</param>
/// <param name="RecommendedConfig">Configuration with the highest F1-Score.</param>
public record AblationResult(
    IReadOnlyList<(PipelineConfiguration Config, ValidationMetrics Metrics)> Results,
    PipelineConfiguration RecommendedConfig
);

/// <summary>
/// Runs the validation pipeline across multiple configurations and identifies the best one.
/// </summary>
public class AblationRunner
{
    private readonly TestDataLoader _loader;

    /// <summary>
    /// Default ablation configurations for comparing pipeline settings.
    /// </summary>
    public static IReadOnlyList<PipelineConfiguration> DefaultConfigurations { get; } = new List<PipelineConfiguration>
    {
        new("baseline", UseClahe: true, ScaleFactor: 3.0, BlurKernelSize: 5, ClaheClipLimit: 3.0),
        new("no-clahe", UseClahe: false, ScaleFactor: 3.0, BlurKernelSize: 5, ClaheClipLimit: 3.0),
        new("scale-2x", UseClahe: true, ScaleFactor: 2.0, BlurKernelSize: 5, ClaheClipLimit: 3.0),
        new("scale-4x", UseClahe: true, ScaleFactor: 4.0, BlurKernelSize: 5, ClaheClipLimit: 3.0),
        new("large-blur", UseClahe: true, ScaleFactor: 3.0, BlurKernelSize: 7, ClaheClipLimit: 3.0),
    };

    public AblationRunner(TestDataLoader loader)
    {
        _loader = loader;
    }

    /// <summary>
    /// Runs validation for each configuration and identifies the best one by F1-Score.
    /// </summary>
    /// <param name="configurations">List of pipeline configurations to compare.</param>
    /// <returns>Ablation result with metrics for each config and the recommended config.</returns>
    public AblationResult Run(IReadOnlyList<PipelineConfiguration> configurations)
    {
        if (configurations == null || configurations.Count == 0)
        {
            throw new ArgumentException("At least one configuration must be provided.", nameof(configurations));
        }

        var runner = new ValidationRunner(_loader);
        var results = new List<(PipelineConfiguration Config, ValidationMetrics Metrics)>();

        foreach (var config in configurations)
        {
            var validationResult = runner.Run(config);
            var metrics = MetricsCalculator.Calculate(validationResult);
            results.Add((config, metrics));
        }

        // Identify configuration with highest F1-Score
        var recommended = results
            .OrderByDescending(r => r.Metrics.Classification.F1Score)
            .First()
            .Config;

        return new AblationResult(results, recommended);
    }
}

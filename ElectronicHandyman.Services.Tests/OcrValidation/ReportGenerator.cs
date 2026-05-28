using System.Globalization;
using System.Text;
using ElectronicHandyman.Services.Tests.OcrValidation.Models;

namespace ElectronicHandyman.Services.Tests.OcrValidation;

/// <summary>
/// Generates Markdown and CSV reports from OCR validation results.
/// </summary>
public class ReportGenerator
{
    private readonly string _outputDir;

    public ReportGenerator(string outputDir)
    {
        _outputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
    }

    /// <summary>
    /// Generates full Markdown report with all sections.
    /// Returns the file path of the generated report.
    /// </summary>
    public string GenerateMarkdownReport(
        ValidationResult result,
        ValidationMetrics metrics,
        PipelineConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(config);

        Directory.CreateDirectory(_outputDir);

        var timestamp = DateTime.Now;
        var fileName = $"report_{timestamp:yyyy-MM-dd_HHmmss}.md";
        var filePath = Path.Combine(_outputDir, fileName);

        var sb = new StringBuilder();

        // Header with timestamp and config name
        sb.AppendLine($"# OCR Validation Report");
        sb.AppendLine();
        sb.AppendLine($"- **Configuration:** {config.Name}");
        sb.AppendLine($"- **Run Timestamp:** {result.RunTimestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Total Images:** {result.Results.Count}");
        sb.AppendLine($"- **Skipped Files:** {result.SkippedFiles.Count}");
        sb.AppendLine();

        // Metrics Summary
        sb.AppendLine("## Metrics Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Precision | {metrics.Classification.Precision:F4} |");
        sb.AppendLine($"| Recall | {metrics.Classification.Recall:F4} |");
        sb.AppendLine($"| F1-Score | {metrics.Classification.F1Score:F4} |");
        sb.AppendLine($"| Mean Char Accuracy | {metrics.CharAccuracy.MeanAccuracy:F4} |");
        sb.AppendLine($"| Mean Confidence | {metrics.Confidence.MeanConfidence:F4} |");
        sb.AppendLine();
        sb.AppendLine("### Classification Counts");
        sb.AppendLine();
        sb.AppendLine($"- True Positives: {metrics.Classification.TruePositives}");
        sb.AppendLine($"- False Positives: {metrics.Classification.FalsePositives}");
        sb.AppendLine($"- False Negatives: {metrics.Classification.FalseNegatives}");
        sb.AppendLine();

        // Per-Image Results Table
        sb.AppendLine("## Per-Image Results");
        sb.AppendLine();
        sb.AppendLine("| FileName | Expected | Actual | Result | CharAccuracy | Confidence | Time (ms) | Category |");
        sb.AppendLine("|----------|----------|--------|--------|--------------|------------|-----------|----------|");

        foreach (var img in result.Results)
        {
            var resultLabel = img.Classification switch
            {
                Classification.TruePositive => "TP",
                Classification.FalsePositive => "FP",
                Classification.FalseNegative => "FN",
                _ => "?"
            };

            sb.AppendLine($"| {img.FileName} | {img.Expected} | {img.Actual} | {resultLabel} | {img.CharAccuracy:F2} | {img.Confidence:F2} | {img.ProcessingTimeMs} | {img.Category} |");
        }

        sb.AppendLine();

        // Error Analysis
        sb.AppendLine("## Error Analysis");
        sb.AppendLine();

        if (metrics.Errors.Count == 0)
        {
            sb.AppendLine("No errors detected — all results matched ground truth.");
        }
        else
        {
            sb.AppendLine($"Total errors: {metrics.Errors.Count}");
            sb.AppendLine();
            sb.AppendLine("| FileName | Expected | Actual | Confidence | Error Type |");
            sb.AppendLine("|----------|----------|--------|------------|------------|");

            foreach (var error in metrics.Errors)
            {
                sb.AppendLine($"| {error.FileName} | {error.Expected} | {error.Actual} | {error.Confidence:F2} | {error.Type} |");
            }

            sb.AppendLine();

            // Error type summary
            var errorGroups = metrics.Errors.GroupBy(e => e.Type).OrderByDescending(g => g.Count());
            sb.AppendLine("### Error Type Distribution");
            sb.AppendLine();
            foreach (var group in errorGroups)
            {
                sb.AppendLine($"- **{group.Key}**: {group.Count()}");
            }
        }

        sb.AppendLine();

        // Performance Metrics
        sb.AppendLine("## Performance Metrics");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Average Time | {metrics.Performance.AverageTimeMs:F2} ms |");
        sb.AppendLine($"| Min Time | {metrics.Performance.MinTimeMs:F2} ms |");
        sb.AppendLine($"| Max Time | {metrics.Performance.MaxTimeMs:F2} ms |");
        sb.AppendLine($"| FPS | {metrics.Performance.Fps:F2} |");
        sb.AppendLine($"| Peak Memory | {result.PeakMemoryBytes / (1024.0 * 1024.0):F2} MB |");
        sb.AppendLine();

        if (metrics.Performance.Bottlenecks.Count > 0)
        {
            sb.AppendLine("### Bottlenecks (> 2x average time)");
            sb.AppendLine();
            foreach (var bottleneck in metrics.Performance.Bottlenecks)
            {
                sb.AppendLine($"- {bottleneck}");
            }
        }

        File.WriteAllText(filePath, sb.ToString());
        return filePath;
    }

    /// <summary>
    /// Exports raw results to CSV with required columns.
    /// Returns the file path of the generated CSV.
    /// </summary>
    public string GenerateCsvReport(ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        Directory.CreateDirectory(_outputDir);

        var timestamp = DateTime.Now;
        var fileName = $"results_{timestamp:yyyy-MM-dd_HHmmss}.csv";
        var filePath = Path.Combine(_outputDir, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("FileName,Expected,Actual,IsCorrect,CharAccuracy,Confidence,ProcessingTimeMs,Category");

        foreach (var img in result.Results)
        {
            var isCorrect = img.Classification == Classification.TruePositive;
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4:F2},{5:F2},{6},{7}",
                EscapeCsvField(img.FileName),
                EscapeCsvField(img.Expected),
                EscapeCsvField(img.Actual),
                isCorrect,
                img.CharAccuracy * 100.0,
                img.Confidence,
                img.ProcessingTimeMs,
                EscapeCsvField(img.Category)));
        }

        File.WriteAllText(filePath, sb.ToString());
        return filePath;
    }

    /// <summary>
    /// Generates ablation comparison table in Markdown.
    /// Returns the file path of the generated report.
    /// </summary>
    public string GenerateAblationReport(
        IReadOnlyList<(PipelineConfiguration Config, ValidationMetrics Metrics)> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        Directory.CreateDirectory(_outputDir);

        var timestamp = DateTime.Now;
        var fileName = $"ablation_{timestamp:yyyy-MM-dd_HHmmss}.md";
        var filePath = Path.Combine(_outputDir, fileName);

        var sb = new StringBuilder();

        sb.AppendLine("# Ablation Study Report");
        sb.AppendLine();
        sb.AppendLine($"- **Run Timestamp:** {timestamp:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Configurations Tested:** {results.Count}");
        sb.AppendLine();

        sb.AppendLine("## Configuration Comparison");
        sb.AppendLine();
        sb.AppendLine("| Config Name | Precision | Recall | F1 | Mean Char Accuracy | Mean Processing Time (ms) |");
        sb.AppendLine("|-------------|-----------|--------|----|--------------------|---------------------------|");

        foreach (var (config, metrics) in results)
        {
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "| {0} | {1:F4} | {2:F4} | {3:F4} | {4:F4} | {5:F2} |",
                config.Name,
                metrics.Classification.Precision,
                metrics.Classification.Recall,
                metrics.Classification.F1Score,
                metrics.CharAccuracy.MeanAccuracy,
                metrics.Performance.AverageTimeMs));
        }

        sb.AppendLine();

        // Identify best configuration
        if (results.Count > 0)
        {
            var best = results.OrderByDescending(r => r.Metrics.Classification.F1Score).First();
            sb.AppendLine($"**Recommended Configuration:** {best.Config.Name} (F1: {best.Metrics.Classification.F1Score:F4})");
        }

        File.WriteAllText(filePath, sb.ToString());
        return filePath;
    }

    /// <summary>
    /// Escapes a CSV field value by quoting it if it contains commas, quotes, or newlines.
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}

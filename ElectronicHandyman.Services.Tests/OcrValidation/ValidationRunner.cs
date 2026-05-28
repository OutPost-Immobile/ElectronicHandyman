using System.Diagnostics;
using ElectronicHandyman.Services.Tests.OcrValidation.Models;
using Services;

namespace ElectronicHandyman.Services.Tests.OcrValidation;

/// <summary>
/// Orchestrates the full OCR validation pipeline: loads test data, processes images,
/// collects results with timing and memory metrics.
/// </summary>
public class ValidationRunner
{
    private readonly TestDataLoader _loader;

    public ValidationRunner(TestDataLoader loader)
    {
        _loader = loader;
    }

    /// <summary>
    /// Runs the full validation pipeline on all loaded test images.
    /// Returns empty result with warning if no test data available.
    /// </summary>
    /// <param name="config">Optional pipeline configuration for ablation studies.
    /// Currently passed to the processing wrapper for future use when ProcessImage
    /// supports configurable parameters.</param>
    public ValidationResult Run(PipelineConfiguration? config = null)
    {
        var dataSet = _loader.Load();

        if (dataSet.Entries.Count == 0)
        {
            return new ValidationResult(
                Results: Array.Empty<ImageResult>(),
                SkippedFiles: dataSet.SkippedFiles,
                PeakMemoryBytes: 0,
                RunTimestamp: DateTime.UtcNow
            );
        }

        var results = new List<ImageResult>();
        long peakMemory = GC.GetTotalMemory(false);

        foreach (var entry in dataSet.Entries)
        {
            var imageResult = ProcessSingleImage(entry, config);
            results.Add(imageResult);

            // Track peak memory after each image processing
            var currentMemory = GC.GetTotalMemory(false);
            if (currentMemory > peakMemory)
            {
                peakMemory = currentMemory;
            }
        }

        return new ValidationResult(
            Results: results,
            SkippedFiles: dataSet.SkippedFiles,
            PeakMemoryBytes: peakMemory,
            RunTimestamp: DateTime.UtcNow
        );
    }

    /// <summary>
    /// Processes a single image through the OCR pipeline, measuring time and collecting results.
    /// Corrupt or unreadable images are treated as FN with empty actual text.
    /// </summary>
    private ImageResult ProcessSingleImage(GroundTruthEntry entry, PipelineConfiguration? config)
    {
        var imagePath = _loader.GetImagePath(entry);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var imageBytes = File.ReadAllBytes(imagePath);
            var (actual, confidence) = ProcessImageWithConfidence(imageBytes, config);
            stopwatch.Stop();

            var classification = MetricsCalculator.Classify(entry.ExpectedChipName, actual);
            var charAccuracy = MetricsCalculator.ComputeCharAccuracy(entry.ExpectedChipName, actual);

            return new ImageResult(
                FileName: entry.FileName,
                Expected: entry.ExpectedChipName,
                Actual: actual,
                Confidence: confidence,
                CharAccuracy: charAccuracy,
                Classification: classification,
                ProcessingTimeMs: stopwatch.ElapsedMilliseconds,
                Category: entry.Category
            );
        }
        catch (Exception)
        {
            stopwatch.Stop();

            // Corrupt or unreadable images are classified as FN with empty actual
            return new ImageResult(
                FileName: entry.FileName,
                Expected: entry.ExpectedChipName,
                Actual: string.Empty,
                Confidence: 0.0f,
                CharAccuracy: 0.0,
                Classification: Classification.FalseNegative,
                ProcessingTimeMs: stopwatch.ElapsedMilliseconds,
                Category: entry.Category
            );
        }
    }

    /// <summary>
    /// Thin wrapper around <see cref="ImageProcessing.ProcessImage"/> that returns both
    /// the recognized text and a confidence score.
    ///
    /// LIMITATION: The current <c>ImageProcessing.ProcessImage</c> method only returns a string.
    /// The internal <c>RunOcrOnImage</c> method computes confidence (BoxScore average) but does
    /// not expose it through the public API. Without modifying the Services project, we cannot
    /// extract the actual confidence value.
    ///
    /// As a result, confidence is set to 0.0f as a placeholder. When the Services project is
    /// updated to expose confidence scores (e.g., via a <c>ProcessImageWithConfidence</c> overload),
    /// this wrapper should be updated to return the real value.
    /// </summary>
    /// <param name="imageBytes">Raw image bytes to process.</param>
    /// <param name="config">Optional pipeline configuration for ablation (reserved for future use).</param>
    /// <returns>Tuple of (recognized text, confidence score).</returns>
    private static (string Text, float Confidence) ProcessImageWithConfidence(
        byte[] imageBytes, PipelineConfiguration? config)
    {
        // NOTE: PipelineConfiguration is accepted here for ablation study support.
        // Currently ImageProcessing.ProcessImage uses hardcoded parameters internally
        // (CLAHE clipLimit=3.0, scale=3x, blur kernel=5). When the Services project
        // is updated to accept these as parameters, pass config values here.
        var text = ImageProcessing.ProcessImage(imageBytes);

        // Confidence is 0.0f because we cannot extract it from the current API.
        // The OCR engine internally computes confidence via BoxScore averaging in
        // RunOcrOnImage, but this value is not returned by ProcessImage.
        const float placeholderConfidence = 0.0f;

        return (text, placeholderConfidence);
    }
}

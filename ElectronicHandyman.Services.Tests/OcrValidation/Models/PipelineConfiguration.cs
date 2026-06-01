namespace ElectronicHandyman.Services.Tests.OcrValidation.Models;

/// <summary>
/// Configuration parameters for the OCR image processing pipeline.
/// Used for ablation studies comparing different settings.
/// </summary>
/// <param name="Name">Human-readable configuration name.</param>
/// <param name="UseClahe">Whether to apply CLAHE contrast enhancement.</param>
/// <param name="ScaleFactor">Image scale factor for preprocessing.</param>
/// <param name="BlurKernelSize">Gaussian blur kernel size.</param>
/// <param name="ClaheClipLimit">CLAHE clip limit parameter.</param>
public record PipelineConfiguration(
    string Name,
    bool UseClahe = true,
    double ScaleFactor = 3.0,
    int BlurKernelSize = 5,
    double ClaheClipLimit = 3.0
);

namespace ElectronicHandyman.App.Services;

/// <summary>
/// Service interface for identifying electronic chips via the backend API.
/// </summary>
public interface IChipIdentificationService
{
    /// <summary>
    /// Sends a cropped chip image to the API and returns the identification result.
    /// </summary>
    /// <param name="croppedImagePng">The cropped chip image as a PNG byte array.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>An <see cref="IdentificationResult"/> indicating success with SVG content or failure with an error message.</returns>
    Task<IdentificationResult> IdentifyChipAsync(byte[] croppedImagePng, CancellationToken ct = default);
}

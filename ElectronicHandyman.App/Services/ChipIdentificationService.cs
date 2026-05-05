using Microsoft.Extensions.Logging;

namespace ElectronicHandyman.App.Services;

/// <summary>
/// Sends cropped chip images to the backend API for identification
/// and parses the response into an <see cref="IdentificationResult"/>.
/// </summary>
public class ChipIdentificationService : IChipIdentificationService
{
    private const string ErrorPrefix = "ERROR:";
    private const string HttpClientName = "ChipIdentificationApi";
    private const string EndpointPath = "/api/image/identify-svg";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChipIdentificationService> _logger;

    public ChipIdentificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<ChipIdentificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IdentificationResult> IdentifyChipAsync(byte[] croppedImagePng, CancellationToken ct = default)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient(HttpClientName);

            using var content = new MultipartFormDataContent();
            using var imageContent = new ByteArrayContent(croppedImagePng);
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "file", "chip.png");

            var response = await client.PostAsync(EndpointPath, content, ct);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync(ct);

            if (responseText.StartsWith(ErrorPrefix, StringComparison.Ordinal))
            {
                var errorMessage = responseText[ErrorPrefix.Length..].TrimStart();
                _logger.LogWarning("Chip identification returned error: {Error}", errorMessage);

                return new IdentificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = errorMessage
                };
            }

            // Parse response: first line is "CHIP:name", rest is SVG
            string? chipName = null;
            string svgContent = responseText;

            if (responseText.StartsWith("CHIP:", StringComparison.Ordinal))
            {
                var newlineIndex = responseText.IndexOf('\n');
                if (newlineIndex > 0)
                {
                    chipName = responseText[5..newlineIndex].Trim();
                    svgContent = responseText[(newlineIndex + 1)..];
                }
            }

            return new IdentificationResult
            {
                IsSuccess = true,
                SvgContent = svgContent,
                ChipName = chipName
            };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Timeout — the HttpClient's configured timeout was exceeded
            _logger.LogWarning(ex, "Chip identification request timed out.");
            return new IdentificationResult
            {
                IsSuccess = false,
                ErrorMessage = "Przekroczono czas oczekiwania. Spróbuj ponownie."
            };
        }
        catch (TaskCanceledException)
        {
            // Caller-initiated cancellation — rethrow so the coordinator can handle it
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during chip identification.");
            return new IdentificationResult
            {
                IsSuccess = false,
                ErrorMessage = "Serwer niedostępny. Spróbuj później."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during chip identification.");
            return new IdentificationResult
            {
                IsSuccess = false,
                ErrorMessage = "Wystąpił nieoczekiwany błąd."
            };
        }
    }
}

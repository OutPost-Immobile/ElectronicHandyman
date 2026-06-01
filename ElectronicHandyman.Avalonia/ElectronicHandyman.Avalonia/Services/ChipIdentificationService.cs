using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ElectronicHandyman.Avalonia.Services;

public class ChipIdentificationService
{
    private const string EndpointPath = "/api/image/identify-svg";
    private const string BatchEndpointPath = "/api/image/identify-batch";
    private readonly HttpClient _httpClient;

    public ChipIdentificationService(string baseUrl)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<ChipIdentificationResult> IdentifyChipAsync(byte[] jpegImage, CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var imageContent = new ByteArrayContent(jpegImage);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(imageContent, "file", "chip.jpg");

            var response = await _httpClient.PostAsync(EndpointPath, content, ct);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync(ct);

            if (responseText.StartsWith("ERROR:", StringComparison.Ordinal))
            {
                return new ChipIdentificationResult
                {
                    IsSuccess = false,
                    ErrorMessage = responseText[6..].TrimStart()
                };
            }

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

            return new ChipIdentificationResult
            {
                IsSuccess = true,
                SvgContent = svgContent,
                ChipName = chipName
            };
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new ChipIdentificationResult { IsSuccess = false, ErrorMessage = "Timeout." };
        }
        catch (HttpRequestException ex)
        {
            return new ChipIdentificationResult { IsSuccess = false, ErrorMessage = $"Serwer niedostępny: {ex.Message}" };
        }
        catch (Exception ex)
        {
            return new ChipIdentificationResult { IsSuccess = false, ErrorMessage = $"Błąd: {ex.Message}" };
        }
    }

    public async Task<List<ChipIdentificationResult>> IdentifyBatchAsync(List<byte[]> images, CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();

            for (int i = 0; i < images.Count; i++)
            {
                var imageContent = new ByteArrayContent(images[i]);
                imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "files", $"chip{i}.jpg");
            }

            var response = await _httpClient.PostAsync(BatchEndpointPath, content, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var results = JsonSerializer.Deserialize<List<BatchResultItem>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var output = new List<ChipIdentificationResult>();
            foreach (var item in results ?? [])
            {
                output.Add(new ChipIdentificationResult
                {
                    IsSuccess = item.Success,
                    ChipName = item.ChipName,
                    SvgContent = item.SvgContent,
                    ErrorMessage = item.Error
                });
            }

            return output;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return [new ChipIdentificationResult { IsSuccess = false, ErrorMessage = "Timeout." }];
        }
        catch (HttpRequestException ex)
        {
            return [new ChipIdentificationResult { IsSuccess = false, ErrorMessage = $"Serwer niedostępny: {ex.Message}" }];
        }
        catch (Exception ex)
        {
            return [new ChipIdentificationResult { IsSuccess = false, ErrorMessage = $"Błąd: {ex.Message}" }];
        }
    }

    private record BatchResultItem
    {
        public bool Success { get; init; }
        public string? ChipName { get; init; }
        public string? SvgContent { get; init; }
        public string? Error { get; init; }
        public string? OcrText { get; init; }
        public string? FileName { get; init; }
    }
}

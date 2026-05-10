using ElectronicHandyman.Scrapper.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.Abstractions;
using Services.Internal;
using MatchType = Services.Internal.MatchType;

namespace ElectronicHandyman.Api.Controllers;

public static class ImageController
{
    public static IEndpointRouteBuilder MapImages(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/image/");

        group.MapPost("/upload", UploadAndProcessImageAsync)
            .WithName("Upload Images")
            .DisableAntiforgery();

        group.MapPost("/identify-svg", IdentifySvgAsync)
            .WithName("Identify SVG")
            .DisableAntiforgery();

        group.MapPost("/identify-batch", IdentifyBatchAsync)
            .WithName("Identify Batch")
            .DisableAntiforgery();
        
        return endpoints;
    }
    
    private static async Task<Results<Ok<string>, FileStreamHttpResult>> UploadAndProcessImageAsync(
        IFormFile file, 
        [FromServices] ITexasService texasService, 
        [FromServices] IDataProvider provider, 
        [FromServices] ISvgGenerator svgGenerator,
        [FromServices] IImageOverlayService overlayService) 
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        
        var originalImageBytes = memoryStream.ToArray();
        
        var text = ImageProcessing.ProcessImage(originalImageBytes);
        
        if (string.IsNullOrWhiteSpace(text))
        {
            return TypedResults.Ok("Nie udało się zidentyfikować płytki ze zdjęcia.");
        }
        
        // var response = await texasService.AuthenticateAndGetBoardsFamily(text);
        var searchResult = await provider.SearchForPinoutFuzzyAsync(text);

        if (searchResult.Type == MatchType.NotFound)
        {
            return TypedResults.Ok(searchResult.ErrorMessage);
        }

        var svgContent = await svgGenerator.GenerateSvgDocumentAsync(searchResult.Symbol!);
        
        byte[] finalImage;
        try
        {
            finalImage = overlayService.OverlaySvgOnOriginalImage(originalImageBytes, svgContent.ToString());
        }
        catch (Exception ex)
        {
            return TypedResults.Ok($"Błąd nakładania obrazu: {ex.Message}");
        }
        
        var outputStream = new MemoryStream(finalImage);
        var fileName = searchResult.Symbol!.Name ?? text;
        return TypedResults.File(outputStream, "image/jpeg", $"{fileName}_pinout.jpg");
    }

    private static async Task<IResult> IdentifySvgAsync(
        IFormFile file,
        [FromServices] IDataProvider provider,
        [FromServices] ISvgGenerator svgGenerator)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var imageBytes = memoryStream.ToArray();

        var text = ImageProcessing.ProcessImage(imageBytes);

        if (string.IsNullOrWhiteSpace(text))
        {
            return Results.Text("ERROR: Nie udało się zidentyfikować płytki ze zdjęcia.", "text/plain");
        }

        var searchResult = await provider.SearchForPinoutFuzzyAsync(text);

        if (searchResult.Type == MatchType.NotFound)
        {
            return Results.Text($"ERROR: {searchResult.ErrorMessage}", "text/plain");
        }

        var svgDocument = await svgGenerator.GenerateSvgDocumentAsync(searchResult.Symbol!);
        var chipName = searchResult.Symbol!.Name ?? text;

        // Return chip name on first line, then SVG content
        var responseBody = $"CHIP:{chipName}\n{svgDocument}";
        return Results.Text(responseBody, "text/plain");
    }

    private static async Task<IResult> IdentifyBatchAsync(
        IFormFileCollection files,
        [FromServices] IDataProvider provider,
        [FromServices] ISvgGenerator svgGenerator)
    {
        var results = new List<object>();

        // Save crops to local folder for debugging/training
        var cropsDir = Path.Combine(AppContext.BaseDirectory, "crops", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        var processedDir = Path.Combine(AppContext.BaseDirectory, "processed", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(cropsDir);
        Directory.CreateDirectory(processedDir);

        int index = 0;
        foreach (var file in files)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            // Save crop locally
            var cropPath = Path.Combine(cropsDir, $"chip_{index:D2}.jpg");
            await File.WriteAllBytesAsync(cropPath, imageBytes);

            var processedPath = Path.Combine(processedDir, $"chip_{index:D2}_processed.png");
            var text = ImageProcessing.ProcessImage(imageBytes, processedPath);
            index++;

            if (string.IsNullOrWhiteSpace(text))
            {
                results.Add(new { success = false, error = "Nie udało się odczytać tekstu z obrazu.", fileName = file.FileName });
                continue;
            }

            var searchResult = await provider.SearchForPinoutFuzzyAsync(text);

            if (searchResult.Type == MatchType.NotFound)
            {
                results.Add(new { success = false, error = searchResult.ErrorMessage, ocrText = text, fileName = file.FileName });
                continue;
            }

            var svgDocument = await svgGenerator.GenerateSvgDocumentAsync(searchResult.Symbol!);
            var chipName = searchResult.Symbol!.Name ?? text;

            results.Add(new { success = true, chipName, svgContent = svgDocument.ToString(), ocrText = text, fileName = file.FileName });
        }

        return Results.Json(results);
    }
}
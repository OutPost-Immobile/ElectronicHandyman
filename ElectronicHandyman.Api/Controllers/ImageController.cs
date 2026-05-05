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
}
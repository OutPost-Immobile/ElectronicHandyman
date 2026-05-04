using ElectronicHandyman.Scrapper.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services;
using Services.Abstractions;

namespace ElectronicHandyman.Api.Controllers;

public static class ImageController
{
    public static IEndpointRouteBuilder MapImages(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/image/");

        group.MapPost("/upload", UploadAndProcessImageAsync)
            .WithName("Upload Images")
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
        var board = await provider.SearchForPinoutAsync(text);
        var svgContent = await svgGenerator.GenerateSvgDocumentAsync(board);
        
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
        return TypedResults.File(outputStream, "image/jpeg", $"{text}_pinout.jpg");
    }
}
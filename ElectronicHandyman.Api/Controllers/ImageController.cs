using Microsoft.AspNetCore.Http.HttpResults;
using Services;

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
    
    private static async Task<Ok<string>> UploadAndProcessImageAsync(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var imageBytes = memoryStream.ToArray();
        
        var text = ImageProcessing.ProcessImage(imageBytes);
        
        
        
        return TypedResults.Ok("Plik wczytany pomyślnie");
    }
}
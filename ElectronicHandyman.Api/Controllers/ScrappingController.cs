using System.Xml.Linq;
using ElectronicHandyman.Api.Services;
using ElectronicHandyman.Scrapper.Abstractions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Abstractions;

namespace ElectronicHandyman.Api.Controllers;

public static class ScrappingController
{
    public static IEndpointRouteBuilder MapScrapping(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/scrapping");

        group.MapGet("/arduino", ScrapArduinoPageAsync);
        group.MapGet("/file-loader", LoadDataFromKicadFilesAsync);
        group.MapGet("/pinout/{boardName}", GetBoardPinoutSvgAsync);
        
        return endpoints;
    }

    private static async Task ScrapArduinoPageAsync([FromServices] ArduinoService arduinoService)
    {
        await arduinoService.GetAndPersistDataFromArduinoPage();
    }

    private static async Task LoadDataFromKicadFilesAsync([FromServices] IFileDataLoader fileLoader)
    {
        await fileLoader.LoadKicadSymFilesAsync();
    }

    private static async Task<IResult> GetBoardPinoutSvgAsync([FromRoute] string boardName, [FromServices] IDataProvider provider,
        [FromServices] ISvgGenerator svgGenerator)
    {
        var data = await provider.SearchForPinoutAsync(boardName);
        
        var svg = await svgGenerator.GenerateSvgDocumentAsync(data);

        return TypedResults.Content(svg.ToString(SaveOptions.DisableFormatting), "image/svg+xml");
    }
}
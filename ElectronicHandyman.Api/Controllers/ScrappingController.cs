using ElectronicHandyman.Api.Services;
using ElectronicHandyman.Scrapper.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace ElectronicHandyman.Api.Controllers;

public static class ScrappingController
{
    public static IEndpointRouteBuilder MapScrapping(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/scrapping");

        group.MapGet("/arduino", ScrapArduinoPageAsync);
        
        return endpoints;
    }

    private static async Task ScrapArduinoPageAsync([FromServices] ArduinoService arduinoService)
    {
        await arduinoService.GetAndPersistDataFromArduinoPage();
    }
}
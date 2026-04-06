using ElectronicHandyman.Domain.Enums;
using ElectronicHandyman.Scrapper.Abstractions;
using ElectronicHandyman.Scrapper.Models;

namespace ElectronicHandyman.Api.Services;

internal class ArduinoService
{
    private readonly IArduinoScrapperService _arduinoScrapperService;
    private readonly IScrappingDataPersister _scrappingDataPersister;

    public ArduinoService(IArduinoScrapperService arduinoScrapperService, IScrappingDataPersister scrappingDataPersister)
    {
        _arduinoScrapperService = arduinoScrapperService;
        _scrappingDataPersister = scrappingDataPersister;
    }

    public async Task GetAndPersistDataFromArduinoPage()
    {
        var boardsFamilies = await _arduinoScrapperService.ScrapArduinoBoardsPageAsync();

        var tasks = boardsFamilies
            .SelectMany(x => x.Boards)
            .Select(x => _arduinoScrapperService.ScrapFromJsonAsync(x.Href, x.Name));
        
        var boardData = (await Task.WhenAll(tasks)).AsEnumerable();

        foreach (var data in boardData)
        {
            var documentSources = data.Downloads.Edges
                .Select(x => x.NodeModel)
                .Where(x => x.Base.Contains("pinout.pdf") || x.Base.Contains("schematics.pdf"))
                .Select(x => new DocumentModel
                {
                    DocumentType = x.Base.Contains("pinout.pdf") ? DocumentType.Pinout : DocumentType.Schematics,
                    FileName = x.Base,
                    StaticUrl = x.PublicUrl
                })
                .ToList();

            if (documentSources.Count > 0)
            {
                var existingBoard = boardsFamilies
                    .SelectMany(x => x.Boards)
                    .First(x => x.Name == data.BoardName);
                
                existingBoard.Documents.AddRange(documentSources);
            }
        }

        await _scrappingDataPersister.PersistArduinoScrapingData(boardsFamilies);
    }
}
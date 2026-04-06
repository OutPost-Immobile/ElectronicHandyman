using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using ElectronicHandyman.Scrapper.Abstractions;
using ElectronicHandyman.Scrapper.Internal;
using ElectronicHandyman.Scrapper.Models;
using ElectronicHandyman.Scrapper.Models.Api;
using ElectronicHandyman.Scrapper.Options;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectronicHandyman.Scrapper.Services;

internal class ArduinoScrapperService : IArduinoScrapperService
{
    private readonly ArduinoPageFetcher _pageFetcher;
    private readonly SourceUrl _arduinoOptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ArduinoScrapperService> _logger;

    public ArduinoScrapperService(ArduinoPageFetcher pageFetcher, IOptions<SourceOptions> options, IHttpClientFactory httpClientFactory, ILogger<ArduinoScrapperService> logger)
    {
        _pageFetcher = pageFetcher;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _arduinoOptions = options.Value.Items.First(x => x.Name == "Arduino");
    }

    public async Task<IEnumerable<BoardFamilyModel>> ScrapArduinoBoardsPageAsync()
    {
        var families = await _pageFetcher.FetchFamiliesAsync();
        
        var htmlDict = await _pageFetcher.FetchHtmlBoardsFromFamilyAsync(families);
        
        var boardFamilyList = new ConcurrentBag<BoardFamilyModel>();

        await Parallel.ForEachAsync(htmlDict.Keys,  (key, ct) =>
        {
            var model = new BoardFamilyModel
            {
                FamilyName = key,
                Boards = []
            };

            var htmlDoc = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true,
                OptionCheckSyntax = false
            };

            htmlDoc.LoadHtml(htmlDict[key]);

            var selectedDivsContent = htmlDoc.DocumentNode
                .SelectNodes("//div[contains(@class, 'arduino-boards__categories--item')]");

            foreach (var selectedDiv in selectedDivsContent)
            {
                var nodes = selectedDiv.SelectNodes(".//a[@href]");
                    
                var boards = nodes
                    .Select(x => new BoardModel
                    {
                        Href = x.GetAttributeValue("href",
                            string.Empty),
                        Name = x.InnerText,
                        Documents = []
                    });

                model.Boards.AddRange(boards);
            }
            
            boardFamilyList.Add(model);
            return default;
        });

        return boardFamilyList;
    }

    public async Task<DataModel> ScrapFromJsonAsync(string boardUrl, string boardName)
    {
        var baseUrl = _arduinoOptions.Url.TrimEnd('/');
        var path = boardUrl.Trim('/');

        var primaryUrl = $"{baseUrl}/{path}/page-data.json";
        var fallbackUrl = $"{baseUrl}/page-data/{path}/page-data.json";

        using var client = _httpClientFactory.CreateClient();

        try
        {
            return await FetchDataAsync(client, primaryUrl, boardName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch from primary URL: {Url}. Attempting fallback.", primaryUrl);
            
            return await FetchDataAsync(client, fallbackUrl, boardName);
        }
    }
    
    private async Task<DataModel> FetchDataAsync(HttpClient client, string url, string boardName)
    {
        var data = await client.GetFromJsonAsync<ArduinoPageDataResponseModel>(url);
        
        data?.Result.Data.BoardName = boardName;
        
        return data?.Result.Data ?? throw new InvalidOperationException($"Deserialized data from {url} was null.");
    }
}
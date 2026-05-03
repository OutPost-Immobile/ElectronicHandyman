using ElectronicHandyman.Scrapper.Abstractions;
using ElectronicHandyman.Scrapper.Clients;
using ElectronicHandyman.Scrapper.Internal;
using ElectronicHandyman.Scrapper.Models;
using ElectronicHandyman.Scrapper.Options;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ElectronicHandyman.Scrapper.Services;

internal class TexasService : ITexasService
{
    private readonly PlaywrightBrowserProvider _playwrightBrowserProvider;
    private readonly TexasOptions _texasOptions;
    private readonly TexasApiClient _texasApiClient;

    public TexasService(PlaywrightBrowserProvider playwrightBrowserProvider, IOptions<TexasOptions> texasOptions, TexasApiClient texasApiClient)
    {
        _playwrightBrowserProvider = playwrightBrowserProvider;
        _texasApiClient = texasApiClient;
        _texasOptions = texasOptions.Value;
    }

    public async Task<IEnumerable<BoardFamilyModel>> AuthenticateAndGetBoardsFamily(string boardFamilyName)
    {
        await _texasApiClient.AuthenticateAndPersistTokenAsync();
        
        var response = await _texasApiClient.SearchForBoardAsync(boardFamilyName);
        
        // TODO;

        return [];
    }

    public async Task<Stream> GetBoardDocumentAsync(string boardFamilyName)
    {
        var browser = await _playwrightBrowserProvider.GetBrowserAsync();
        
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync(_texasOptions.DataSheetUrl + boardFamilyName);
            
            await page.Locator(".//div[@id='4']")
                .ClickAsync(new LocatorClickOptions
                {
                    Force = true
                });
            
            await Task.Delay(500);

            var url = await page.Locator("img.image[src$='.svg']")
                .First
                .EvaluateAsync<string>("img => img.src");

            var response = await page.APIRequest.GetAsync(url);

            var stream = new MemoryStream(await response.BodyAsync());
            
            return stream;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch data from {_texasOptions.DataSheetUrl + boardFamilyName}.", ex);            
        }
    }
}
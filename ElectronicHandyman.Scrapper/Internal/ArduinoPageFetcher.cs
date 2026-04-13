using ElectronicHandyman.Scrapper.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ElectronicHandyman.Scrapper.Internal;

internal class ArduinoPageFetcher
{
    private readonly PlaywrightBrowserProvider _playwrightProvider;
    private readonly ArduinoOptions _arduinoOptions;
    private readonly ILogger<ArduinoPageFetcher> _logger;

    public ArduinoPageFetcher(PlaywrightBrowserProvider playwrightProvider, IOptions<ArduinoOptions> options, ILogger<ArduinoPageFetcher> logger)
    {
        _playwrightProvider = playwrightProvider;
        _logger = logger;
        _arduinoOptions = options.Value;
    }

    public async Task<List<string>> FetchFamiliesAsync()
    {
        var browser = await _playwrightProvider.GetBrowserAsync();
        
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        
        try
        {
            await page.GotoAsync(_arduinoOptions.Url + "/hardware");
            await page.WaitForSelectorAsync(".arduino-boards__box");
            var families = await page.Locator(".arduino-boards__box h5").AllInnerTextsAsync();
            return families.ToList();
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    public async Task<Dictionary<string, string>> FetchHtmlBoardsFromFamilyAsync(List<string> familyNames)
    {
        var browser = await _playwrightProvider.GetBrowserAsync();
        
        await using var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();
        
        var htmlDict = new Dictionary<string, string>();
        
        try
        {
            await page.GotoAsync(_arduinoOptions.Url + "/hardware");
            await page.WaitForSelectorAsync(".arduino-boards__box");

            foreach (var familyName in familyNames)
            {
                await page.Locator(".arduino-boards__box h5")
                    .GetByText(familyName, new LocatorGetByTextOptions { Exact = true})
                    .ClickAsync(new LocatorClickOptions
                    {
                        Force = true
                    });
                
                await Task.Delay(500);
                
                var html = await page.Locator(".arduino-boards__columns")
                    .First
                    .InnerHTMLAsync();
                
                htmlDict.Add(familyName, html);
                _logger.LogInformation($"{familyName} loaded");
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        finally
        {
            await page.CloseAsync();
        }

        return htmlDict;
    }
}
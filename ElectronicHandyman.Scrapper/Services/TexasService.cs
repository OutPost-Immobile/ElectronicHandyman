using ElectronicHandyman.Domain.Enums;
using ElectronicHandyman.Scrapper.Abstractions;
using ElectronicHandyman.Scrapper.Clients;
using ElectronicHandyman.Scrapper.Internal;
using ElectronicHandyman.Scrapper.Models;
using ElectronicHandyman.Scrapper.Models.Api;
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

    public async Task<IEnumerable<BoardFamilyModel>> AuthenticateAndGetBoardsFamily(string query)
    {
        await _texasApiClient.AuthenticateAndPersistTokenAsync();
   
        var familyResponse = await _texasApiClient.SearchForBoardFamilyAsync(query);
        
        var productsToMap = new List<Product>();

        if (familyResponse?.Content != null && familyResponse.Content.Count > 0)
        {
            productsToMap.AddRange(familyResponse.Content);
        }
        else
        {
            var specificProduct = await _texasApiClient.GetProductByIdentifierAsync(query);
            
            if (specificProduct != null)
            {
                productsToMap.Add(specificProduct);
            }
        }
        
        if (productsToMap.Count == 0)
        {
            return [];
        }
        
        var families = productsToMap
            .GroupBy(p => p.GenericProductIdentifier ?? "Unknown Family")
            .Select(group => new BoardFamilyModel
            {
                FamilyName = group.Key,
                Boards = group.Select(product => new BoardModel
                {
                    Name = product.Identifier,
                    Href = product.Url,
                    Documents = new List<DocumentModel>
                    {
                        new DocumentModel 
                        { 
                            DocumentType = DocumentType.Datasheet, 
                            FileName = $"{product.Identifier}_Datasheet", 
                            StaticUrl = product.DatasheetUrl 
                        },
                        new DocumentModel 
                        { 
                            DocumentType = DocumentType.QualityEstimator, 
                            FileName = $"{product.Identifier}_QualityEstimator", 
                            StaticUrl = product.QualityEstimatorUrl 
                        },
                        new DocumentModel 
                        { 
                            DocumentType = DocumentType.MaterialContent, 
                            FileName = $"{product.Identifier}_MaterialContent", 
                            StaticUrl = product.MaterialContentUrl 
                        }
                    }.Where(doc => !string.IsNullOrWhiteSpace(doc.StaticUrl)).ToList()
                }).ToList()
            });

        return families;
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
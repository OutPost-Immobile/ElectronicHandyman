using System.Net.Http.Headers;
using System.Net.Http.Json;
using ElectronicHandyman.Domain;
using ElectronicHandyman.Domain.Domain.Config;
using ElectronicHandyman.Scrapper.Models.Api;
using ElectronicHandyman.Scrapper.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ElectronicHandyman.Scrapper.Clients;

internal class TexasApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TexasOptions _texasOptions;
    private readonly HandymanDbContext _dbContext;
    
    public TexasApiClient(IHttpClientFactory httpClientFactory, IOptions<TexasOptions> options, HandymanDbContext dbContext)
    {
        _httpClientFactory = httpClientFactory;
        _dbContext = dbContext;
        _texasOptions = options.Value;
    }

    public async Task<string> AuthenticateAndPersistTokenAsync()
    {
        using var client = _httpClientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

        var request = new
        {
            grant_type = "client_credentials",
            client_id = _texasOptions.Key,
            client_secret = _texasOptions.Secret,
        };
        
        var response = await client.PostAsJsonAsync(_texasOptions.Url + "oauth/accesstoken", request);
        
        var token = await response.Content.ReadAsStringAsync();

        var entity = new TexasInstrumentsApiConfigEntity
        {
            AccessToken = token
        };
        
        _dbContext.Add(entity);
        await _dbContext.SaveChangesAsync();

        return token;
    }

    public async Task<TexasApiResponseModel> SearchForBoardAsync(string boardName)
    {
        var token = await _dbContext.TexasApiConfig
            .Select(x => x.AccessToken)
            .FirstOrDefaultAsync() ?? await AuthenticateAndPersistTokenAsync();

        using var client = _httpClientFactory.CreateClient();
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var parameters = new Dictionary<string, string>
        {
            ["GenericProductIdentifier"] = boardName,
            ["PageNumber"] = "0",
            ["Size"] = "100"
        };
        
        var url = QueryHelpers.AddQueryString(_texasOptions.Url + "products", parameters);

        var response = await client.GetFromJsonAsync<TexasApiResponseModel>(url) ?? throw new InvalidOperationException($"Failed to fetch data from {url}.");

        if (response.TotalPages <= 1)
        {
            return response;
        }

        for (int i = 1; i < response.TotalPages; i++)
        {
            parameters["PageNumber"] = i.ToString();
                
            var tempUrl = QueryHelpers.AddQueryString(_texasOptions.Url + "products", parameters);

            var tempResponse = await client.GetFromJsonAsync<TexasApiResponseModel>(tempUrl);
                
            response.Content.AddRange(tempResponse.Content);
        }

        return response;
    }
}
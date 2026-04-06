using ElectronicHandyman.Scrapper.Abstractions;
using ElectronicHandyman.Scrapper.Internal;
using ElectronicHandyman.Scrapper.Options;
using ElectronicHandyman.Scrapper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicHandyman.Scrapper;

public static class ServiceExtensions
{
    public static IServiceCollection AddScrapper(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<PlaywrightBrowserProvider>();
        services.AddHttpClient();
        
        services
            .AddScoped<ArduinoPageFetcher>()
            .AddScoped<IArduinoScrapperService, ArduinoScrapperService>()
            .AddScoped<IScrappingDataPersister, ScrappingDataPersister>();
        
        services.AddOptions<SourceOptions>()
            .Bind(configuration.GetSection("Source"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }
}
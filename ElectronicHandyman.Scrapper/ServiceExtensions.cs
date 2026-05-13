using ElectronicHandyman.Scrapper.Abstractions;
using ElectronicHandyman.Scrapper.Clients;
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
            .AddScoped<TexasApiClient>()
            .AddScoped<ArduinoPageFetcher>()
            .AddScoped<ITexasService, TexasService>()
            .AddScoped<IArduinoScrapperService, ArduinoScrapperService>()
            .AddScoped<IScrappingDataPersister, ScrappingDataPersister>();
        
        services.AddOptions<ArduinoOptions>()
            .Bind(configuration.GetSection(ArduinoOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddOptions<TexasOptions>()
            .Bind(configuration.GetSection(TexasOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        services.AddOptions<PlatformIOOPtions>()
            .Bind(configuration.GetSection(PlatformIOOPtions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        
        return services;
    }
}
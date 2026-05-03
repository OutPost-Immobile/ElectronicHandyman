using Microsoft.Extensions.DependencyInjection;
using Services.Abstractions;
using Services.Internal;

namespace Services;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services
            .AddScoped<IFileDataLoader, FileDataLoader>()
            .AddScoped<ISvgGenerator, SvgGenerator>()
            .AddScoped<IDataProvider, DataProvider>();
        
        return services;
    } 
}
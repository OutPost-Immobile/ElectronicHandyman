using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicHandyman.Domain;

public static class ServiceExtensions
{
    public static IServiceCollection AddDomain(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");
        
        services.AddPooledDbContextFactory<HandymanDbContext>(options => 
                options.UseNpgsql(connectionString), 
            poolSize: 32);
        
        services.AddScoped<HandymanDbContext>(sp => 
        {
            var factory = sp.GetRequiredService<IDbContextFactory<HandymanDbContext>>();
            return factory.CreateDbContext();
        });
        
        return services;
    }
}
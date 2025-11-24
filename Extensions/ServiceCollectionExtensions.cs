using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TourViet.Data;
using TourViet.Services;

namespace TourViet.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SqlServer");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'SqlServer' was not found.");
        }

        services.AddDbContext<TourBookingDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
    
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {

        // ITourService is now registered in Program.cs with new implementation
        services.AddScoped<ITourInstanceService, TourInstanceService>();
        
        return services;
    }
}


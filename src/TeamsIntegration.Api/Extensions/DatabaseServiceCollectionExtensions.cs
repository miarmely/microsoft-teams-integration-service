using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Data;

namespace TeamsIntegration.Api.Extensions;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSql(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL") ??
            throw new InvalidOperationException("PostgreSQL connection string was not configured.");

        services.AddDbContext<TeamsDbContext>(opts =>
        {
            opts.UseNpgsql(connectionString);
        });

        return services;
    }
}

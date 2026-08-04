using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Logging.Database;
using TeamsIntegration.Api.Logging.Providers;
using TeamsIntegration.Api.Logging.Queue;
using TeamsIntegration.Api.Logging.Services;

namespace TeamsIntegration.Api.Logging.Extensions;

public static class LoggingExtensions
{
    public static IServiceCollection AddDatabaseLogging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // get "database looger options" settings
        services
            .AddOptions<DatabaseLoggerOptions>()
            .Bind(configuration.GetSection(DatabaseLoggerOptions.SectionName))
            .Validate(opt =>
                Enum.IsDefined(opt.MinimumLevel),
                "'DatabaseLogging:MinumumLevel' is invalid in 'application.json' file!")
            .Validate(opt =>
                opt.QueueCapacity > 0,
                "'DatabaseLogging:QueueCapacity' must be greater than 0 in 'application.json' file!")
            .Validate(opt =>
                opt.BatchSize > 0,
                "'DatabaseLogging:BatchSize' must be greater than 0 in 'application.json' file!")
            .Validate(opt =>
                opt.FlushInterval > TimeSpan.Zero,
                "'DatabaseLogging:FlushInterval' must be greater than 0 in 'application.json' file!")
            .Validate(opt =>
                opt.BatchSize <= opt.QueueCapacity,
                "'DatabaseLogging:BatchSize' cannot exceed 'DatabaseLogging:QueueCapacity' in 'application.json' file!")
            .ValidateOnStart();

        services.AddPooledDbContextFactory<LoggingDbContext>(opts =>
        {
            var connectionString = configuration.GetConnectionString("PostgreSQL") ??
                throw new InvalidOperationException("PostgreSQL connection string was not configured.");

            opts.UseNpgsql(
                connectionString,
                opts =>
                {
                    opts.MigrationsHistoryTable("__LoggingMigrationsHistory");  // save different place
                });
        });

        // For access "Http Context" infos when creating "Application Log" entity.
        services.AddHttpContextAccessor();

        services.AddSingleton<ILogQueue, LogQueue>();
        services.AddSingleton<DatabaseLoggerProvider>();
        services.AddSingleton<ILoggerProvider>(serviceProvider => serviceProvider.GetRequiredService<DatabaseLoggerProvider>());

        services.AddHostedService<DatabaseLogWriterBackgroundService>();

        return services;
    }

    public static ILoggingBuilder AddFiltersFoDatabaseLogging(
        this ILoggingBuilder logging)
    {
        logging.AddFilter<DatabaseLoggerProvider>(
            "Microsoft.EntityFrameworkCore.Database.Command",
            LogLevel.Warning);

        logging.AddFilter<DatabaseLoggerProvider>(
            "Microsoft.AspNetCore",
            LogLevel.Warning);

        logging.AddFilter<DatabaseLoggerProvider>(
            "TeamsIntegration.Api",
            LogLevel.Information);

        return logging;
    }
}

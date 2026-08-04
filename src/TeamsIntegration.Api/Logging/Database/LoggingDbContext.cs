using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Logging.Configurations;
using TeamsIntegration.Api.Logging.Entities;

namespace TeamsIntegration.Api.Logging.Database;

public sealed class LoggingDbContext(
    DbContextOptions<LoggingDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(
            new ApplicationLogConfiguration());
    }
}

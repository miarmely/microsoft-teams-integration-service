using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Entities;

namespace TeamsIntegration.Api.Data;

public sealed class TeamsDbContext(
    DbContextOptions<TeamsDbContext> options) : DbContext(options)
{
    public DbSet<TeamsMessage> TeamsMessages => Set<TeamsMessage>();
    public DbSet<MessageMedia> MessageMedias => Set<MessageMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TeamsDbContext).Assembly,
            type => type.Namespace == "TeamsIntegration.Api.Data.Configurations");
    }
}

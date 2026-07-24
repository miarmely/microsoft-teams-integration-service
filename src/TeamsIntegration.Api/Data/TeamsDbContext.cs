using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Entities;

namespace TeamsIntegration.Api.Data;

public sealed class TeamsDbContext(
    DbContextOptions<TeamsDbContext> options) : DbContext(options)
{
    public DbSet<TeamsMessage> TeamsMessages => Set<TeamsMessage>();
    public DbSet<MessageMedia> MessageMedia => Set<MessageMedia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamsDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}

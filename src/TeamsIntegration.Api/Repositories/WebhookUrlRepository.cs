using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Data;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed class WebhookUrlRepository(
    TeamsDbContext dbCtx,
    ILogger<WebhookUrlRepository> logger)
    : BaseRepository<WebhookUrlRepository>(dbCtx, logger), IWebhookUrlRepository
{
    public async Task CreateAsync(
        WebhookUrl entity,
        CancellationToken cancellationToken = default)
    {
        await DbCtx.WebhookUrls.AddAsync(entity, cancellationToken);
    }

    public async Task<WebhookUrl?> GetWebhookUrlAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default)
    {
        return await DbCtx.WebhookUrls
            .AsNoTracking()
            .SingleOrDefaultAsync(
            w => w.TeamId == teamId
                && w.ChannelId == channelId,
            cancellationToken);
    }

    public Task<WebhookUrl?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return DbCtx.WebhookUrls.SingleOrDefaultAsync(
            webhook => webhook.Id == id,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<WebhookUrl>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbCtx.WebhookUrls
            .AsNoTracking()
            .OrderBy(webhook => webhook.TeamId)
            .ThenBy(webhook => webhook.ChannelId)
            .ToListAsync(cancellationToken);
    }

    public void Update(
        WebhookUrl entity)
    {
        DbCtx.WebhookUrls.Update(entity);
    }

    public void Delete(
        WebhookUrl entity)
    {
        DbCtx.WebhookUrls.Remove(entity);
    }
}

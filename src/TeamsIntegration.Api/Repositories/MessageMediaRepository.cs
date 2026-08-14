using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Data;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed class MessageMediaRepository(
    TeamsDbContext dbCtx,
    ILogger<MessageMediaRepository> logger)
    : BaseRepository<MessageMediaRepository>(dbCtx, logger), IMessageMediaRepository
{
    public Task<MessageMedia?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return DbCtx.MessageMedias
            .AsNoTracking()
            .SingleOrDefaultAsync(
                media => media.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        MessageMedia media,
        CancellationToken cancellationToken = default)
    {
        await DbCtx.MessageMedias.AddAsync(
            media,
            cancellationToken);
    }

    public async Task<MessageMedia?> GetByTeamsMessageAndHostedContentIdAsync(
        Guid teamsMessageId,
        string graphHostedContentId,
        CancellationToken cancellationToken = default)
    {
        var msgMedia = await DbCtx.MessageMedias.SingleOrDefaultAsync(
            m => m.TeamsMessageId == teamsMessageId
                && m.GraphHostedContentId == graphHostedContentId,
            cancellationToken);

        return msgMedia;
    }
}

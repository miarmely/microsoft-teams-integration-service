using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Data;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed class MessageMediaRepository(
    TeamsDbContext dbCtx) : IMessageMediaRepository
{
    public async Task AddAsync(
        MessageMedia media,
        CancellationToken cancellationToken = default)
    {
        await dbCtx.MessageMedias.AddAsync(
            media,
            cancellationToken);
    }

    public async Task<MessageMedia?> GetByTeamsMessageAndHostedContentIdAsync(
        Guid teamsMessageId,
        string graphHostedContentId,
        CancellationToken cancellationToken = default)
    {
        var msgMedia = await dbCtx.MessageMedias.SingleOrDefaultAsync(
            m => m.TeamsMessageId == teamsMessageId
                && m.GraphHostedContentId == graphHostedContentId,
            cancellationToken);

        return msgMedia;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbCtx.SaveChangesAsync(cancellationToken);
    }
}

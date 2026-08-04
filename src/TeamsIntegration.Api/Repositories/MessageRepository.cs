using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Data;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed class MessageRepository(
    TeamsDbContext dbCtx,
    ILogger<MessageRepository> logger)
    : BaseRepository<MessageRepository>(dbCtx, logger), IMessageRepository
{
    public async Task AddAsync(
        TeamsMessage message,
        CancellationToken cancellationToken = default)
    {
        await DbCtx.TeamsMessages.AddAsync(
            message,
            cancellationToken);
    }

    public async Task<TeamsMessage?> GetByGraphIdAsync(
        string teamId,
        string channelId,
        string graphMessageId,
        CancellationToken cancellationToken = default)
    {
        var msg = await DbCtx.TeamsMessages
            .Include(x => x.Media)
            .SingleOrDefaultAsync(
                m => m.TeamId == teamId
                    && m.ChannelId == channelId
                    && m.GraphMessageId == graphMessageId,
                cancellationToken);

        return msg;
    }

    public async Task<List<TeamsMessage>> GetByChannelAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default)
    {
        var messages = await DbCtx.TeamsMessages
            .Include(m => m.Media)
            .Where(m => m.TeamId == teamId
                && m.ChannelId == channelId)
            .OrderByDescending(m => m.MessageCreatedAt)
            .ToListAsync(cancellationToken);

        return messages;
    }
}

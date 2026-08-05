using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Data;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;
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

    public async Task<IReadOnlyCollection<TeamsMessageResponse>> GetByChannelAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default)
    {
        var messages = await DbCtx.TeamsMessages
            .AsNoTracking()
            .Where(m => m.TeamId == teamId
                && m.ChannelId == channelId)
            .OrderByDescending(m => m.MessageCreatedAt)
            .Select(message => new TeamsMessageResponse
            {
                Id = message.Id,
                GraphMessageId = message.GraphMessageId,
                TeamId = message.TeamId,
                ChannelId = message.ChannelId,
                ReplyToId = message.ReplyToId,
                Subject = message.Subject,
                HtmlContent = message.HtmlContent,
                ContentType = message.ContentType,
                SenderId = message.SenderId,
                SenderDisplayName = message.SenderDisplayName,
                MessageCreatedAt = message.MessageCreatedAt,
                MessageLastModifiedAt = message.MessageLastModifiedAt,
                MessageDeletedAt = message.MessageDeletedAt,
                WebUrl = message.WebUrl,
                CreatedAt = message.CreatedAt,
                UpdatedAt = message.UpdatedAt,

                Media = message.Media
                    .OrderBy(media => media.UploadedAt)
                    .Select(media => new MessageMediaResponse
                    {
                        Id = media.Id,
                        GraphHostedContentId = media.GraphHostedContentId,
                        BucketName = media.BucketName,
                        ObjectName = media.ObjectName,
                        ContentType = media.ContentType,
                        SizeBytes = media.SizeBytes,
                        ETag = media.ETag,
                        UploadedAt = media.UploadedAt
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return messages;
    }
}

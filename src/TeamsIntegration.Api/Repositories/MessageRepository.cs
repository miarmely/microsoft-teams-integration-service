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
        int pageNumber = 1,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var rawMessages = new List<TeamsMessage>();

        // if messages by pagination
        if (pageSize != null
            && pageSize > 0)
        {
            int _pageSize = (int)pageSize;
            var skip = (pageNumber - 1) * _pageSize;

            rawMessages = await DbCtx.TeamsMessages
                .AsNoTracking()
                .Include(message => message.Media)
                .Where(m => m.TeamId == teamId
                    && m.ChannelId == channelId)
                .OrderByDescending(m => m.MessageCreatedAt)
                .ThenByDescending(m => m.Id)
                .Skip(skip)
                .Take(_pageSize)
                .ToListAsync(cancellationToken);
        }

        // get all messages
        else
        {
            rawMessages = await DbCtx.TeamsMessages
                .AsNoTracking()
                .Include(message => message.Media)
                .Where(m => m.TeamId == teamId
                    && m.ChannelId == channelId)
                .OrderByDescending(m => m.MessageCreatedAt)
                .ThenByDescending(m => m.Id)
                .ToListAsync(cancellationToken);
        }

        var messages = rawMessages
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
            .ToList();

        return messages;
    }

    public async Task<IReadOnlyCollection<TeamsMessage>> GetForExportAsync(
        string teamId,
        string channelId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        CancellationToken cancellationToken = default)
    {
        var query = DbCtx.TeamsMessages
            .AsNoTracking()
            .AsSplitQuery()
            .Include(message => message.Media)
            .Where(message => message.TeamId == teamId
                && message.ChannelId == channelId);

        if (fromDate.HasValue)
            query = query.Where(message => message.MessageCreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(message => message.MessageCreatedAt <= toDate.Value);

        var messages = await query
            .OrderByDescending(message => message.MessageCreatedAt)
            .ThenByDescending(message => message.Id)
            .ToListAsync(cancellationToken);

        return messages;
    }

    public async Task<IReadOnlyCollection<TeamsMessage>> GetForDeletionAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default)
    {
        return await DbCtx.TeamsMessages
            .AsSplitQuery()
            .Include(message => message.Media)
            .Where(message => message.TeamId == teamId
                && message.ChannelId == channelId
                && message.MessageCreatedAt >= fromDate
                && message.MessageCreatedAt <= toDate)
            .OrderBy(message => message.MessageCreatedAt)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
    }

    public void DeleteRange(IEnumerable<TeamsMessage> messages)
    {
        DbCtx.TeamsMessages.RemoveRange(messages);
    }
}

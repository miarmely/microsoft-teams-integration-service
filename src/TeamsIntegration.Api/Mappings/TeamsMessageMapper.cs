using Microsoft.Graph.Models;
using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Mappings;

public static partial class TeamsMessageMapper
{
    /// <summary>
    /// Compare "currentValue" and "newValue" based on bit.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="currentValue"></param>
    /// <param name="newValue"></param>
    /// <param name="setter"></param>
    /// <returns></returns>
    private static bool SetIfChanged<T>(
        T currentValue,
        T newValue,
        Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            return false;

        setter(newValue);

        return true;
    }
}

public static partial class TeamsMessageMapper
{
    /// <summary>
    /// Convert "ChatMessage" to "TeamsMessage".
    /// </summary>
    /// <param name="graphMessage"></param>
    /// <param name="teamId"></param>
    /// <param name="channelId"></param>
    /// <param name="utcNow">Current UTC time used for persistence timestamps.</param>
    /// <returns></returns>
    public static TeamsMessage CreateEntity(
        ChatMessage graphMessage,
        string teamId,
        string channelId,
        DateTimeOffset utcNow)
    {
        // validations
        ArgumentNullException.ThrowIfNull(graphMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);

        if (string.IsNullOrWhiteSpace(graphMessage.Id))
        {
            throw new ArgumentNullException(
                "Message hasn't id! Microsoft Graph Message ID cannot be empty.",
                nameof(graphMessage));
        }

        return new TeamsMessage
        {
            Id = Guid.NewGuid(),
            GraphMessageId = graphMessage.Id!,
            TeamId = teamId,
            ChannelId = channelId,
            ReplyToId = graphMessage.ReplyToId,
            Subject = graphMessage.Subject,
            HtmlContent = graphMessage.Body?.Content,
            ContentType = graphMessage.Body?.ContentType?.ToString(),
            SenderId = graphMessage.From?.User?.Id,
            SenderDisplayName = graphMessage.From?.User?.DisplayName,
            MessageCreatedAt = graphMessage.CreatedDateTime,
            MessageLastModifiedAt = graphMessage.LastModifiedDateTime,
            MessageDeletedAt = graphMessage.DeletedDateTime,
            WebUrl = graphMessage.WebUrl,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };
    }

    /// <summary>
    /// Compare parameters of "graphMessage" and "entity", if any param of "graphMesage" has changed then write to "entity".
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="graphMessage"></param>
    /// <param name="utcNow">Current UTC time used when a change is persisted.</param>
    /// <returns></returns>
    public static bool UpdateEntity(
        TeamsMessage entity,
        ChatMessage graphMessage,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(graphMessage);

        var hasChanges = false;

        hasChanges |= SetIfChanged(
            entity.Subject,
            graphMessage.Subject,
            value => entity.Subject = value);

        hasChanges |= SetIfChanged(
            entity.HtmlContent,
            graphMessage.Body?.Content,
            value => entity.HtmlContent = value);

        hasChanges |= SetIfChanged(
            entity.ContentType,
            graphMessage.Body?.ContentType?.ToString(),
            value => entity.ContentType = value);

        hasChanges |= SetIfChanged(
            entity.SenderId,
            graphMessage.From?.User?.Id,
            value => entity.SenderId = value);

        hasChanges |= SetIfChanged(
            entity.SenderDisplayName,
            graphMessage.From?.User?.DisplayName,
            value => entity.SenderDisplayName = value);

        hasChanges |= SetIfChanged(
            entity.MessageLastModifiedAt,
            graphMessage.LastModifiedDateTime,
            value => entity.MessageLastModifiedAt = value);

        hasChanges |= SetIfChanged(
            entity.MessageDeletedAt,
            graphMessage.DeletedDateTime,
            value => entity.MessageDeletedAt = value);

        hasChanges |= SetIfChanged(
            entity.WebUrl,
            graphMessage.WebUrl,
            value => entity.WebUrl = value);

        // if any param has changed
        if (hasChanges)
            entity.UpdatedAt = utcNow;

        return hasChanges;
    }
}

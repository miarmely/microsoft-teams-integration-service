using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Repositories.Interfaces;

/// <summary>
/// It provides to manipulate teams messages on Database. Example Scenario: You fetched messages from "Microsoft Teams" and you will save them to Database.
/// </summary>
public interface IMessageRepository : IBaseRepository
{
    /// <summary>
    /// Create message on db.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddAsync(
        TeamsMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get message on the channel which matched by id.
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="channelId"></param>
    /// <param name="graphMessageId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TeamsMessage?> GetByGraphIdAsync(
        string teamId,
        string channelId,
        string graphMessageId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get messages on the channel.
    /// </summary>
    /// <param name="teamId"></param>
    /// <param name="channelId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<TeamsMessageResponse>> GetByChannelAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default);
}

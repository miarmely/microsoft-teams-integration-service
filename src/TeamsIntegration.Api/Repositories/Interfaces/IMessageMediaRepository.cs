using TeamsIntegration.Api.Entities;

namespace TeamsIntegration.Api.Repositories.Interfaces;


/// <summary>
/// It provides to manipulate "medias" of "teams messages" on Database. Example Scenario: You fetched "medias" of one "teams message" from "Microsoft Teams" and you will save them to Database.
/// </summary>
public interface IMessageMediaRepository : IBaseRepository
{
    Task<MessageMedia?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MessageMedia?> GetByTeamsMessageAndHostedContentIdAsync(
        Guid teamsMessageId,
        string graphHostedContentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MessageMedia media,
        CancellationToken cancellationToken = default);
}

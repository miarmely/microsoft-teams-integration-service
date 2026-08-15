using TeamsIntegration.Api.Entities;

namespace TeamsIntegration.Api.Repositories.Interfaces;

public interface IWebhookUrlRepository : IBaseRepository
{
    Task CreateAsync(
        WebhookUrl entity,
        CancellationToken cancellationToken = default);

    Task<WebhookUrl?> GetWebhookUrlAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default);

    Task<WebhookUrl?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WebhookUrl>> GetAllAsync(
        CancellationToken cancellationToken = default);

    void Update(WebhookUrl entity);

    void Delete(WebhookUrl entity);
}

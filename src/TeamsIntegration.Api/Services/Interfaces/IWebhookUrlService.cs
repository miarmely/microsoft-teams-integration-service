using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IWebhookUrlService
{
    Task<ServiceResponse<IReadOnlyCollection<WebhookUrlResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ServiceResponse<WebhookUrlResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResponse<WebhookUrlResponse>> GetByChannelAsync(string teamId, string channelId, CancellationToken cancellationToken = default);
    Task<ServiceResponse<WebhookUrlResponse>> CreateAsync(CreateWebhookUrlRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse<WebhookUrlResponse>> UpdateAsync(Guid id, UpdateWebhookUrlRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResponse> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

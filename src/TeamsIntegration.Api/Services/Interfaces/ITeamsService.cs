using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface ITeamsService
{
    Task<ServiceResponse<MessageSendResponse>> SendMessageToChannelAsync(
        TeamsSendMultipleMessageRequest req,
        CancellationToken cancellationToken = default);
}

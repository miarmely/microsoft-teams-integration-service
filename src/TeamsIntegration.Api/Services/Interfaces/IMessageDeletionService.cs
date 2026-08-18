using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMessageDeletionService
{
    /// <summary>
    /// Permanently removes synchronized messages in an inclusive date range.
    /// MinIO objects are removed first so database rows remain available for a safe retry
    /// whenever object storage rejects part of the operation.
    /// </summary>
    Task<ServiceResponse<DeleteSynchronizedMessagesResponse>> DeleteAsync(
        string teamId,
        string channelId,
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        CancellationToken cancellationToken = default);
}

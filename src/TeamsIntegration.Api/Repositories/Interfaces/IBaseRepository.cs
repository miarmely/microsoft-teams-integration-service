using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Repositories.Interfaces;

public interface IBaseRepository
{
    /// <summary>
    /// Save changes to the database with error handling and logging.
    /// </summary>
    /// <param name="teamId">For logging (optional)</param>
    /// <param name="channelId">For logging (optional)</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ServiceResponse> SaveChangesAsync(
        string? teamId = null,
        string? channelId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detach the entity from the DbContext to stop tracking it. This is useful when you want to prevent changes to the entity from being saved to the database.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="message"></param>
    ServiceResponse Detach<TEntity>(
        TEntity message);

    /// <summary>
    /// Clear all tracked entities from the DbContext. This is useful when you want to reset the state of the context and prevent any changes from being saved to the database.
    /// </summary>
    ServiceResponse ClearTracking();
}

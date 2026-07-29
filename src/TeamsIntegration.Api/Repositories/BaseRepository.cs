using Microsoft.EntityFrameworkCore;
using Npgsql;
using TeamsIntegration.Api.Data;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;


public partial class BaseRepository<TSubRepo>(
    TeamsDbContext dbCtx,
    ILogger<TSubRepo> logger) : IBaseRepository
{
    public async Task<ServiceResponse> SaveChangesAsync(
        string? teamId = null,
        string? channelId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await dbCtx.SaveChangesAsync(cancellationToken);

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException ex) // Database update error
        {
            ClearTracking();

            if (teamId != null
                && channelId != null)
                logger.LogError(
                    ex,
                    "Failed to save changes to the database. (Team: {TeamId}, Channel: {ChannelId})",
                    teamId,
                    channelId);

            else
                logger.LogError(
                    ex,
                    "Failed to save changes to the database.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "Teams messages could not be saved to the database."
            };
        }
        catch (NpgsqlException ex) // PostgreSQL connection error
        {
            ClearTracking();

            if (teamId != null
                && channelId != null)
                logger.LogError(
                    ex,
                    "PostgreSQL became unavailable while synchronizing Teams messages. (Team: {TeamId}, Channel: {ChannelId})",
                    teamId,
                    channelId);

            else
                logger.LogError(
                    ex,
                    "PostgreSQL became unavailable while synchronizing Teams messages.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "The database is temporarily unavailable."
            };
        }
        catch (Exception ex)
        {
            ClearTracking();

            if (teamId != null
                && channelId != null)
                logger.LogError(
                    ex,
                    "Unexpected error occurred while saving changes to the database. (Team: {TeamId}, Channel: {ChannelId})",
                    teamId,
                    channelId);

            else
                logger.LogError(
                    ex,
                    "Unexpected error occurred while saving changes to the database.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Unexpected error occurred while saving changes to the database."
            };
        }
    }

    public ServiceResponse Detach<TEntity>(TEntity message)
    {
        if (message == null)
            return new()
            {
                IsSuccess = false,
                StatusCode = 400,
                ErrorMessage = "'message' cannot be empty."
            };

        try
        {
            dbCtx.Entry(message).State = EntityState.Detached;

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }
        catch (NpgsqlException ex) // PostgreSQL connection error
        {
            logger.LogError(
                ex,
                "PostgreSQL became unavailable while detaching a entity on database.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "The database is temporarily unavailable."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error occurred while detaching a entity on database.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Unexpected error occurred while detaching a entity on database."
            };
        }
    }

    public ServiceResponse ClearTracking()
    {
        try
        {
            dbCtx.ChangeTracker.Clear();

            return new()
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status204NoContent
            };
        }
        catch (NpgsqlException ex) // PostgreSQL connection error
        {
            logger.LogError(
                ex,
                "PostgreSQL became unavailable while clearing tracking.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "The database is temporarily unavailable."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error occurred while clearing trackings on database.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Unexpected error occurred while clearing trackings on database."
            };
        }
    }
}

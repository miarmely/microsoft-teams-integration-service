using TeamsIntegration.Api.Entities;
using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;
using TeamsIntegration.Api.Services.Interfaces;

namespace TeamsIntegration.Api.Services;

public sealed partial class WebhookUrlService
{
    private static string? Validate(
        string teamId,
        string channelId,
        string url)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return "Team is required.";

        if (string.IsNullOrWhiteSpace(channelId))
            return "Channel is required.";

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
            return "Webhook URL must be a valid HTTPS URL.";

        return null;
    }

    private static WebhookUrlResponse Map(
        WebhookUrl entity)
        => new()
        {
            Id = entity.Id,
            TeamId = entity.TeamId,
            ChannelId = entity.ChannelId,
            Url = entity.Url,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

    private static ServiceResponse<T> Success<T>(
        T data,
        int statusCode = 200)
        => new()
        {
            IsSuccess = true,
            StatusCode = statusCode,
            Data = data
        };

    private static ServiceResponse<T> Error<T>(
        int statusCode,
        string? message)
        => new()
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = message
        };
}

public sealed partial class WebhookUrlService(
    IWebhookUrlRepository webhookRepository,
    TimeProvider timeProvider,
    ILogger<WebhookUrlService> logger) : IWebhookUrlService
{
    /// <summary>Returns every configured channel webhook. (EXCEPTION-SAFE)</summary>
    public async Task<ServiceResponse<IReadOnlyCollection<WebhookUrlResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var webhooks = await webhookRepository.GetAllAsync(cancellationToken);

            return Success<IReadOnlyCollection<WebhookUrlResponse>>(
                webhooks
                    .Select(Map)
                    .ToList());
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Failed retrieving webhook URLs.");

            return Error<IReadOnlyCollection<WebhookUrlResponse>>(
                500,
                "Webhook URLs could not be retrieved.");
        }
    }

    /// <summary>Returns one webhook by its database identifier.</summary>
    public async Task<ServiceResponse<WebhookUrlResponse>> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var webhook = await webhookRepository.GetByIdAsync(
            id,
            cancellationToken);

        return webhook == null ?
            Error<WebhookUrlResponse>(404, "Webhook URL was not found.")
            : Success(Map(webhook));
    }

    /// <summary>Resolves the single webhook assigned to a Teams channel.</summary>
    public async Task<ServiceResponse<WebhookUrlResponse>> GetByChannelAsync(
        string teamId,
        string channelId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)
            || string.IsNullOrWhiteSpace(channelId))
            return Error<WebhookUrlResponse>(
                400,
                "Team and channel are required.");

        var webhook = await webhookRepository.GetWebhookUrlAsync(
            teamId,
            channelId,
            cancellationToken);

        return webhook == null ?
            Error<WebhookUrlResponse>(404, "No webhook URL is configured for the selected channel.")
            : Success(Map(webhook));
    }

    /// <summary>Creates one webhook assignment for a Teams channel.</summary>
    public async Task<ServiceResponse<WebhookUrlResponse>> CreateAsync(
        CreateWebhookUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        // validate parameters
        var validationError = Validate(
            request.TeamId,
            request.ChannelId,
            request.Url);

        if (validationError is not null)
            return Error<WebhookUrlResponse>(400, validationError);

        // conflict control
        var existing = await webhookRepository.GetWebhookUrlAsync(
            request.TeamId,
            request.ChannelId,
            cancellationToken);

        if (existing is not null)
            return Error<WebhookUrlResponse>(409, "A webhook URL is already configured for the selected channel.");

        // create 
        var now = timeProvider.GetUtcNow();
        var entity = new WebhookUrl
        {
            Id = Guid.NewGuid(),
            TeamId = request.TeamId.Trim(),
            ChannelId = request.ChannelId.Trim(),
            Url = request.Url.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await webhookRepository.CreateAsync(
            entity,
            cancellationToken);

        // save changes to db (EXCEPTION-SAFE)
        var saveResult = await webhookRepository.SaveChangesAsync(
            entity.TeamId,
            entity.ChannelId,
            cancellationToken);

        return saveResult.IsSuccess ?
            Success(Map(entity), 201)
            : Error<WebhookUrlResponse>(saveResult.StatusCode, saveResult.ErrorMessage);
    }

    /// <summary>Updates the team, channel, or workflow URL of an assignment.</summary>
    public async Task<ServiceResponse<WebhookUrlResponse>> UpdateAsync(
        Guid id,
        UpdateWebhookUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        // validate parameters
        var validationError = Validate(
            request.TeamId,
            request.ChannelId,
            request.Url);
        if (validationError is not null)
            return Error<WebhookUrlResponse>(400, validationError);

        // check target whether exists
        var entity = await webhookRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (entity is null)
            return Error<WebhookUrlResponse>(404, "Webhook URL was not found.");

        // conflict control
        var channelWebhook = await webhookRepository.GetWebhookUrlAsync(
            request.TeamId,
            request.ChannelId,
            cancellationToken);

        if (channelWebhook is not null
            && channelWebhook.Id != id)
            return Error<WebhookUrlResponse>(
                409,
                "Another webhook URL is already configured for the selected channel.");

        // update
        entity.TeamId = request.TeamId.Trim();
        entity.ChannelId = request.ChannelId.Trim();
        entity.Url = request.Url.Trim();
        entity.UpdatedAt = timeProvider.GetUtcNow();

        webhookRepository.Update(entity);

        // save changes to db (EXCEPTION-SAFE)
        var saveResult = await webhookRepository.SaveChangesAsync(
            entity.TeamId,
            entity.ChannelId,
            cancellationToken);

        return saveResult.IsSuccess
            ? Success(Map(entity))
            : Error<WebhookUrlResponse>(saveResult.StatusCode, saveResult.ErrorMessage);
    }

    /// <summary>Deletes a channel webhook assignment.</summary>
    public async Task<ServiceResponse> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // check target entity whether exists
        var entity = await webhookRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (entity is null)
            return new()
            {
                IsSuccess = false,
                StatusCode = 404,
                ErrorMessage = "Webhook URL was not found."
            };

        // delete
        webhookRepository.Delete(entity);

        var saveResult = await webhookRepository.SaveChangesAsync(
            entity.TeamId,
            entity.ChannelId,
            cancellationToken);

        return saveResult.IsSuccess ?
            new ServiceResponse
            {
                IsSuccess = true,
                StatusCode = 200
            }
            : saveResult;
    }
}

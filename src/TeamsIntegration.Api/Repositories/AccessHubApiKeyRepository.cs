using TeamsIntegration.Api.Models.Requests;
using TeamsIntegration.Api.Models.Responses;
using TeamsIntegration.Api.Repositories.Interfaces;

namespace TeamsIntegration.Api.Repositories;

public sealed class AccessHubApiKeyRepository(
    IHttpClientFactory httpClientFactory,
    ILogger<AccessHubApiKeyRepository> logger) : IAccessHubApiKeyRepository
{
    private const string HttpClientName = "AccessHubPublic";

    public async Task<ServiceResponse<AccessHubApiKeyValidationResponse>> ValidateApiKeyAsync(
        AccessHubApiKeyValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var httpClient = httpClientFactory.CreateClient(HttpClientName);

            using var response = await httpClient.PostAsJsonAsync(
                "/api/auth/validate-api-key",
                request,
                cancellationToken);

            var result = await response.Content
                .ReadFromJsonAsync<AccessHubApiKeyValidationResponse>(cancellationToken: cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (result is null)
                {
                    logger.LogError(
                        "AccessHub API key validation returned " +
                        "an empty response.");

                    return new()
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status502BadGateway,
                        ErrorMessage = "AccessHub returned an invalid response."
                    };
                }

                return new()
                {
                    IsSuccess = true,
                    StatusCode = StatusCodes.Status200OK,
                    Data = result
                };
            }

            var statusCode = (int)response.StatusCode;

            logger.LogWarning(
                "AccessHub API key validation failed. " +
                "(StatusCode: {StatusCode}, Message: {Message})",
                statusCode,
                result?.Message);

            return new()
            {
                IsSuccess = false,
                StatusCode = statusCode,
                ErrorMessage =
                    result?.Message ??
                    "API key validation failed."
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "AccessHub API key validation timed out.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status504GatewayTimeout,
                ErrorMessage = "AccessHub validation request timed out."
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(
                ex,
                "AccessHub is unavailable during " +
                "API key validation.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ErrorMessage = "AccessHub is currently unavailable."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected exception occurred while " +
                "validating AccessHub API key.");

            return new()
            {
                IsSuccess = false,
                StatusCode = StatusCodes.Status500InternalServerError,
                ErrorMessage = "Unexpected API key validation error."
            };
        }
    }
}
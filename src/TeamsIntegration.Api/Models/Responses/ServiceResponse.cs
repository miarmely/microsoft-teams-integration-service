namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Standard envelope returned by JSON API operations.</summary>
public record ServiceResponse
{
    /// <summary>Whether the application operation completed successfully.</summary>
    public bool IsSuccess { get; init; }
    /// <summary>HTTP status code selected by the application service.</summary>
    public int StatusCode { get; init; }
    /// <summary>Human-readable failure reason; null for successful operations.</summary>
    public string? ErrorMessage { get; init; }

    public static ServiceResponse CreateErrorResponse(
        int statusCode,
        string? errorMessage = null)
    {
        return new ServiceResponse
        {
            IsSuccess = false,
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }
}


/// <summary>Standard response envelope containing a typed successful result.</summary>
/// <typeparam name="TData">Type of the endpoint result.</typeparam>
public record ServiceResponse<TData> : ServiceResponse
{
    /// <summary>Endpoint result; normally null when <c>isSuccess</c> is false.</summary>
    public TData? Data { get; init; } = default;
}

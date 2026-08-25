using System.Net;

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

    public static ServiceResponse Failure(
        string errorMessage,
        HttpStatusCode statusCode)
    {
        return new()
        {
            IsSuccess = false,
            StatusCode = (int)statusCode,
            ErrorMessage = errorMessage
        };
    }

    public static ServiceResponse Success(
        HttpStatusCode statusCode)
    {
        return new()
        {
            IsSuccess = true,
            StatusCode = (int)statusCode,

        };
    }
}


/// <summary>Standard response envelope containing a typed successful result.</summary>
/// <typeparam name="TData">Type of the endpoint result.</typeparam>
public record ServiceResponse<TData> : ServiceResponse
{
    /// <summary>Endpoint result; normally null when <c>isSuccess</c> is false.</summary>
    public TData? Data { get; init; } = default;

    public static new ServiceResponse<TData> Failure(
        string errorMessage,
        HttpStatusCode statusCode)
    {
        return new()
        {
            IsSuccess = false,
            StatusCode = (int)statusCode,
            ErrorMessage = errorMessage
        };
    }

    public static ServiceResponse<TData> Success(
        TData data,
        HttpStatusCode statusCode)
    {
        return new()
        {
            IsSuccess = true,
            StatusCode = (int)statusCode,
            Data = data
        };
    }
}

namespace TeamsIntegration.Api.Models.Responses;

public record ServiceResponse
{
    public bool IsSuccess { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }
}


public sealed record ServiceResponse<TData> : ServiceResponse
{
    public TData? Data { get; init; } = default;
}
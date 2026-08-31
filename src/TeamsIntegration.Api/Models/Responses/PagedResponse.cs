namespace TeamsIntegration.Api.Models.Responses;

public sealed record PagedResponse<T>
{
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
    public required IReadOnlyCollection<T> Items { get; init; }
}

namespace TeamsIntegration.Api.Models.Responses;

/// <summary>Basic Microsoft Teams team information.</summary>
public sealed record TeamResponse
{
    /// <summary>Microsoft Graph team identifier.</summary>
    public required string Id { get; init; }
    /// <summary>Human-readable team name.</summary>
    public string? DisplayName { get; init; }
    /// <summary>Optional team description.</summary>
    public string? Description { get; init; }
}

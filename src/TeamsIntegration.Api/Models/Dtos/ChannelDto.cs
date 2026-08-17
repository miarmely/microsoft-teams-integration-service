namespace TeamsIntegration.Api.Models.Dtos;

/// <summary>Microsoft Teams channel information returned by Graph.</summary>
public sealed record ChannelDto
{
    /// <summary>Microsoft Graph channel identifier.</summary>
    public string? Id { get; init; }
    /// <summary>Human-readable channel name.</summary>
    public string? DisplayName { get; init; }
    /// <summary>Optional channel description.</summary>
    public string? Description { get; init; }
    /// <summary>Channel membership mode, such as standard, private, or shared.</summary>
    public string? MembershipType { get; init; }
    /// <summary>Microsoft Teams URL for opening the channel.</summary>
    public string? WebUrl { get; init; }
}

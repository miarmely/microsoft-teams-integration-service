namespace TeamsIntegration.Api.Models.Dtos;

public class UserDto
{
    public required string Id { get; init; }
    public string? DisplayName { get; init; }
    public string? GivenName { get; init; }
    public string? Surname { get; init; }
    public string? Mail { get; init; }
    public string? UserPrincipalName { get; init; }
}

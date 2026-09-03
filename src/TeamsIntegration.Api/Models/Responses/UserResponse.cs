using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Models.Responses;

public sealed record UserResponse
{
    public IReadOnlyCollection<UserDto> Users { get; init; } = [];

    public int TotalUserCount => Users.Count;
}

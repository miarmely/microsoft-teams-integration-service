using TeamsIntegration.Api.Models.Dtos;

namespace TeamsIntegration.Api.Models.Responses;

public sealed record ChannelResponse
{
    public IReadOnlyCollection<ChannelDto> Channels { get; set; } = [];
}

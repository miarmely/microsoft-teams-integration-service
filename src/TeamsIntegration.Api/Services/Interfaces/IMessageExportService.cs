using TeamsIntegration.Api.Models.Dtos;
using TeamsIntegration.Api.Models.Responses;

namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMessageExportService
{
    /// <summary>
    /// Export "messages" and "messages images" in Zip format for one channel.
    /// Creates a ZIP containing "dataset.json" file and "images" folder.
    /// "dataset.json" includes Teams message infos and "images" folder includes "message images".
    /// The temporary file is deleted automatically after ASP.NET closes the response stream.
    /// </summary>
    Task<ServiceResponse<MediaContent>> ExportChannelAsync(
        string teamId,
        string channelId,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
}

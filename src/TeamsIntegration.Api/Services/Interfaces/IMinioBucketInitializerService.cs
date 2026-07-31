namespace TeamsIntegration.Api.Services.Interfaces;

public interface IMinioBucketInitializerService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

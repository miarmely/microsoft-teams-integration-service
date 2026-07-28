using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Options;
using Minio;
using TeamsIntegration.Api.Configuration;

namespace TeamsIntegration.Api.Extensions;

public static class MinioExtensions
{
    public static IServiceCollection AddMinio(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MinioOptions>(
            configuration.GetSection(MinioOptions.SectionName));


        services
            .AddOptions<MinioOptions>()
            .Bind(configuration.GetSection(MinioOptions.SectionName))
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.Endpoint),
                $"{MinioOptions.SectionName}:Endpoint is required in 'application.json' file!")
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.AccessKey),
                $"{MinioOptions.SectionName}:AccessKey is required in 'application.json' file!")
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.SecretKey),
                $"{MinioOptions.SectionName}:SecretKey is required in 'application.json' file!")
            .Validate(opt =>
                !string.IsNullOrWhiteSpace(opt.BucketName),
                $"{MinioOptions.SectionName}:BucketName is required in 'application.json' file!")
            .Validate(opt =>
                bool.TryParse(opt.UseSsl.ToString(), out var _),
                $"{MinioOptions.SectionName}:UseSsl is required in 'application.json' file!")
            .Validate(opt =>
                {
                    if (int.TryParse(opt.PresignedUrlExpirationDay.ToString(), out int presignedUrlExpirationDay)
                        && presignedUrlExpirationDay > 0)
                        return true;

                    return false;
                },
                $"{MinioOptions.SectionName}:PresignedUrlExpirationDay is required in 'application.json' file!");

        services.AddSingleton<IMinioClient>(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<MinioOptions>>()
                .Value;

            return new MinioClient()
                .WithEndpoint(options.Endpoint)
                .WithCredentials(options.AccessKey, options.SecretKey)
                .WithSSL(options.UseSsl)
                .Build();
        });

        return services;
    }
}

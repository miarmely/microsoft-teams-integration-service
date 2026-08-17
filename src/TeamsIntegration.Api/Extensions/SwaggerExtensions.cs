using Microsoft.OpenApi;
using System.Reflection;

namespace TeamsIntegration.Api.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwagger(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(opts =>
        {
            opts.SwaggerDoc(
                "v1",
                new()
                {
                    Title = "Teams Integration API",
                    Version = "v1",
                    Description = "Microsoft Teams message synchronization, media storage and notification API."
                }
            );

            // add security definition
            opts.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter the AccessHub JWT Token. Example: Bearer eyJhbGciOi..."
                });

            opts.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>()
            });

            // Import controller and DTO XML comments into endpoint and schema descriptions.
            var xmlFileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFileName));
        });

        return services;
    }
}

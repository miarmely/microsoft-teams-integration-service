using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using TeamsIntegration.Api.Data;
using TeamsIntegration.Api.Extensions;
using TeamsIntegration.Api.Logging.Database;
using TeamsIntegration.Api.Logging.Extensions;
using TeamsIntegration.Api.Services;
using TeamsIntegration.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddPostgreSql(builder.Configuration);
builder.Services.AddMicrosoftGraph(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();
builder.Services.AddMinio(builder.Configuration);
builder.Services.AddDatabaseLogging(builder.Configuration);
builder.Services.SetupAccessHubAuthorization(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("Dashboard", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["http://localhost:5173", "http://localhost:3000"];

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Content-Disposition");
    });
});

builder.Logging.AddFiltersFoDatabaseLogging();

var app = builder.Build();

// auto impletement "migrations" to db (FOR PRODUCTION)
using (var scope = app.Services.CreateScope())
{
    var teamsDbCtx = scope.ServiceProvider.GetRequiredService<TeamsDbContext>();
    await teamsDbCtx.Database.MigrateAsync();

    var loggingDbCtxFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<LoggingDbContext>>();
    await using var loggingDbCtx = await loggingDbCtxFactory.CreateDbContextAsync();
    await loggingDbCtx.Database.MigrateAsync();
}

// create "bucket" on MinIO if not exists
using (var scope = app.Services.CreateScope())
{
    var bucketInitializer = scope.ServiceProvider.GetRequiredService<IMinioBucketInitializerService>();

    await bucketInitializer.InitializeAsync();
}

// synchronize "permissions" of "Teams Integration Service" on "AccessHub"
using (var scope = app.Services.CreateScope())
{
    var accessHubInitializer = scope.ServiceProvider.GetRequiredService<AccessHubPermissionInitializerService>();

    await accessHubInitializer.InitializeAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Teams Integration API v1");

        opts.RoutePrefix = "swagger";
    });
}

// for production
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Don't use "https redirections" for docker containers (FOR PRODUCTION)
if (builder.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseCors("Dashboard");
app.UseAuthorization();
app.MapControllers();
app.Run();

/*
    DB NAME: teams_integration

    BNS Uretim:
    Team Id: 1560909e-d5c6-4695-a367-853e9beae2ff
    Channel Id: 19:z-xYxl8ZP388iVnmiFk9mKQHT48_bmLqIqZmhv1ubkM1@thread.tacv2
*/

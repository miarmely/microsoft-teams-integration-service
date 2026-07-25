using TeamsIntegration.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddPostgreSql(builder.Configuration);
builder.Services.AddMicrosoftGraph(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();

var app = builder.Build();

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

/*
    BNS Uretim:
    Team Id: 1560909e-d5c6-4695-a367-853e9beae2ff
    Channel Id: 19:z-xYxl8ZP388iVnmiFk9mKQHT48_bmLqIqZmhv1ubkM1@thread.tacv2
*/

/*
    //////// Get All Teams Api Route ////////
    -- http://localhost:5195/api/teams

    //////// Get All Channels of One Team ////////
    -- http://localhost:5195/api/teams/1560909e-d5c6-4695-a367-853e9beae2ff/channels

    //////// Get All Messages of One Channel ////////
    -- http://localhost:5195/api/teams/1560909e-d5c6-4695-a367-853e9beae2ff/channels/19:z-xYxl8ZP388iVnmiFk9mKQHT48_bmLqIqZmhv1ubkM1@thread.tacv2/messages/50

    //////// Get Image on the Image ////////
    -- http://localhost:5195/api/teams/1560909e-d5c6-4695-a367-853e9beae2ff/channels/19:z-xYxl8ZP388iVnmiFk9mKQHT48_bmLqIqZmhv1ubkM1@thread.tacv2/messages/{MESSAGE_ID}/images/{IMAGE_ID}

    /// Tehlikeli Transfer Takip Grubu (GET ALL MESSAGES)
    /// http://localhost:5195/api/teams/87d7804b-3c3e-493a-881e-d177834a3215/channels/19:lyQDSaFlPHZEEcdUTXPaqCAN_M8MVVngmyPDMGWCqy01@thread.tacv2/messages/50
    

    /// ISG SAHA Routine kontrol (GET ALL MESSAGES)
    /// http://localhost:5195/api/teams/aa2e1aea-f1db-4e0d-8059-8a15aac0d859/channels/19:9wXjJCtxOOpr76HcsHYVu-ZkKFVGOjBw1LNcPrNg1Vc1@thread.tacv2/messages/50    
*/
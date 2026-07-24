using TeamsIntegration.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMicrosoftGraph(builder.Configuration);
builder.Services.AddApplicationServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

    //////// Get All Messages of One Channel ////////
    -- http://localhost:5195/api/teams/1560909e-d5c6-4695-a367-853e9beae2ff/channels/19:z-xYxl8ZP388iVnmiFk9mKQHT48_bmLqIqZmhv1ubkM1@thread.tacv2/messages/1784816200501/images/aWQ9LHR5cGU9MSx1cmw9aHR0cHM6Ly9ldS1hcGkuYXNtLnNreXBlLmNvbS92MS9vYmplY3RzLzAtd2V1LWQyMC1jMzk3NDMxOWRkMjNmOTgyYmRiOTcyOGFlOGQwZmM2My92aWV3cy9pbWdv

    {
      "id": "1784810603358",
      "content": "\u003Cp\u003E\u003Cimg alt=\"Medya\" src=\"https://graph.microsoft.com/v1.0/teams/1560909e-d5c6-4695-a367-853e9beae2ff/channels/19:z-xYxl8ZP388iVnmiFk9mKQHT48_bmLqIqZmhv1ubkM1@thread.tacv2/messages/1784810603358/hostedContents/aWQ9LHR5cGU9MSx1cmw9aHR0cHM6Ly9ldS1hcGkuYXNtLnNreXBlLmNvbS92MS9vYmplY3RzLzAtd2V1LWQxMy01NjBlYTdkYjI3MjNiNzBlNjJmODliYTFkODJlMDg0Zi92aWV3cy9pbWdv/$value\" width=\"2448\" height=\"3264\"\u003E\u003C/p\u003E\n\n\n\u003Cp\u003ENs4 reaktörü 2688 parti nolu Kor Pd Pcl3 üretimi 1 nci basamak onayı için labaratuvara numune verilmiştir \u003C/p\u003E",
      "contentType": "Html",
      "subject": "",
      "senderDisplayName": "BNS Tesisi",
      "createdDateTime": "2026-07-23T12:43:23.358+00:00",
      "lastModifiedDateTime": "2026-07-23T12:43:24.087+00:00",
      "webUrl": "https://teams.microsoft.com/l/message/19%3Az-xYxl8ZP388iVnmiFk9mKQHT48_bmLqIqZmhv1ubkM1%40thread.tacv2/1784810603358?groupId=1560909e-d5c6-4695-a367-853e9beae2ff&tenantId=5686d588-e212-4854-9c3b-1215742e2daf&createdTime=1784810603358&parentMessageId=1784810603358"
    }

*/
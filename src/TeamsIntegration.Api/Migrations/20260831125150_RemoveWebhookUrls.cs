using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsIntegration.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWebhookUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_urls");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_urls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    team_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_urls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_webhook_url_team_channel",
                table: "webhook_urls",
                columns: new[] { "team_id", "channel_id" },
                unique: true);
        }
    }
}

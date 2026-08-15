using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsIntegration.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixWebhookUrlChannelIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_webhook_url",
                table: "webhook_urls");

            migrationBuilder.Sql(
                "ALTER TABLE webhook_urls ALTER COLUMN team_id TYPE character varying(512) USING team_id::text;");
            migrationBuilder.Sql(
                "ALTER TABLE webhook_urls ALTER COLUMN channel_id TYPE character varying(512) USING channel_id::text;");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_url_team_channel",
                table: "webhook_urls",
                columns: new[] { "team_id", "channel_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_webhook_url_team_channel",
                table: "webhook_urls");

            migrationBuilder.Sql(
                "ALTER TABLE webhook_urls ALTER COLUMN team_id TYPE uuid USING team_id::uuid;");
            migrationBuilder.Sql(
                "ALTER TABLE webhook_urls ALTER COLUMN channel_id TYPE uuid USING channel_id::uuid;");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_url",
                table: "webhook_urls",
                columns: new[] { "team_id", "channel_id", "url" },
                unique: true);
        }
    }
}

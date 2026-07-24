using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsIntegration.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "teams_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    graph_message_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    team_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    channel_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    reply_to_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    html_content = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sender_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    sender_display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    message_created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    message_last_modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    message_deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    web_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teams_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "message_media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamsMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    graph_hosted_content_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    graph_attachment_id = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    media_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_extension = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    relative_path = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    checksum = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_message_media", x => x.Id);
                    table.ForeignKey(
                        name: "FK_message_media_teams_messages_TeamsMessageId",
                        column: x => x.TeamsMessageId,
                        principalTable: "teams_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_message_media_hosted_content",
                table: "message_media",
                columns: new[] { "TeamsMessageId", "graph_hosted_content_id" },
                unique: true,
                filter: "\"graph_hosted_content_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_teams_messages_team_channel_graph_message",
                table: "teams_messages",
                columns: new[] { "team_id", "channel_id", "graph_message_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "message_media");

            migrationBuilder.DropTable(
                name: "teams_messages");
        }
    }
}

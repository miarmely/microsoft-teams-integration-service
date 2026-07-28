using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsIntegration.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMinioMetadataToMessageMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_message_media_hosted_content",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "checksum",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "file_extension",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "file_name",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "file_size",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "graph_attachment_id",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "relative_path",
                table: "message_media");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "message_media",
                newName: "uploaded_at");

            migrationBuilder.RenameColumn(
                name: "media_type",
                table: "message_media",
                newName: "bucket_name");

            migrationBuilder.AlterColumn<string>(
                name: "graph_hosted_content_id",
                table: "message_media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "content_type",
                table: "message_media",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "e_tag",
                table: "message_media",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "object_name",
                table: "message_media",
                type: "character varying(1500)",
                maxLength: 1500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "size_bytes",
                table: "message_media",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "ux_bucket_name_object_name",
                table: "message_media",
                columns: new[] { "bucket_name", "object_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_message_media_hosted_content",
                table: "message_media",
                columns: new[] { "TeamsMessageId", "graph_hosted_content_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_bucket_name_object_name",
                table: "message_media");

            migrationBuilder.DropIndex(
                name: "ux_message_media_hosted_content",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "e_tag",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "object_name",
                table: "message_media");

            migrationBuilder.DropColumn(
                name: "size_bytes",
                table: "message_media");

            migrationBuilder.RenameColumn(
                name: "uploaded_at",
                table: "message_media",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "bucket_name",
                table: "message_media",
                newName: "media_type");

            migrationBuilder.AlterColumn<string>(
                name: "graph_hosted_content_id",
                table: "message_media",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "content_type",
                table: "message_media",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AddColumn<string>(
                name: "checksum",
                table: "message_media",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "message_media",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "file_extension",
                table: "message_media",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                table: "message_media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "file_size",
                table: "message_media",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "graph_attachment_id",
                table: "message_media",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "relative_path",
                table: "message_media",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ux_message_media_hosted_content",
                table: "message_media",
                columns: new[] { "TeamsMessageId", "graph_hosted_content_id" },
                unique: true,
                filter: "\"graph_hosted_content_id\" IS NOT NULL");
        }
    }
}

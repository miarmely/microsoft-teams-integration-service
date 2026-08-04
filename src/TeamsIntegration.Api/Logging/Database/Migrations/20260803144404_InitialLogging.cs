using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeamsIntegration.Api.Logging.Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "application_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    category = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    event_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    exception_type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    exception_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    stack_trace = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    trace_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    span_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    request_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    properties_json = table.Column<string>(type: "jsonb", nullable: true),
                    environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    machine_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application_logs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application_logs");
        }
    }
}

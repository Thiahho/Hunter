using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduled_messages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    organization_id = table.Column<int>(type: "integer", nullable: false),
                    created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                    prospect_id = table.Column<int>(type: "integer", nullable: false),
                    message_template_id = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    run_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    message_id = table.Column<int>(type: "integer", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheduled_messages_message_templates_message_template_id",
                        column: x => x.message_template_id,
                        principalTable: "message_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_scheduled_messages_prospects_prospect_id",
                        column: x => x.prospect_id,
                        principalTable: "prospects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_messages_message_template_id",
                table: "scheduled_messages",
                column: "message_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_messages_organization_id_status_scheduled_at",
                table: "scheduled_messages",
                columns: new[] { "organization_id", "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_messages_prospect_id",
                table: "scheduled_messages",
                column: "prospect_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduled_messages");
        }
    }
}

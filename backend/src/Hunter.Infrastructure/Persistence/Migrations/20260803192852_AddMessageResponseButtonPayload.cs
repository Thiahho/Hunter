using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageResponseButtonPayload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "button_payload",
                table: "message_responses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "button_payload",
                table: "message_responses");
        }
    }
}

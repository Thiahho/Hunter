using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTelegramChatId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telegram_chat_id",
                table: "users",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telegram_chat_id",
                table: "users");
        }
    }
}

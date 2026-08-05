using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTelegramLinkCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telegram_link_code",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "telegram_link_code_expires_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "telegram_link_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "telegram_link_code_expires_at",
                table: "users");
        }
    }
}

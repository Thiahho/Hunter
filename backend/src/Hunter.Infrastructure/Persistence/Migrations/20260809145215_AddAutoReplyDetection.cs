using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoReplyDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "created_by_user_id",
                table: "scheduled_messages",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "scheduled_messages",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<int>(
                name: "auto_reply_attempts",
                table: "prospects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_follow_up_template",
                table: "message_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source",
                table: "scheduled_messages");

            migrationBuilder.DropColumn(
                name: "auto_reply_attempts",
                table: "prospects");

            migrationBuilder.DropColumn(
                name: "is_follow_up_template",
                table: "message_templates");

            migrationBuilder.AlterColumn<int>(
                name: "created_by_user_id",
                table: "scheduled_messages",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "area",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unassigned");

            migrationBuilder.CreateIndex(
                name: "ix_users_organization_id_area",
                table: "users",
                columns: new[] { "organization_id", "area" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_organization_id_area",
                table: "users");

            migrationBuilder.DropColumn(
                name: "area",
                table: "users");
        }
    }
}

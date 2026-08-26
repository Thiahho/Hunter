using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToFollowUpAndLeadActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable primero: las filas existentes no tienen forma de saber su organización
            // hasta que se hace el backfill de abajo. Un defaultValue fijo (ej. 0) las dejaría
            // con un organization_id que no matchea ninguna organización real, y el filtro global
            // nuevo (HunterDbContext) las volvería invisibles para todo el mundo.
            migrationBuilder.AddColumn<int>(
                name: "organization_id",
                table: "lead_activities",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "organization_id",
                table: "follow_ups",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE lead_activities SET organization_id = leads.organization_id " +
                "FROM leads WHERE leads.id = lead_activities.lead_id;");

            migrationBuilder.Sql(
                "UPDATE follow_ups SET organization_id = leads.organization_id " +
                "FROM leads WHERE leads.id = follow_ups.lead_id;");

            migrationBuilder.AlterColumn<int>(
                name: "organization_id",
                table: "lead_activities",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "organization_id",
                table: "follow_ups",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_lead_activities_organization_id_lead_id",
                table: "lead_activities",
                columns: new[] { "organization_id", "lead_id" });

            migrationBuilder.CreateIndex(
                name: "ix_follow_ups_organization_id_lead_id",
                table: "follow_ups",
                columns: new[] { "organization_id", "lead_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_lead_activities_organization_id_lead_id",
                table: "lead_activities");

            migrationBuilder.DropIndex(
                name: "ix_follow_ups_organization_id_lead_id",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "lead_activities");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "follow_ups");
        }
    }
}

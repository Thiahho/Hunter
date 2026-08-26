using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProspectAutomationSource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default explícito "OpenStreetMap" (no el default vacío que genera EF para un
            // enum-como-string): las filas existentes son todas búsquedas OSM (Apify recién se
            // puede programar a partir de este cambio), y un "" acá rompería la deserialización
            // de HasConversion<string>() al leerlas de vuelta.
            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "scheduled_prospect_automations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "OpenStreetMap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "source",
                table: "scheduled_prospect_automations");
        }
    }
}

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
            //
            // AddScheduledProspectAutomationSource (2026-08-13) ya había agregado esta misma
            // columna como integer en algunos entornos, antes de decidir representar Source como
            // string acá — residuo de un merge que dejó las dos migraciones. Un AddColumn liso
            // pisa una columna que ya existe (error 42701), así que se convierte en vez de agregar
            // cuando la columna ya está, preservando los valores 0/1 ya guardados.
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    col_type text;
                BEGIN
                    SELECT data_type INTO col_type
                    FROM information_schema.columns
                    WHERE table_name = 'scheduled_prospect_automations' AND column_name = 'source';

                    IF col_type IS NULL THEN
                        ALTER TABLE scheduled_prospect_automations
                            ADD COLUMN source character varying(20) NOT NULL DEFAULT 'OpenStreetMap';
                    ELSIF col_type = 'integer' THEN
                        ALTER TABLE scheduled_prospect_automations
                            ALTER COLUMN source TYPE character varying(20)
                            USING (CASE source WHEN 0 THEN 'OpenStreetMap' WHEN 1 THEN 'Apify' ELSE 'OpenStreetMap' END);
                        ALTER TABLE scheduled_prospect_automations ALTER COLUMN source SET DEFAULT 'OpenStreetMap';
                        ALTER TABLE scheduled_prospect_automations ALTER COLUMN source SET NOT NULL;
                    END IF;
                END $$;
            ");
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

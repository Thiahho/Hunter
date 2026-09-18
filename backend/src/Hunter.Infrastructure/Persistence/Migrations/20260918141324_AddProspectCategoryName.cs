using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hunter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProspectCategoryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category_name",
                table: "prospects",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            // Backfill de prospectos ya importados: el rubro sale de lo que quedó guardado en el
            // batch de importación.
            // 1) CSV/Excel: la columna "category" cruda (raw_data), si no es el nombre del enum.
            migrationBuilder.Sql("""
                UPDATE prospects p
                SET category_name = LEFT(TRIM(r.raw_data->>'category'), 150)
                FROM import_batch_records r
                JOIN import_batches b ON b.id = r.import_batch_id
                WHERE r.prospect_id = p.id
                  AND b.source_type = 'CsvImport'
                  AND p.category_name IS NULL
                  AND COALESCE(TRIM(r.raw_data->>'category'), '') <> ''
                  AND LOWER(TRIM(r.raw_data->>'category')) NOT IN
                      ('unknown', 'distributor', 'autopartsstore', 'workshop', 'lubricentro', 'tireshop', 'reseller', 'other');
                """);

            // 2) Apify: el rubro buscado está en file_name ("apify: {rubros} — {localidades}");
            //    solo se puede asignar cuando la búsqueda fue de un único rubro (sin coma).
            migrationBuilder.Sql("""
                UPDATE prospects p
                SET category_name = LEFT(TRIM(split_part(substring(b.file_name FROM 8), ' — ', 1)), 150)
                FROM import_batch_records r
                JOIN import_batches b ON b.id = r.import_batch_id
                WHERE r.prospect_id = p.id
                  AND b.file_name LIKE 'apify: %'
                  AND p.category_name IS NULL
                  AND POSITION(',' IN split_part(substring(b.file_name FROM 8), ' — ', 1)) = 0;
                """);

            // 3) Enum deducido del texto para los que quedaron en Unknown (mismos fragmentos que
            //    ProspectCategoryNames.Resolve, en el mismo orden de prioridad).
            migrationBuilder.Sql("""
                UPDATE prospects
                SET category = CASE
                    WHEN category_name ILIKE '%mayorista%' OR category_name ILIKE '%distribuidor%' THEN 'Distributor'
                    WHEN category_name ILIKE '%lubricentro%' THEN 'Lubricentro'
                    WHEN category_name ILIKE '%gomer_a%' OR category_name ILIKE '%neum_tico%' THEN 'TireShop'
                    WHEN category_name ILIKE '%repuesto%' OR category_name ILIKE '%autoparte%' THEN 'AutoPartsStore'
                    WHEN category_name ILIKE '%taller%' OR category_name ILIKE '%mec_nic%' THEN 'Workshop'
                    WHEN category_name ILIKE '%concesionari%' THEN 'Reseller'
                    ELSE category END
                WHERE category = 'Unknown' AND category_name IS NOT NULL;
                """);

            // 4) El resto (rubro de la lista fija sin texto libre): nombre en español del rubro
            //    (mismos nombres que ProspectCategoryNames.DisplayName).
            migrationBuilder.Sql("""
                UPDATE prospects
                SET category_name = CASE category
                    WHEN 'Distributor' THEN 'Mayorista/Distribuidor'
                    WHEN 'AutoPartsStore' THEN 'Casa de repuestos'
                    WHEN 'Workshop' THEN 'Taller'
                    WHEN 'Lubricentro' THEN 'Lubricentro'
                    WHEN 'TireShop' THEN 'Gomería'
                    WHEN 'Reseller' THEN 'Revendedor'
                    WHEN 'Other' THEN 'Otro' END
                WHERE category_name IS NULL AND category <> 'Unknown';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category_name",
                table: "prospects");
        }
    }
}

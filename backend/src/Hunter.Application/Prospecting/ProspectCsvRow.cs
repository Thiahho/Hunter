namespace Hunter.Application.Prospecting;

// Los nombres de propiedad calzan literalmente con las columnas esperadas del CSV
// (doc 21, sección 8): business_name, phone, whatsapp, email, address, city, province, category, source.
public sealed class ProspectCsvRow
{
    public string? business_name { get; set; }
    public string? phone { get; set; }
    public string? whatsapp { get; set; }
    public string? email { get; set; }
    public string? address { get; set; }
    public string? city { get; set; }
    public string? province { get; set; }
    public string? category { get; set; }
    public string? source { get; set; }
}

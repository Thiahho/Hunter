using System.Globalization;
using System.Text;
using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting;

// Nombre visible del rubro de un prospecto y resolución inversa desde texto libre (rubro
// seleccionado en la búsqueda, categoryName de Google Maps vía Apify, columna "category" de un
// CSV/Excel). Prospect.CategoryName guarda el texto tal cual; Prospect.Category es el enum
// cerrado que se usa para filtrar/rutear, así que se intenta deducir de ese mismo texto.
public static class ProspectCategoryNames
{
    private static readonly Dictionary<ProspectCategory, string> DisplayNames = new()
    {
        [ProspectCategory.Unknown] = "Sin clasificar",
        [ProspectCategory.Distributor] = "Mayorista/Distribuidor",
        [ProspectCategory.AutoPartsStore] = "Casa de repuestos",
        [ProspectCategory.Workshop] = "Taller",
        [ProspectCategory.Lubricentro] = "Lubricentro",
        [ProspectCategory.TireShop] = "Gomería",
        [ProspectCategory.Reseller] = "Revendedor",
        [ProspectCategory.Other] = "Otro"
    };

    // Fragmentos (sin acentos, en minúscula) que alcanzan para deducir el enum. El orden importa:
    // "mayorista de repuestos" es Distributor antes que AutoPartsStore.
    private static readonly (string Fragment, ProspectCategory Category)[] Fragments =
    [
        ("mayorista", ProspectCategory.Distributor),
        ("distribuidor", ProspectCategory.Distributor),
        ("lubricentro", ProspectCategory.Lubricentro),
        ("gomeria", ProspectCategory.TireShop),
        ("neumatico", ProspectCategory.TireShop),
        ("repuesto", ProspectCategory.AutoPartsStore),
        ("autoparte", ProspectCategory.AutoPartsStore),
        ("taller", ProspectCategory.Workshop),
        ("mecanic", ProspectCategory.Workshop),
        ("concesionari", ProspectCategory.Reseller),
    ];

    public static string DisplayName(ProspectCategory category) =>
        DisplayNames.GetValueOrDefault(category, category.ToString());

    // Nombre en español para guardar en Prospect.CategoryName cuando no hay texto libre de la
    // fuente. null para Unknown: "Sin clasificar" no es un rubro.
    public static string? StoredName(ProspectCategory category) =>
        category == ProspectCategory.Unknown ? null : DisplayName(category);

    // Rubro a mostrar: el texto libre guardado si hay, si no el nombre del enum.
    public static string DisplayName(ProspectCategory category, string? categoryName) =>
        string.IsNullOrWhiteSpace(categoryName) ? DisplayName(category) : categoryName;

    // Acepta el nombre del enum ("AutoPartsStore"), su nombre visible ("Casa de repuestos") o
    // cualquier texto que contenga uno de los fragmentos conocidos. Unknown si no matchea nada.
    public static ProspectCategory Resolve(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ProspectCategory.Unknown;

        var trimmed = text.Trim();
        if (Enum.TryParse<ProspectCategory>(trimmed, ignoreCase: true, out var parsed) && !int.TryParse(trimmed, out _))
            return parsed;

        var normalized = RemoveAccents(trimmed).ToLowerInvariant();
        foreach (var (category, name) in DisplayNames)
            if (RemoveAccents(name).ToLowerInvariant() == normalized)
                return category;

        foreach (var (fragment, category) in Fragments)
            if (normalized.Contains(fragment))
                return category;

        return ProspectCategory.Unknown;
    }

    private static string RemoveAccents(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}

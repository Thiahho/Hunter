using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting;

// Normaliza valores de contacto para deduplicación consistente. No es una validación
// telefónica completa (no maneja el "9" móvil argentino ni el prefijo "15"):
// alcanza para el objetivo del MVP de no tratar +5491112345678 / 01112345678 como
// prospectos distintos. Validación avanzada queda para V2 (doc 12, P2).
public static class ContactValueNormalizer
{
    public static string Normalize(ProspectContactChannel channel, string rawValue)
    {
        var value = rawValue.Trim();

        return channel switch
        {
            ProspectContactChannel.Phone or ProspectContactChannel.Whatsapp => NormalizePhone(value),
            ProspectContactChannel.Email => value.ToLowerInvariant(),
            ProspectContactChannel.Instagram or ProspectContactChannel.Facebook => value.TrimStart('@').ToLowerInvariant(),
            _ => value
        };
    }

    private static string NormalizePhone(string rawValue)
    {
        var digits = new string(rawValue.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("00"))
            digits = digits[2..];

        if (digits.StartsWith("54"))
            return digits;

        if (digits.StartsWith('0'))
            digits = digits[1..];

        return digits.Length == 0 ? digits : "54" + digits;
    }
}

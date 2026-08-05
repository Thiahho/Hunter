namespace Hunter.Application.Prospecting;

// Ayuda separada de ContactValueNormalizer a propósito: ese normalizador documenta
// explícitamente que NO maneja el "9" móvil argentino (ver ContactValueNormalizer.cs líneas
// 6-7), así que tocarlo para detectar celulares cambiaría el comportamiento de las rutas CSV y
// Google Places existentes. OpenStreetMap sí necesita esta distinción: guarda muchos teléfonos
// fijos, y registrarlos como contacto de WhatsApp generaría envíos que fallan en producción.
public static class ArgentineMobileDetector
{
    // Regla conservadora: solo true para el formato normalizado completo del celular argentino
    // (54 + 9 + código de área + número, 13 dígitos en total, ej. 5491122692061). Un falso
    // negativo acá solo pierde un contacto de WhatsApp; un falso positivo genera envíos que
    // Meta rechaza, así que ante la duda se prefiere no marcarlo.
    public static bool IsWhatsAppCapable(string normalizedPhone) =>
        normalizedPhone.Length == 13 && normalizedPhone.StartsWith("549", StringComparison.Ordinal);

    // Mismo número sin el "9" móvil, tal como se lo suele escribir a mano (ContactValueNormalizer
    // no lo inserta a propósito). Null si no aplica el patrón de celular argentino. Usado para
    // reintentar el matching de un inbound real de Meta (que siempre trae el "9") contra un
    // contacto guardado sin él, sin tocar cómo se normaliza/guarda en el resto del sistema.
    public static string? WithoutMobilePrefix(string normalizedPhone) =>
        IsWhatsAppCapable(normalizedPhone) ? "54" + normalizedPhone[3..] : null;
}

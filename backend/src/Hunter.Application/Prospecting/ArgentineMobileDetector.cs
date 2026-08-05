namespace Hunter.Application.Prospecting;

// Ayuda separada de ContactValueNormalizer a propósito: ese normalizador documenta
// explícitamente que NO maneja el "9" móvil argentino (ver ContactValueNormalizer.cs líneas
// 6-7), así que tocarlo para detectar celulares cambiaría el comportamiento de las rutas CSV y
// Google Places existentes. OpenStreetMap sí necesita esta distinción: guarda muchos teléfonos
// fijos, y registrarlos como contacto de WhatsApp generaría envíos que fallan en producción.
public static class ArgentineMobileDetector
{
    // Regla conservadora: solo true para el formato normalizado completo del celular argentino
    // (54 + 9 + código de área + número, 13 dígitos en total, ej. 5491122602000). Un falso
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

    // Variante permisiva para fuentes como OpenStreetMap, donde el "9" casi nunca está cargado
    // (no es algo que un mapper piense al anotar un teléfono): asume que cualquier número con
    // forma de argentino completo (54 + código de área + número, 12 dígitos, sin "9") es un
    // celular y le inserta el "9". A diferencia de IsWhatsAppCapable, acá se prioriza no perder
    // leads reales por sobre el riesgo de un envío fallido contra algún fijo real que se cuele
    // — ese fallo queda visible en Mensajes > Enviados, ya no es un silencio total. Devuelve el
    // propio normalizedPhone si ya tenía el "9", o null si ni siquiera tiene forma de teléfono
    // argentino con código de país.
    public static string? AssumeWhatsAppCapable(string normalizedPhone)
    {
        if (IsWhatsAppCapable(normalizedPhone))
            return normalizedPhone;

        return normalizedPhone.Length == 12 && normalizedPhone.StartsWith("54", StringComparison.Ordinal)
            ? "549" + normalizedPhone[2..]
            : null;
    }
}

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
}

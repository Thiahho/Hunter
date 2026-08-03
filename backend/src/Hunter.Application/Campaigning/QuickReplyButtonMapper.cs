namespace Hunter.Application.Campaigning;

// Payload del único botón quick_reply de la plantilla de bienvenida ("Estoy interesado").
// ASCII, sin espacios ni acentos: es un valor opaco para Meta, pero mantenerlo así evita
// cualquier sorpresa de codificación y desacopla el texto visible del botón (que marketing
// puede reescribir) del código que decide la clasificación.
public static class QuickReplyPayloads
{
    public const string Interested = "ESTOY_INTERESADO";
}

// Detecta si un tap de botón quick-reply es una señal de interés. No mapea a rubro: con un
// solo botón genérico ya no hay forma de que el prospecto se autodeclare mayorista/casa de
// repuestos desde acá (ver docs de la feature de derivación por rubro, que quedó reducida a
// esto). El rubro para el ruteo Administración/Ventas sigue viniendo de Prospect.Category tal
// cual esté cargado (import, carga manual, etc.), no del tap.
public static class QuickReplyButtonMapper
{
    public static bool IsInterestTap(string? buttonPayload, string? buttonText)
    {
        var payload = buttonPayload?.Trim().ToUpperInvariant();
        if (payload == QuickReplyPayloads.Interested)
            return true;

        // Fallback por texto: cubre el caso en que el envío no incluyó componentes de payload
        // (Meta entonces echa la etiqueta visible como "payload"), o el payload configurado no
        // coincide con la constante.
        var text = buttonText?.Trim().ToLowerInvariant();
        return !string.IsNullOrEmpty(text) && text.Contains("interes");
    }
}

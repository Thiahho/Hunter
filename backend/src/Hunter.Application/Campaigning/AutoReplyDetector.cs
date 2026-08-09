using Hunter.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Application.Campaigning;

// Heurística por reglas, mismo criterio que KeywordIntentClassifier (placeholder hasta decidir
// proveedor de IA, doc 12 §27): dos señales combinadas por OR.
//
// 1) Frases típicas de auto-responder de WhatsApp Business en español (saludo / fuera de horario).
// 2) El mismo texto exacto ya llegó como respuesta de otros prospectos distintos de la misma
//    organización: un mensaje enlatado se repite palabra por palabra entre negocios distintos, algo
//    que una respuesta humana real prácticamente nunca hace. Se auto-adapta a los mensajes reales
//    de cada organización sin depender solo de la lista fija de frases.
public class AutoReplyDetector(IHunterDbContext db) : IAutoReplyDetector
{
    // Cuántos prospectos distintos tienen que haber contestado el mismo texto exacto para
    // considerarlo enlatado en vez de una coincidencia humana casual.
    private const int DuplicateProspectThreshold = 2;

    // Evita falsos positivos por señal de duplicado con saludos cortos genéricos ("hola", "si"):
    // los mensajes enlatados de auto-responder son oraciones completas, no una palabra suelta.
    private const int MinContentLengthForDuplicateSignal = 20;

    private static readonly string[] AutoReplyPhrases =
    [
        "mensaje automático", "respuesta automática", "este es un mensaje generado automáticamente",
        "en este momento no podemos atenderte", "en este momento no puedo atenderte",
        "fuera de nuestro horario de atención", "estamos fuera de horario",
        "gracias por comunicarte con", "gracias por contactarte con", "gracias por escribirnos",
        "en breve nos pondremos en contacto", "te responderemos a la brevedad",
        "a la brevedad posible", "nuestro horario de atención es", "pronto te responderemos",
        "un asesor te va a contactar", "en instantes te va a atender"
    ];

    public async Task<bool> IsLikelyAutomatedAsync(int organizationId, string content, CancellationToken ct = default)
    {
        var normalized = content.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            return false;

        if (AutoReplyPhrases.Any(normalized.Contains))
            return true;

        if (normalized.Length < MinContentLengthForDuplicateSignal)
            return false;

        var distinctProspectsWithSameText = await db.MessageResponses.IgnoreQueryFilters()
            .Where(r => r.OrganizationId == organizationId && r.Content.Trim().ToLower() == normalized)
            .Select(r => r.ProspectId)
            .Distinct()
            .CountAsync(ct);

        return distinctProspectsWithSameText >= DuplicateProspectThreshold;
    }
}

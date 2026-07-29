using Hunter.Application.Campaigning;
using Hunter.Domain.Campaigning;

namespace Hunter.Infrastructure.Messaging;

// Placeholder hasta que se cierre la decisión de proveedor de IA (doc 12, sección 27).
// Reglas de palabras clave calcadas de los ejemplos del doc 08 (sección 12) y
// doc 13 (sección 37, para el caso "sí, pero no me interesa").
public class KeywordIntentClassifier : IIntentClassifier
{
    private const string ModelName = "keyword-rules-v1";
    private const string PromptVersion = "n/a";

    private static readonly string[] StopPhrases =
    [
        "no me contacten", "no me escriban", "borrame", "no quiero recibir",
        "sacame de la lista", "no molesten", "dejen de escribirme", "no contactar"
    ];

    private static readonly string[] NotInterestedPhrases =
    [
        "no me interesa", "no me interesan", "gracias pero no", "no gracias",
        "ya tengo proveedor", "no por ahora", "no necesito"
    ];

    private static readonly string[] InterestedPhrases =
    [
        "me interesa", "pasame", "pasame informacion", "pasame información", "mandame",
        "dale", "quiero saber mas", "quiero saber más", "quiero comprar", "si, claro", "sí, claro"
    ];

    public Task<IntentClassificationResult> ClassifyAsync(string content, CancellationToken ct = default)
    {
        var normalized = content.Trim().ToLowerInvariant();

        if (ContainsAny(normalized, StopPhrases))
            return Result(IntentClassification.Stop, 0.95m);

        if (ContainsAny(normalized, NotInterestedPhrases))
            return Result(IntentClassification.NotInterested, 0.90m);

        if (ContainsAny(normalized, InterestedPhrases))
            return Result(IntentClassification.Interested, 0.90m);

        if (normalized.Contains('?'))
            return Result(IntentClassification.Question, 0.85m);

        if (normalized is "si" or "sí" or "dale" or "ok" or "bueno")
            return Result(IntentClassification.Interested, 0.60m);

        return Result(IntentClassification.Unclear, 0.40m);
    }

    private static bool ContainsAny(string content, string[] phrases) =>
        phrases.Any(content.Contains);

    private static Task<IntentClassificationResult> Result(IntentClassification classification, decimal confidence) =>
        Task.FromResult(new IntentClassificationResult(classification, confidence, ModelName, PromptVersion));
}

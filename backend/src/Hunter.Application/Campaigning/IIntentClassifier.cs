using Hunter.Domain.Campaigning;

namespace Hunter.Application.Campaigning;

public record IntentClassificationResult(IntentClassification Classification, decimal Confidence, string ModelName, string PromptVersion);

// Abstracción de clasificación de intención (doc 07 Epic08). Todavía no hay proveedor
// de IA decidido (doc 12, sección 27) — Infrastructure registra un clasificador basado
// en reglas de palabras clave (doc 08, sección 12) hasta que esa decisión se cierre.
public interface IIntentClassifier
{
    Task<IntentClassificationResult> ClassifyAsync(string content, CancellationToken ct = default);
}

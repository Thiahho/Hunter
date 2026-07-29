using Hunter.Application.Campaigning;
using Microsoft.Extensions.Logging;

namespace Hunter.Infrastructure.Messaging;

// Placeholder hasta que se cierre la decisión de proveedor de WhatsApp/Meta Cloud API
// (doc 12, sección 27). No envía nada real: solo registra y simula un envío exitoso,
// para que el resto del pipeline (Campaign -> Message -> estado) sea probable end-to-end.
public class StubMessageProvider(ILogger<StubMessageProvider> logger) : IMessageProvider
{
    public string ProviderName => "stub";

    public Task<SendMessageResult> SendAsync(SendMessageRequest request, CancellationToken ct = default)
    {
        var externalMessageId = $"stub-{Guid.NewGuid():N}";

        logger.LogInformation(
            "[StubMessageProvider] {Channel} -> {Contact}: {Content} (externalMessageId={ExternalMessageId})",
            request.Channel, request.ToContact, request.Content, externalMessageId);

        return Task.FromResult(new SendMessageResult(true, externalMessageId, null));
    }
}

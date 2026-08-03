using Hunter.Application.Campaigning;

namespace Hunter.Tests.TestSupport;

// StubMessageProvider (Infrastructure) solo loguea, no hay forma de asertar contra qué se
// mandó. Este helper de test graba cada SendMessageRequest para poder verificar destino,
// contenido y plantilla usados (ej. la notificación de derivación al vendedor).
public class RecordingMessageProvider : IMessageProvider
{
    public List<SendMessageRequest> SentRequests { get; } = [];

    public string ProviderName => "recording-test-provider";

    public string? HandoffTemplateName { get; set; }

    // Permite simular un fallo de envío (ej. error 131047 fuera de la ventana de 24hs) o una
    // excepción de red, sin que eso deba romper el procesamiento del webhook que lo dispara.
    public bool NextSendSucceeds { get; set; } = true;
    public string? NextSendError { get; set; }
    public bool ThrowOnNextSend { get; set; }

    public Task<SendMessageResult> SendAsync(SendMessageRequest request, CancellationToken ct = default)
    {
        SentRequests.Add(request);

        if (ThrowOnNextSend)
            throw new InvalidOperationException("Fallo simulado de proveedor de mensajería.");

        return Task.FromResult(NextSendSucceeds
            ? new SendMessageResult(true, $"test-{Guid.NewGuid():N}", null)
            : new SendMessageResult(false, null, NextSendError ?? "Fallo simulado."));
    }
}

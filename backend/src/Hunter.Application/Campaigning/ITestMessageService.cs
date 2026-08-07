using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface ITestMessageService
{
    Task<Result<TestMessageResultDto>> SendAsync(int prospectId, SendTestMessageRequest request, CancellationToken ct = default);

    // Reintenta un envío individual que falló (Message.CampaignId == null): reenvía el mismo
    // contenido al mismo prospecto. Los mensajes de campaña se reintentan por el flujo de
    // CampaignRecipient (ver CampaignService.RetryRecipientsAsync), no por acá.
    Task<Result<TestMessageResultDto>> RetryAsync(int messageId, CancellationToken ct = default);
}

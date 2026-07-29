using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface IInboundMessageService
{
    Task<Result<InboundMessageResultDto>> ProcessAsync(InboundMessageRequest request, CancellationToken ct = default);
}

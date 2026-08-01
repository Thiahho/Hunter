using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

public interface ITestMessageService
{
    Task<Result<TestMessageResultDto>> SendAsync(int prospectId, SendTestMessageRequest request, CancellationToken ct = default);
}

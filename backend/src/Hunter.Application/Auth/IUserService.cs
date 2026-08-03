using Hunter.Application.Auth.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Auth;

public interface IUserService
{
    Task<Result<IReadOnlyCollection<UserDto>>> ListAsync(int organizationId, CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(int organizationId, CreateUserRequest request, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateAsync(int organizationId, int userId, UpdateUserRequest request, CancellationToken ct = default);
}

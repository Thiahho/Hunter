using Hunter.Domain.Identity;

namespace Hunter.Application.Auth;

public interface IPasswordHasher
{
    string Hash(User user, string password);
    bool Verify(User user, string hash, string password);
}

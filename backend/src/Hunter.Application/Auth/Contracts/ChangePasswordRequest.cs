namespace Hunter.Application.Auth.Contracts;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

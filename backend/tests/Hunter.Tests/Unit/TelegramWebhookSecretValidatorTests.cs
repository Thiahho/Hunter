using Hunter.Infrastructure.Messaging;

namespace Hunter.Tests.Unit;

public class TelegramWebhookSecretValidatorTests
{
    [Fact]
    public void IsValid_MatchingSecret_ReturnsTrue()
    {
        Assert.True(TelegramWebhookSecretValidator.IsValid("mi-secreto", "mi-secreto"));
    }

    [Fact]
    public void IsValid_WrongSecret_ReturnsFalse()
    {
        Assert.False(TelegramWebhookSecretValidator.IsValid("otro-valor", "mi-secreto"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_MissingProvidedSecret_ReturnsFalse(string? provided)
    {
        Assert.False(TelegramWebhookSecretValidator.IsValid(provided, "mi-secreto"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsValid_MissingExpectedSecret_ReturnsFalse(string? expected)
    {
        Assert.False(TelegramWebhookSecretValidator.IsValid("cualquier-cosa", expected));
    }
}

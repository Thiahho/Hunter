using Hunter.Application.Prospecting;

namespace Hunter.Tests.Unit;

public class ArgentineMobileDetectorTests
{
    [Theory]
    [InlineData("5491122692061")] // celular normalizado (54 + 9 + área + número, 13 dígitos)
    public void IsWhatsAppCapable_MobileNumber_ReturnsTrue(string normalizedPhone)
    {
        Assert.True(ArgentineMobileDetector.IsWhatsAppCapable(normalizedPhone));
    }

    [Theory]
    [InlineData("541141234567")]   // fijo normalizado (54 + área + número, sin el "9", 12 dígitos)
    [InlineData("54")]             // demasiado corto
    [InlineData("")]               // vacío
    [InlineData("11122692061")]    // sin el prefijo de país 54
    public void IsWhatsAppCapable_NonMobileOrMalformed_ReturnsFalse(string normalizedPhone)
    {
        Assert.False(ArgentineMobileDetector.IsWhatsAppCapable(normalizedPhone));
    }

    [Fact]
    public void WithoutMobilePrefix_MobileNumber_RemovesThe9()
    {
        Assert.Equal("541122692061", ArgentineMobileDetector.WithoutMobilePrefix("5491122692061"));
    }

    [Theory]
    [InlineData("541141234567")] // ya no tiene el "9": no aplica
    [InlineData("11122692061")]  // sin prefijo de país: no aplica
    [InlineData("")]
    public void WithoutMobilePrefix_NonMobile_ReturnsNull(string normalizedPhone)
    {
        Assert.Null(ArgentineMobileDetector.WithoutMobilePrefix(normalizedPhone));
    }
}

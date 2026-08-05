using Hunter.Application.Prospecting;

namespace Hunter.Tests.Unit;

public class ArgentineMobileDetectorTests
{
    [Theory]
    [InlineData("5491122602000")] // celular normalizado (54 + 9 + área + número, 13 dígitos)
    public void IsWhatsAppCapable_MobileNumber_ReturnsTrue(string normalizedPhone)
    {
        Assert.True(ArgentineMobileDetector.IsWhatsAppCapable(normalizedPhone));
    }

    [Theory]
    [InlineData("541141234567")]   // fijo normalizado (54 + área + número, sin el "9", 12 dígitos)
    [InlineData("54")]             // demasiado corto
    [InlineData("")]               // vacío
    [InlineData("1122602000")]    // sin el prefijo de país 54
    public void IsWhatsAppCapable_NonMobileOrMalformed_ReturnsFalse(string normalizedPhone)
    {
        Assert.False(ArgentineMobileDetector.IsWhatsAppCapable(normalizedPhone));
    }

    [Fact]
    public void WithoutMobilePrefix_MobileNumber_RemovesThe9()
    {
        Assert.Equal("541122602000", ArgentineMobileDetector.WithoutMobilePrefix("5491122602000"));
    }

    [Theory]
    [InlineData("541141234567")] // ya no tiene el "9": no aplica
    [InlineData("1122602000")]  // sin prefijo de país: no aplica
    [InlineData("")]
    public void WithoutMobilePrefix_NonMobile_ReturnsNull(string normalizedPhone)
    {
        Assert.Null(ArgentineMobileDetector.WithoutMobilePrefix(normalizedPhone));
    }

    [Fact]
    public void AssumeWhatsAppCapable_AlreadyHasThe9_ReturnsAsIs()
    {
        Assert.Equal("5491122602000", ArgentineMobileDetector.AssumeWhatsAppCapable("5491122602000"));
    }

    [Fact]
    public void AssumeWhatsAppCapable_ArgentineNumberWithoutThe9_InsertsIt()
    {
        // Caso real de OSM: "54" + área + número, 12 dígitos, sin "9" — se asume celular.
        Assert.Equal("5491122602000", ArgentineMobileDetector.AssumeWhatsAppCapable("541122602000"));
    }

    [Theory]
    [InlineData("1122602000")] // sin prefijo de país: no tiene forma de argentino completo
    [InlineData("54")]          // demasiado corto
    [InlineData("")]
    public void AssumeWhatsAppCapable_NotArgentineShape_ReturnsNull(string normalizedPhone)
    {
        Assert.Null(ArgentineMobileDetector.AssumeWhatsAppCapable(normalizedPhone));
    }
}

using Hunter.Application.Prospecting;
using Hunter.Domain.Prospecting;

namespace Hunter.Tests.Unit;

public class ContactValueNormalizerTests
{
    [Theory]
    [InlineData("011 15 1234-5678", "54111512345678")]
    [InlineData("01112345678", "541112345678")]
    [InlineData("+54 9 11 1512-345678", "549111512345678")]
    public void Normalize_Phone_StripsFormattingAndAppliesCountryCode(string raw, string expected)
    {
        var result = ContactValueNormalizer.Normalize(ProspectContactChannel.Phone, raw);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_Email_TrimsAndLowercases()
    {
        var result = ContactValueNormalizer.Normalize(ProspectContactChannel.Email, "  CONTACTO@Empresa.COM  ");

        Assert.Equal("contacto@empresa.com", result);
    }

    [Fact]
    public void Normalize_Instagram_StripsLeadingAtAndLowercases()
    {
        var result = ContactValueNormalizer.Normalize(ProspectContactChannel.Instagram, "@MiNegocio");

        Assert.Equal("minegocio", result);
    }

    [Fact]
    public void Normalize_SamePhoneDifferentFormat_ProducesSameCanonicalValue()
    {
        var a = ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, "011 15 1234-5678");
        var b = ContactValueNormalizer.Normalize(ProspectContactChannel.Whatsapp, "0111512345678");

        Assert.Equal(a, b);
    }
}

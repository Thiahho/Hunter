using Hunter.Application.Campaigning;

namespace Hunter.Tests.Unit;

public class QuickReplyButtonMapperTests
{
    [Fact]
    public void IsInterestTap_KnownPayload_ReturnsTrue()
    {
        Assert.True(QuickReplyButtonMapper.IsInterestTap(QuickReplyPayloads.Interested, "Estoy interesado"));
    }

    [Fact]
    public void IsInterestTap_PayloadMatchIsCaseInsensitive()
    {
        Assert.True(QuickReplyButtonMapper.IsInterestTap("estoy_interesado", "Estoy interesado"));
    }

    [Theory]
    [InlineData("Estoy interesado")]
    [InlineData("ESTOY INTERESADA")]
    [InlineData("Me interesa")]
    public void IsInterestTap_UnknownPayload_FallsBackToText(string text)
    {
        // El payload no coincide con la constante (ej. plantilla reenviada desde el panel de
        // Meta sin componentes de payload), pero el texto visible sigue siendo reconocible.
        Assert.True(QuickReplyButtonMapper.IsInterestTap("some_other_payload", text));
    }

    [Fact]
    public void IsInterestTap_NullPayload_FallsBackToText()
    {
        Assert.True(QuickReplyButtonMapper.IsInterestTap(null, "Estoy interesado"));
    }

    [Fact]
    public void IsInterestTap_UnrecognizedPayloadAndText_ReturnsFalse()
    {
        Assert.False(QuickReplyButtonMapper.IsInterestTap("otro_payload", "Quiero mas info"));
    }

    [Fact]
    public void IsInterestTap_NullPayloadAndNullText_ReturnsFalse()
    {
        Assert.False(QuickReplyButtonMapper.IsInterestTap(null, null));
    }
}

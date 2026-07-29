using Hunter.Domain.Campaigning;
using Hunter.Infrastructure.Messaging;

namespace Hunter.Tests.Unit;

public class KeywordIntentClassifierTests
{
    private readonly KeywordIntentClassifier _classifier = new();

    [Theory]
    [InlineData("Sí, pasame información")]
    [InlineData("dale, me interesa")]
    [InlineData("mandame el catalogo")]
    public async Task Classify_ClearInterestPhrases_ReturnsInterestedAboveThreshold(string message)
    {
        var result = await _classifier.ClassifyAsync(message);

        Assert.Equal(IntentClassification.Interested, result.Classification);
        Assert.True(result.Confidence >= 0.80m);
    }

    [Theory]
    [InlineData("No me interesa, gracias")]
    [InlineData("ya tengo proveedor")]
    public async Task Classify_ClearRejectionPhrases_ReturnsNotInterested(string message)
    {
        var result = await _classifier.ClassifyAsync(message);

        Assert.Equal(IntentClassification.NotInterested, result.Classification);
    }

    [Fact]
    public async Task Classify_SiPeroNoMeInteresa_ReturnsNotInterested_NotInterested()
    {
        // Caso trampa documentado explícitamente en doc 13, sección 37:
        // el "sí" inicial no debe hacer que se clasifique como INTERESTED.
        var result = await _classifier.ClassifyAsync("si, pero no me interesa");

        Assert.Equal(IntentClassification.NotInterested, result.Classification);
    }

    [Theory]
    [InlineData("no me contacten mas")]
    [InlineData("borrame de la lista")]
    [InlineData("no quiero recibir mas mensajes")]
    public async Task Classify_OptOutPhrases_ReturnsStop(string message)
    {
        var result = await _classifier.ClassifyAsync(message);

        Assert.Equal(IntentClassification.Stop, result.Classification);
    }

    [Fact]
    public async Task Classify_QuestionMark_ReturnsQuestion()
    {
        var result = await _classifier.ClassifyAsync("Que marcas manejan?");

        Assert.Equal(IntentClassification.Question, result.Classification);
    }

    [Fact]
    public async Task Classify_AmbiguousMessage_ReturnsUnclearBelowThreshold()
    {
        var result = await _classifier.ClassifyAsync("che como andas todo bien");

        Assert.Equal(IntentClassification.Unclear, result.Classification);
        Assert.True(result.Confidence < 0.80m);
    }
}

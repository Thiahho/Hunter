namespace Hunter.Infrastructure.Prospecting;

public class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";

    public string? ApiKey { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

namespace Hunter.Infrastructure.Prospecting;

public class ApifyOptions
{
    public const string SectionName = "Apify";

    public string? ApiToken { get; set; }

    // Formato username~actorName (el "/" de "compass/crawler-google-places" no es válido en un
    // path de URL, así que la API de Apify usa "~" como separador ahí).
    public string ActorId { get; set; } = "compass~crawler-google-places";

    // Debajo del límite duro de 300s del endpoint run-sync-get-dataset-items (ver
    // ApifyGoogleMapsClient): si el run no llegó a terminar, Apify devuelve el objeto del run
    // (status Running) en vez del dataset, y el margen de 20s es para no cortar nosotros la
    // conexión antes de que llegue esa respuesta.
    public int TimeoutSeconds { get; set; } = 280;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiToken);
}

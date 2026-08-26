namespace Hunter.Domain.Prospecting;

public enum ProspectStatus
{
    New,
    Validated,
    Ready,
    Contacted,
    Responded,
    NotInterested,
    NoResponse,
    Lead,
    Customer,
    Suppressed,
    Invalid,
    AutoReplyDetected
}

public enum ProspectCategory
{
    Unknown,
    Distributor,
    AutoPartsStore,
    Workshop,
    Lubricentro,
    TireShop,
    Reseller,
    Other
}

public enum BusinessSize
{
    Unknown,
    Micro,
    Small,
    Medium,
    Large
}

public enum RecurrencePotential
{
    Unknown,
    Low,
    Medium,
    High
}

public enum DistanceCategory
{
    Unknown,
    Local,
    Near,
    Medium,
    Far
}

public enum DataQuality
{
    D,
    C,
    B,
    A
}

public enum OperationalPriority
{
    PriorityD,
    PriorityC,
    PriorityB,
    PriorityA
}

public enum ProspectContactChannel
{
    Phone,
    Whatsapp,
    Email,
    Instagram,
    Facebook
}

public enum ProspectSourceType
{
    GooglePlaces,
    OpenStreetMap,
    Directory,
    PublicWebsite,
    CsvImport,
    Manual,
    ExternalApi,
    Other
}

// Fuente que usa una ScheduledProspectAutomation para buscar (ver ScheduledProspectAutomation.Source):
// distinto de ProspectSourceType (que clasifica el origen de un ImportBatch ya importado) porque acá
// solo hace falta distinguir entre las dos fuentes que ImportService sabe automatizar sin revisión
// humana (OpenStreetMap/Nominatim, gratis; Apify/Google Maps, pago) — no las 8 categorías de
// ProspectSourceType.
public enum ProspectAutomationSource
{
    OpenStreetMap,
    Apify
}

public enum ImportBatchStatus
{
    Processing,
    Preview,
    Confirmed,
    Completed,
    Failed,
    Cancelled
}

public enum ImportBatchRecordStatus
{
    Valid,
    Duplicate,
    Invalid,
    Suppressed,
    Imported
}

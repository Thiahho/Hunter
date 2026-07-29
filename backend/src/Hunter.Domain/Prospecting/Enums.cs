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
    Invalid
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

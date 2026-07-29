namespace Hunter.Domain.Compliance;

public enum SuppressionContactType
{
    Phone,
    Whatsapp,
    Email
}

public enum SuppressionReason
{
    UserRequested,
    InvalidNumber,
    Blocked,
    Manual,
    Other
}

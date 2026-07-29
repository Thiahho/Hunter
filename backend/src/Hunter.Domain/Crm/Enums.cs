namespace Hunter.Domain.Crm;

public enum LeadStatus
{
    New,
    InProgress,
    Won,
    Lost
}

public enum LeadPriority
{
    Low,
    Medium,
    High
}

public enum LostReason
{
    Price,
    NoStock,
    ExistingSupplier,
    NoResponse,
    NotInterested,
    Logistics,
    CommercialConditions,
    Other
}

public enum LeadActivityType
{
    Contact,
    Call,
    Whatsapp,
    Quote,
    Note,
    FollowUp
}

public enum FollowUpStatus
{
    Pending,
    Completed,
    Cancelled
}

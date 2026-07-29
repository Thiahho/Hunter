namespace Hunter.Domain.Campaigning;

public enum MessagingChannel
{
    Whatsapp,
    Email,
    Telegram,
    Sms
}

public enum CampaignStatus
{
    Draft,
    Ready,
    Running,
    Paused,
    Completed,
    Cancelled
}

public enum CampaignRecipientStatus
{
    Pending,
    Queued,
    Sent,
    Delivered,
    Responded,
    Interested,
    NotInterested,
    Stopped,
    Skipped,
    Failed,
    Completed
}

public enum MessageStatus
{
    Pending,
    Sent,
    Delivered,
    Read,
    Failed,
    Cancelled
}

public enum IntentClassification
{
    Interested,
    NotInterested,
    Question,
    Unclear,
    Stop
}

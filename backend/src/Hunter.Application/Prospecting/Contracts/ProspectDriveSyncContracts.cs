namespace Hunter.Application.Prospecting.Contracts;

public record ProspectDriveSyncResultDto(string FileId, string DriveUrl, DateTimeOffset SyncedAt, int ProspectCount);

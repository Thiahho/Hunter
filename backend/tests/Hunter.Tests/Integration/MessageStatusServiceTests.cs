using Hunter.Application.Campaigning;
using Hunter.Domain.Campaigning;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Los eventos de estado de WhatsApp (webhook de Meta) pueden llegar duplicados o fuera de
// orden por reintentos; el avance de estado debe ser monótono y nunca "retroceder".
public class MessageStatusServiceTests
{
    private static async Task<(string dbName, int messageId)> SeedSentMessageAsync(string externalMessageId)
    {
        var dbName = TestDb.NewDbName();
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var prospect = new Prospect { OrganizationId = org.Id, BusinessName = "Repuestos Oeste" };
        db.Prospects.Add(prospect);
        await db.SaveChangesAsync();

        var message = new Message
        {
            OrganizationId = org.Id,
            ProspectId = prospect.Id,
            Channel = MessagingChannel.Whatsapp,
            Provider = "whatsapp_cloud_api",
            Content = "hola",
            ExternalMessageId = externalMessageId,
            Status = MessageStatus.Sent,
            SentAt = DateTimeOffset.UtcNow
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync();

        return (dbName, message.Id);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_SentToDelivered_UpdatesStatusAndTimestamp()
    {
        var (dbName, _) = await SeedSentMessageAsync("wamid.1");

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new MessageStatusService(db);

        var deliveredAt = DateTimeOffset.UtcNow;
        await service.UpdateDeliveryStatusAsync("wamid.1", MessageStatus.Delivered, deliveredAt);

        var message = await db.Messages.IgnoreQueryFilters().FirstAsync(m => m.ExternalMessageId == "wamid.1");
        Assert.Equal(MessageStatus.Delivered, message.Status);
        Assert.Equal(deliveredAt, message.DeliveredAt);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_DeliveredThenRead_ProgressesCorrectly()
    {
        var (dbName, _) = await SeedSentMessageAsync("wamid.2");

        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            await new MessageStatusService(db).UpdateDeliveryStatusAsync("wamid.2", MessageStatus.Delivered, DateTimeOffset.UtcNow);
        }

        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            await new MessageStatusService(db).UpdateDeliveryStatusAsync("wamid.2", MessageStatus.Read, DateTimeOffset.UtcNow);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: null);
        var message = await assertDb.Messages.IgnoreQueryFilters().FirstAsync(m => m.ExternalMessageId == "wamid.2");
        Assert.Equal(MessageStatus.Read, message.Status);
        Assert.NotNull(message.DeliveredAt);
        Assert.NotNull(message.ReadAt);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_LateSentAfterRead_DoesNotDowngrade()
    {
        var (dbName, _) = await SeedSentMessageAsync("wamid.3");

        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            await new MessageStatusService(db).UpdateDeliveryStatusAsync("wamid.3", MessageStatus.Read, DateTimeOffset.UtcNow);
        }

        // Reintento tardío de webhook con un "delivered" que llega después del "read".
        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            await new MessageStatusService(db).UpdateDeliveryStatusAsync("wamid.3", MessageStatus.Delivered, DateTimeOffset.UtcNow);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: null);
        var message = await assertDb.Messages.IgnoreQueryFilters().FirstAsync(m => m.ExternalMessageId == "wamid.3");
        Assert.Equal(MessageStatus.Read, message.Status);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_Failed_SetsFailureReason()
    {
        var (dbName, _) = await SeedSentMessageAsync("wamid.4");

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new MessageStatusService(db);

        await service.UpdateDeliveryStatusAsync("wamid.4", MessageStatus.Failed, DateTimeOffset.UtcNow, "Recipient phone number not in allowed list");

        var message = await db.Messages.IgnoreQueryFilters().FirstAsync(m => m.ExternalMessageId == "wamid.4");
        Assert.Equal(MessageStatus.Failed, message.Status);
        Assert.Equal("Recipient phone number not in allowed list", message.FailureReason);
        Assert.NotNull(message.FailedAt);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_FailedAfterDelivered_IsIgnored()
    {
        var (dbName, _) = await SeedSentMessageAsync("wamid.5");

        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            await new MessageStatusService(db).UpdateDeliveryStatusAsync("wamid.5", MessageStatus.Delivered, DateTimeOffset.UtcNow);
        }

        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            await new MessageStatusService(db).UpdateDeliveryStatusAsync("wamid.5", MessageStatus.Failed, DateTimeOffset.UtcNow, "algo raro");
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: null);
        var message = await assertDb.Messages.IgnoreQueryFilters().FirstAsync(m => m.ExternalMessageId == "wamid.5");
        Assert.Equal(MessageStatus.Delivered, message.Status);
        Assert.Null(message.FailureReason);
    }

    [Fact]
    public async Task UpdateDeliveryStatus_UnknownExternalMessageId_IsNoOp()
    {
        var dbName = TestDb.NewDbName();
        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = new MessageStatusService(db);

        // No debe tirar excepción aunque el mensaje no exista.
        await service.UpdateDeliveryStatusAsync("wamid-inexistente", MessageStatus.Delivered, DateTimeOffset.UtcNow);
    }
}

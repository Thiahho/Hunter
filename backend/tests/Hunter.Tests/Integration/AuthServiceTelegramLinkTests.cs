using Hunter.Application.Auth;
using Hunter.Application.Auth.Contracts;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Flujo de vinculación self-service de Telegram: generar un link para el usuario logueado y
// completarlo desde el webhook cuando llega el /start <code>.
public class AuthServiceTelegramLinkTests
{
    // AuthService no usa estos dos para el flujo de Telegram: se dejan sin implementar a
    // propósito, así cualquier uso inesperado hace fallar el test en vez de pasar en silencio.
    private class NotImplementedPasswordHasher : IPasswordHasher
    {
        public string Hash(User user, string password) => throw new NotImplementedException();
        public bool Verify(User user, string hash, string password) => throw new NotImplementedException();
    }

    private class NotImplementedTokenService : IJwtTokenService
    {
        public AccessTokenResult CreateAccessToken(User user, IReadOnlyCollection<string> roles) => throw new NotImplementedException();
        public RefreshTokenResult CreateRefreshToken() => throw new NotImplementedException();
        public string HashRefreshToken(string rawToken) => throw new NotImplementedException();
    }

    private static AuthService BuildService(Hunter.Infrastructure.Persistence.HunterDbContext db, RecordingTelegramNotifier telegram) =>
        new(db, new NotImplementedPasswordHasher(), new NotImplementedTokenService(), telegram);

    private static async Task<(string DbName, int UserId)> SeedUserAsync()
    {
        var dbName = TestDb.NewDbName();
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var user = new User
        {
            OrganizationId = org.Id, FirstName = "Juan", LastName = "Perez", Email = "juan@difrani.com", PasswordHash = "irrelevant"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        return (dbName, user.Id);
    }

    [Fact]
    public async Task GenerateTelegramLinkAsync_BotNotConfigured_ReturnsFailure()
    {
        var (dbName, userId) = await SeedUserAsync();
        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = BuildService(db, new RecordingTelegramNotifier { BotUsername = null });

        var result = await service.GenerateTelegramLinkAsync(userId);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task GenerateTelegramLinkAsync_BotConfigured_ReturnsDeepLinkAndPersistsCode()
    {
        var (dbName, userId) = await SeedUserAsync();
        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = BuildService(db, new RecordingTelegramNotifier { BotUsername = "HunterAlertsBot" });

        var result = await service.GenerateTelegramLinkAsync(userId);

        Assert.True(result.Succeeded);
        Assert.StartsWith("https://t.me/HunterAlertsBot?start=", result.Value!.DeepLink);
        Assert.True(result.Value.ExpiresAt > DateTimeOffset.UtcNow);

        await using var assertDb = TestDb.Create(dbName, organizationId: null);
        var user = await assertDb.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        Assert.NotNull(user.TelegramLinkCode);
        Assert.NotNull(user.TelegramLinkCodeExpiresAt);
        Assert.Contains(user.TelegramLinkCode!, result.Value.DeepLink);
    }

    [Fact]
    public async Task CompleteTelegramLinkAsync_ValidCode_SetsChatIdAndClearsCode()
    {
        var (dbName, userId) = await SeedUserAsync();

        string code;
        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            var service = BuildService(db, new RecordingTelegramNotifier { BotUsername = "HunterAlertsBot" });
            var link = await service.GenerateTelegramLinkAsync(userId);
            code = link.Value!.DeepLink.Split("start=")[1];
        }

        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            var service = BuildService(db, new RecordingTelegramNotifier());
            var result = await service.CompleteTelegramLinkAsync(code, "555111222");
            Assert.True(result.Succeeded);
        }

        await using var assertDb = TestDb.Create(dbName, organizationId: null);
        var user = await assertDb.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        Assert.Equal("555111222", user.TelegramChatId);
        Assert.Null(user.TelegramLinkCode);
        Assert.Null(user.TelegramLinkCodeExpiresAt);
    }

    [Fact]
    public async Task CompleteTelegramLinkAsync_UnknownCode_ReturnsFailureWithoutChangingAnything()
    {
        var (dbName, userId) = await SeedUserAsync();

        await using var db = TestDb.Create(dbName, organizationId: null);
        var service = BuildService(db, new RecordingTelegramNotifier());

        var result = await service.CompleteTelegramLinkAsync("codigo-inexistente", "555111222");

        Assert.False(result.Succeeded);

        await using var assertDb = TestDb.Create(dbName, organizationId: null);
        var user = await assertDb.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        Assert.Null(user.TelegramChatId);
    }

    [Fact]
    public async Task CompleteTelegramLinkAsync_ExpiredCode_ReturnsFailure()
    {
        var (dbName, userId) = await SeedUserAsync();

        await using (var db = TestDb.Create(dbName, organizationId: null))
        {
            var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
            user.TelegramLinkCode = "codigo-vencido";
            user.TelegramLinkCodeExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        await using var db2 = TestDb.Create(dbName, organizationId: null);
        var service = BuildService(db2, new RecordingTelegramNotifier());

        var result = await service.CompleteTelegramLinkAsync("codigo-vencido", "555111222");

        Assert.False(result.Succeeded);
    }
}

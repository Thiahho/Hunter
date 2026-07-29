using Hunter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.TestSupport;

public static class TestDb
{
    // Todas las instancias que comparten el mismo dbName apuntan al mismo store en memoria,
    // simulando múltiples requests (cada uno con su propio HunterDbContext y su propio
    // ICurrentUserService) contra la misma base de datos.
    public static HunterDbContext Create(string dbName, int? organizationId = null, int? userId = null)
    {
        var options = new DbContextOptionsBuilder<HunterDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var currentUser = new FakeCurrentUserService { OrganizationId = organizationId, UserId = userId };
        return new HunterDbContext(options, currentUser);
    }

    public static string NewDbName() => Guid.NewGuid().ToString();
}

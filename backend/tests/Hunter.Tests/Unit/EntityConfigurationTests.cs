using Hunter.Domain.Campaigning;
using Hunter.Domain.Identity;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Hunter.Tests.Unit;

// El proveedor InMemory usado en el resto de la suite no hace cumplir constraints únicos
// (doc 10, Sprint 9: "mismo ExternalMessageId no duplica" se garantiza en Postgres real vía
// este índice). Este test protege que la configuración del índice no se pierda en un refactor,
// aunque no reemplaza la verificación manual ya hecha contra Postgres real.
public class EntityConfigurationTests
{
    [Fact]
    public void Message_ExternalMessageId_Has_Unique_Filtered_Index()
    {
        using var db = TestDb.Create(TestDb.NewDbName());

        var entityType = db.Model.FindEntityType(typeof(Message))!;
        var index = entityType.GetIndexes()
            .Single(i => i.Properties.Count == 1 && i.Properties[0].Name == nameof(Message.ExternalMessageId));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void MessageResponse_ExternalInboundId_Has_Unique_Filtered_Index_Per_Organization()
    {
        using var db = TestDb.Create(TestDb.NewDbName());

        var entityType = db.Model.FindEntityType(typeof(MessageResponse))!;
        // Índice compuesto (OrganizationId, ExternalInboundId): la idempotencia del webhook
        // es "por organización", coherente con que el mismo externalId nunca pueda repetirse
        // dentro del mismo tenant pero sí podría, en teoría, coincidir entre tenants distintos.
        var index = entityType.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual([
                nameof(MessageResponse.OrganizationId), nameof(MessageResponse.ExternalInboundId)
            ]));

        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public void User_Area_Is_Persisted_As_String_With_Max_Length_And_Indexed()
    {
        using var db = TestDb.Create(TestDb.NewDbName());

        var entityType = db.Model.FindEntityType(typeof(User))!;
        var areaProperty = entityType.FindProperty(nameof(User.Area))!;

        Assert.Equal(typeof(string), areaProperty.GetProviderClrType());
        Assert.Equal(20, areaProperty.GetMaxLength());

        var index = entityType.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual([
                nameof(User.OrganizationId), nameof(User.Area)
            ]));

        Assert.NotNull(index);
    }
}

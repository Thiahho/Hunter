using Hunter.Application.Crm;
using Hunter.Domain.Crm;
using Hunter.Domain.Identity;
using Hunter.Domain.Organizations;
using Hunter.Domain.Prospecting;
using Hunter.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Hunter.Tests.Integration;

// Caso crítico obligatorio de doc 10 (Sprint 9): "Lead creado = Vendedor notificado".
// Sin un canal de notificación real todavía (doc 12, pendiente), la regla verificable
// del MVP es la asignación round-robin en sí: cada Lead nuevo debe quedar en manos de
// un vendedor activo, alternando entre ellos.
public class LeadAssignmentTests
{
    private static async Task<(int orgId, List<int> sellerIds)> SeedOrgWithSellersAsync(string dbName, int activeSellers, int inactiveSellers = 0)
    {
        await using var db = TestDb.Create(dbName);

        var org = new Organization { Name = "Difrani" };
        db.Organizations.Add(org);
        await db.SaveChangesAsync();

        var sellerIds = new List<int>();
        for (var i = 0; i < activeSellers; i++)
        {
            var seller = new User
            {
                OrganizationId = org.Id,
                FirstName = $"Vendedor{i}",
                LastName = "Test",
                Email = $"vendedor{i}@difrani.com",
                PasswordHash = "irrelevant",
                IsActive = true
            };
            db.Users.Add(seller);
            await db.SaveChangesAsync();
            sellerIds.Add(seller.Id);
        }

        for (var i = 0; i < inactiveSellers; i++)
        {
            db.Users.Add(new User
            {
                OrganizationId = org.Id,
                FirstName = $"Inactivo{i}",
                LastName = "Test",
                Email = $"inactivo{i}@difrani.com",
                PasswordHash = "irrelevant",
                IsActive = false
            });
        }
        await db.SaveChangesAsync();

        return (org.Id, sellerIds);
    }

    [Fact]
    public async Task PickNextAssignee_NoLeadsYet_ReturnsFirstActiveSellerByOrder()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, sellerIds) = await SeedOrgWithSellersAsync(dbName, activeSellers: 3);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var picked = await LeadAssignment.PickNextAssigneeAsync(db, orgId);

        Assert.Equal(sellerIds[0], picked);
    }

    [Fact]
    public async Task PickNextAssignee_CyclesThroughActiveSellersInOrder()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, sellerIds) = await SeedOrgWithSellersAsync(dbName, activeSellers: 3);

        var assignmentOrder = new List<int>();

        for (var round = 0; round < sellerIds.Count * 2; round++)
        {
            await using var db = TestDb.Create(dbName, organizationId: orgId);
            var assignedTo = await LeadAssignment.PickNextAssigneeAsync(db, orgId);
            Assert.NotNull(assignedTo);
            assignmentOrder.Add(assignedTo!.Value);

            db.Leads.Add(new Lead
            {
                OrganizationId = orgId,
                ProspectId = 0,
                AssignedToUserId = assignedTo,
                Status = LeadStatus.New,
                Priority = LeadPriority.Medium
            });
            await db.SaveChangesAsync();
        }

        // Dos vueltas completas al ciclo de 3 vendedores: 0,1,2,0,1,2
        List<int> expectedOrder = [sellerIds[0], sellerIds[1], sellerIds[2], sellerIds[0], sellerIds[1], sellerIds[2]];
        Assert.Equal(expectedOrder, assignmentOrder);
    }

    [Fact]
    public async Task PickNextAssignee_SkipsInactiveSellers()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, sellerIds) = await SeedOrgWithSellersAsync(dbName, activeSellers: 2, inactiveSellers: 5);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var picked = await LeadAssignment.PickNextAssigneeAsync(db, orgId);

        Assert.NotNull(picked);
        Assert.Contains(picked!.Value, sellerIds);
    }

    [Fact]
    public async Task PickNextAssignee_NoActiveSellers_ReturnsNull()
    {
        var dbName = TestDb.NewDbName();
        var (orgId, _) = await SeedOrgWithSellersAsync(dbName, activeSellers: 0, inactiveSellers: 2);

        await using var db = TestDb.Create(dbName, organizationId: orgId);
        var picked = await LeadAssignment.PickNextAssigneeAsync(db, orgId);

        Assert.Null(picked);
    }

    // Regresión específica: antes de acotar el cursor al conjunto de candidatos, el "último
    // asignado" de la org completa casi siempre pertenecía al área contraria, IndexOf devolvía
    // -1 y el round-robin de cada área colapsaba siempre en su primer usuario (starvation).
    [Fact]
    public async Task PickNextAssignee_WithArea_CyclesEachAreaIndependently()
    {
        var dbName = TestDb.NewDbName();
        int orgId;
        var adminIds = new List<int>();
        var ventasIds = new List<int>();

        await using (var db = TestDb.Create(dbName))
        {
            var org = new Organization { Name = "Difrani" };
            db.Organizations.Add(org);
            await db.SaveChangesAsync();
            orgId = org.Id;

            for (var i = 0; i < 2; i++)
            {
                var user = new User
                {
                    OrganizationId = orgId, FirstName = $"Admin{i}", LastName = "Test", Email = $"admin{i}@difrani.com",
                    PasswordHash = "irrelevant", IsActive = true, Area = UserArea.Administracion
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                adminIds.Add(user.Id);
            }

            for (var i = 0; i < 3; i++)
            {
                var user = new User
                {
                    OrganizationId = orgId, FirstName = $"Ventas{i}", LastName = "Test", Email = $"ventas{i}@difrani.com",
                    PasswordHash = "irrelevant", IsActive = true, Area = UserArea.Ventas
                };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                ventasIds.Add(user.Id);
            }
        }

        var adminAssignments = new List<int>();
        var ventasAssignments = new List<int>();

        // Se intercalan asignaciones de ambas áreas, simulando leads de rubros mezclados
        // llegando en cualquier orden.
        for (var round = 0; round < 3; round++)
        {
            await using var db = TestDb.Create(dbName, organizationId: orgId);

            var admin = await LeadAssignment.PickNextAssigneeAsync(db, orgId, UserArea.Administracion);
            Assert.NotNull(admin);
            adminAssignments.Add(admin!.Value);
            db.Leads.Add(new Lead { OrganizationId = orgId, ProspectId = 0, AssignedToUserId = admin, Status = LeadStatus.New, Priority = LeadPriority.Medium });
            await db.SaveChangesAsync();

            var ventas = await LeadAssignment.PickNextAssigneeAsync(db, orgId, UserArea.Ventas);
            Assert.NotNull(ventas);
            ventasAssignments.Add(ventas!.Value);
            db.Leads.Add(new Lead { OrganizationId = orgId, ProspectId = 0, AssignedToUserId = ventas, Status = LeadStatus.New, Priority = LeadPriority.Medium });
            await db.SaveChangesAsync();
        }

        Assert.Equal([adminIds[0], adminIds[1], adminIds[0]], adminAssignments);
        Assert.Equal([ventasIds[0], ventasIds[1], ventasIds[2]], ventasAssignments);
    }
}

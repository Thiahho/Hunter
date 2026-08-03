using Hunter.Application.Crm;
using Hunter.Domain.Identity;
using Hunter.Domain.Prospecting;

namespace Hunter.Tests.Unit;

public class LeadRoutingTests
{
    [Theory]
    [InlineData(ProspectCategory.Distributor, UserArea.Administracion)]
    [InlineData(ProspectCategory.AutoPartsStore, UserArea.Administracion)]
    [InlineData(ProspectCategory.Workshop, UserArea.Ventas)]
    [InlineData(ProspectCategory.Lubricentro, UserArea.Ventas)]
    [InlineData(ProspectCategory.TireShop, UserArea.Ventas)]
    [InlineData(ProspectCategory.Reseller, UserArea.Ventas)]
    [InlineData(ProspectCategory.Other, UserArea.Ventas)]
    [InlineData(ProspectCategory.Unknown, UserArea.Ventas)]
    public void AreaFor_MapsEveryCategory(ProspectCategory category, UserArea expectedArea)
    {
        Assert.Equal(expectedArea, LeadRouting.AreaFor(category));
    }

    [Fact]
    public void AreaFor_CoversEveryEnumMember()
    {
        // Recorre el enum en vez de una lista fija a mano: si se agrega un rubro nuevo,
        // este test obliga a decidir explícitamente a qué área va, en vez de caer
        // silenciosamente en el default del switch sin que nadie lo note.
        foreach (var category in Enum.GetValues<ProspectCategory>())
        {
            var area = LeadRouting.AreaFor(category);
            Assert.True(area is UserArea.Administracion or UserArea.Ventas);
        }
    }
}

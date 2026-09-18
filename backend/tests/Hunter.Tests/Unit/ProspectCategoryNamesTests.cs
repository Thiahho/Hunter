using Hunter.Application.Prospecting;
using Hunter.Domain.Prospecting;

namespace Hunter.Tests.Unit;

public class ProspectCategoryNamesTests
{
    [Theory]
    [InlineData("AutoPartsStore", ProspectCategory.AutoPartsStore)]
    [InlineData("autopartsstore", ProspectCategory.AutoPartsStore)]
    [InlineData("Casa de repuestos", ProspectCategory.AutoPartsStore)]
    [InlineData("Tienda de repuestos para automóviles", ProspectCategory.AutoPartsStore)]
    [InlineData("mayorista suspensión tren delantero", ProspectCategory.Distributor)]
    [InlineData("Mayorista de repuestos", ProspectCategory.Distributor)]
    [InlineData("Gomería", ProspectCategory.TireShop)]
    [InlineData("Taller mecánico", ProspectCategory.Workshop)]
    [InlineData("Peluquería", ProspectCategory.Unknown)]
    [InlineData("3", ProspectCategory.Unknown)]
    [InlineData("", ProspectCategory.Unknown)]
    [InlineData(null, ProspectCategory.Unknown)]
    public void Resolve_MapsTextToCategory(string? text, ProspectCategory expected)
    {
        Assert.Equal(expected, ProspectCategoryNames.Resolve(text));
    }

    [Fact]
    public void DisplayName_PrefersFreeTextOverEnum()
    {
        Assert.Equal("Distribuidora de frenos", ProspectCategoryNames.DisplayName(ProspectCategory.Distributor, "Distribuidora de frenos"));
        Assert.Equal("Mayorista/Distribuidor", ProspectCategoryNames.DisplayName(ProspectCategory.Distributor, null));
    }
}

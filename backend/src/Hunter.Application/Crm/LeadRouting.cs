using Hunter.Domain.Identity;
using Hunter.Domain.Prospecting;

namespace Hunter.Application.Crm;

// Regla de derivación por rubro (autodeclarado por el prospecto vía botón quick-reply de
// WhatsApp, o inferido en la importación): mayoristas y casas de repuestos requieren cuenta
// corriente / lista de precios mayorista, así que los atiende ADMINISTRACIÓN. Todo el resto
// (talleres, lubricentros, gomerías, revendedores, sin rubro definido) lo atiende VENTAS.
public static class LeadRouting
{
    public static UserArea AreaFor(ProspectCategory category) => category switch
    {
        ProspectCategory.Distributor or ProspectCategory.AutoPartsStore => UserArea.Administracion,
        _ => UserArea.Ventas
    };
}

namespace Hunter.Domain.Identity;

// Área operativa del usuario, independiente de los roles de permisos (OWNER/ADMIN/MANAGER/SELLER).
// Define a qué usuarios se les asignan los leads según el rubro del prospecto (ver LeadRouting,
// en Hunter.Application/Crm). Un ADMIN no queda atado por su rol a atender mayoristas: eso lo
// decide este campo.
public enum UserArea
{
    Unassigned,
    Administracion,
    Ventas
}

using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;

namespace Hunter.Application.Campaigning;

// Catálogo de plantillas aprobadas por Meta (Graph API). Separado de IMessageProvider porque es
// una operación administrativa (listar para elegir en la web), no de envío de mensajes: el copy
// real de una plantilla solo existe donde Meta lo aprobó (developers.facebook.com), acá solo se
// refleja para que MessageTemplateService pueda sincronizarlo como plantilla local activa.
public interface IWhatsAppTemplateCatalogClient
{
    Task<Result<IReadOnlyList<MetaWhatsAppTemplateDto>>> ListApprovedAsync(CancellationToken ct = default);
}

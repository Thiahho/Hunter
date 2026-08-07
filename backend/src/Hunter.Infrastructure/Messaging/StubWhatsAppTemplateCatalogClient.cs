using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Shared;

namespace Hunter.Infrastructure.Messaging;

// Placeholder cuando WhatsApp Cloud API no está configurado (ver AddInfrastructure): sin
// AccessToken no hay forma de pegarle a la Graph API, así que se devuelve un error explicable
// en vez de dejar caer una excepción de HttpClient sin BaseAddress.
public class StubWhatsAppTemplateCatalogClient : IWhatsAppTemplateCatalogClient
{
    public Task<Result<IReadOnlyList<MetaWhatsAppTemplateDto>>> ListApprovedAsync(CancellationToken ct = default) =>
        Task.FromResult(Result<IReadOnlyList<MetaWhatsAppTemplateDto>>.Failure(
            "WhatsApp Cloud API no está configurado (falta AccessToken/WabaId)."));
}

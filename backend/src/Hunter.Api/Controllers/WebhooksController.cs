using System.Text;
using System.Text.Json;
using Hunter.Api.Contracts;
using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Infrastructure.Messaging;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hunter.Api.Controllers;

// Endpoint interno para proveedores/n8n (doc 19, sección 51): no usa JWT de usuario,
// se protege con un secreto compartido en el header. La resolución de organización
// por URL específica (un webhook por organización) queda para cuando n8n esté cableado;
// por ahora el organizationId viaja explícito en el payload.
[ApiController]
[AllowAnonymous]
[Route("api/v1/webhooks/messaging")]
public class WebhooksController(
    IInboundMessageService inboundMessageService,
    IConfiguration configuration,
    IOptions<WhatsAppCloudApiOptions> whatsAppOptions,
    ILogger<WebhooksController> logger) : ControllerBase
{
    [HttpPost("inbound")]
    public async Task<IActionResult> Inbound(InboundMessageRequest request, CancellationToken ct)
    {
        if (!IsValidSecret())
            return Unauthorized(ApiResponse<InboundMessageResultDto>.Fail("Secreto de webhook inválido."));

        var result = await inboundMessageService.ProcessAsync(request, ct);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<InboundMessageResultDto>.Fail(result.Error!));

        return Ok(ApiResponse<InboundMessageResultDto>.Ok(result.Value!));
    }

    private bool IsValidSecret()
    {
        var expected = configuration["Webhooks:InboundSecret"];
        if (string.IsNullOrEmpty(expected))
            return false;

        var provided = Request.Headers["X-Webhook-Secret"].ToString();
        return !string.IsNullOrEmpty(provided) && provided == expected;
    }

    // Handshake de verificación que Meta hace una sola vez al dar de alta el webhook
    // (https://developers.facebook.com/docs/graph-api/webhooks/getting-started#verification-requests).
    [HttpGet("whatsapp")]
    public IActionResult VerifyWhatsAppWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        var expected = whatsAppOptions.Value.WebhookVerifyToken;
        if (mode != "subscribe" || string.IsNullOrEmpty(expected) || verifyToken != expected)
            return Unauthorized();

        return Content(challenge ?? string.Empty, "text/plain");
    }

    [HttpPost("whatsapp")]
    public async Task<IActionResult> WhatsAppInbound(CancellationToken ct)
    {
        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync(ct);
        }
        Request.Body.Position = 0;

        var signatureHeader = Request.Headers["X-Hub-Signature-256"].ToString();
        if (!WhatsAppWebhookSignatureValidator.IsValid(rawBody, signatureHeader, whatsAppOptions.Value.AppSecret))
            return Unauthorized();

        var options = whatsAppOptions.Value;
        if (options.OrganizationId is null)
        {
            logger.LogWarning("[WhatsApp webhook] Recibido pero WhatsAppCloudApi:OrganizationId no está configurado.");
            return Ok();
        }

        WhatsAppWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(rawBody);
        }
        catch (JsonException)
        {
            return BadRequest();
        }

        var messages = payload?.Entry
            .SelectMany(e => e.Changes)
            .Select(c => c.Value)
            .Where(v => v?.Messages is not null)
            .SelectMany(v => v!.Messages!)
            .Where(m => m.Type == "text" && m.Text is not null) ?? [];

        foreach (var message in messages)
        {
            var request = new InboundMessageRequest(
                options.OrganizationId.Value,
                message.From,
                message.Text!.Body,
                ExternalInboundId: message.Id);

            var result = await inboundMessageService.ProcessAsync(request, ct);
            if (!result.Succeeded)
                logger.LogWarning("[WhatsApp webhook] No se pudo procesar el mensaje {MessageId}: {Error}", message.Id, result.Error);
        }

        // Meta reintenta si no respondemos 200 rápido; devolvemos OK aunque algún mensaje individual haya fallado.
        return Ok();
    }
}

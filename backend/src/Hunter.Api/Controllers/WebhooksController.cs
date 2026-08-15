using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hunter.Api.Contracts;
using Hunter.Application.Auth;
using Hunter.Application.Campaigning;
using Hunter.Application.Campaigning.Contracts;
using Hunter.Application.Crm;
using Hunter.Domain.Campaigning;
using Hunter.Infrastructure.Messaging;
using Hunter.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Hunter.Api.Controllers;

// Endpoint interno para proveedores/n8n (doc 19, sección 51): no usa JWT de usuario,
// se protege con un secreto compartido en el header.
[ApiController]
[AllowAnonymous]
[Route("api/v1/webhooks/messaging")]
public class WebhooksController(
    IInboundMessageService inboundMessageService,
    IMessageStatusService messageStatusService,
    IAuthService authService,
    ITelegramNotifier telegramNotifier,
    IConfiguration configuration,
    IOptions<WhatsAppCloudApiOptions> whatsAppOptions,
    IOptions<TelegramOptions> telegramOptions,
    ILogger<WebhooksController> logger) : ControllerBase
{
    [HttpPost("inbound")]
    public async Task<IActionResult> Inbound(InboundMessageRequest request, CancellationToken ct)
    {
        if (!IsValidSecret())
            return Unauthorized(ApiResponse<InboundMessageResultDto>.Fail("Secreto de webhook inválido."));

        // El organizationId lo resuelve el servidor, nunca el caller: antes venía tal cual del
        // body (request.OrganizationId), así que quien tuviera el secreto compartido podía
        // apuntar el mensaje a CUALQUIER organización del sistema con solo cambiar ese campo
        // (auditoria.md, hallazgo Medio #4). Mismo mecanismo de config que ya usa el webhook
        // nativo de WhatsApp (WhatsAppCloudApiOptions.OrganizationId) — MVP de un solo tenant por
        // deploy, multi-org queda para cuando n8n tenga un webhook por organización.
        var organizationId = whatsAppOptions.Value.OrganizationId;
        if (organizationId is null)
        {
            logger.LogError("[Webhook inbound] Recibido pero WhatsAppCloudApi:OrganizationId no está configurado.");
            return BadRequest(ApiResponse<InboundMessageResultDto>.Fail("El servidor no tiene una organización configurada para este webhook."));
        }

        var result = await inboundMessageService.ProcessAsync(request with { OrganizationId = organizationId.Value }, ct);
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
        if (string.IsNullOrEmpty(provided))
            return false;

        // Comparación en tiempo constante, igual que TelegramWebhookSecretValidator y
        // WhatsAppWebhookSignatureValidator (auditoria.md, hallazgo Medio #4) — un "==" normal
        // corta en el primer byte distinto, filtrando por timing cuánto del secreto acertó quien
        // ataca.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
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

        var rawMessages = payload?.Entry
            .SelectMany(e => e.Changes)
            .Select(c => c.Value)
            .Where(v => v?.Messages is not null)
            .SelectMany(v => v!.Messages!) ?? [];

        foreach (var raw in rawMessages)
        {
            var message = WhatsAppInboundExtractor.TryExtract(raw);
            if (message is null)
            {
                // Antes esto se descartaba en silencio para cualquier tipo != "text" (incluidos
                // los taps de botón). Con LogDebug al menos queda rastro del body crudo.
                logger.LogDebug("[WhatsApp webhook] Mensaje {MessageId} de tipo {Type} no reconocido, se ignora.", raw.Id, raw.Type);
                continue;
            }

            var request = new InboundMessageRequest(
                options.OrganizationId.Value,
                message.From,
                message.Content,
                ExternalInboundId: message.Id,
                ExternalMessageId: message.ContextMessageId,
                ButtonPayload: message.ButtonPayload);

            var result = await inboundMessageService.ProcessAsync(request, ct);
            if (!result.Succeeded)
                logger.LogWarning("[WhatsApp webhook] No se pudo procesar el mensaje {MessageId}: {Error}", message.Id, result.Error);
        }

        var statuses = payload?.Entry
            .SelectMany(e => e.Changes)
            .Select(c => c.Value)
            .Where(v => v?.Statuses is not null)
            .SelectMany(v => v!.Statuses!) ?? [];

        foreach (var status in statuses)
        {
            var mapped = MapStatus(status.Status);
            if (mapped is null)
                continue;

            var timestamp = long.TryParse(status.Timestamp, out var unixSeconds)
                ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
                : DateTimeOffset.UtcNow;

            var failureReason = status.Errors?.FirstOrDefault() is { } error
                ? $"{error.Title}: {error.Message}"
                : null;

            await messageStatusService.UpdateDeliveryStatusAsync(status.Id, mapped.Value, timestamp, failureReason, ct);
        }

        // Meta reintenta si no respondemos 200 rápido; devolvemos OK aunque algún evento individual haya fallado.
        return Ok();
    }

    private static MessageStatus? MapStatus(string status) => status switch
    {
        "sent" => MessageStatus.Sent,
        "delivered" => MessageStatus.Delivered,
        "read" => MessageStatus.Read,
        "failed" => MessageStatus.Failed,
        _ => null
    };

    // Recibe los updates del bot de Telegram (hoy solo nos importa /start <code> del flujo de
    // vinculación self-service, ver AuthService.CompleteTelegramLinkAsync). Se registra una sola
    // vez a mano contra la Bot API (setWebhook), no hay handshake de verificación como en Meta.
    [HttpPost("telegram")]
    public async Task<IActionResult> TelegramInbound(CancellationToken ct)
    {
        var providedSecret = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
        if (!TelegramWebhookSecretValidator.IsValid(providedSecret, telegramOptions.Value.WebhookSecret))
            return Unauthorized();

        TelegramUpdate? update;
        try
        {
            update = await Request.ReadFromJsonAsync<TelegramUpdate>(ct);
        }
        catch (JsonException)
        {
            return Ok(); // update que no podemos parsear: no hay nada que reintentar del lado de Telegram
        }

        var text = update?.Message?.Text;
        if (text is not null && text.StartsWith("/start ", StringComparison.Ordinal))
        {
            var code = text["/start ".Length..].Trim();
            var chatId = update!.Message!.Chat.Id.ToString();

            var result = await authService.CompleteTelegramLinkAsync(code, chatId, ct);

            var reply = result.Succeeded
                ? "✅ Listo, vas a recibir alertas de leads acá."
                : "Ese link ya expiró o no es válido. Generá uno nuevo desde Hunter.";

            var sendResult = await telegramNotifier.SendAsync(chatId, reply, ct: ct);
            if (!sendResult.Success)
                logger.LogWarning("[Telegram webhook] No se pudo responder al chat_id {ChatId}: {Error}", chatId, sendResult.Error);
        }

        // Telegram reintenta si no respondemos 200 rápido, igual que Meta.
        return Ok();
    }
}

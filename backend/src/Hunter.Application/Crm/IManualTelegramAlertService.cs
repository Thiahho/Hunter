using Hunter.Shared;

namespace Hunter.Application.Crm;

// Reenvío manual del aviso de lead a Telegram (doc handoff): cubre los casos donde la alerta
// automática no dispara sola (ej. el prospecto responde "interesado" en un mensaje de texto
// libre fuera del flujo del bot, o un operador nota interés revisando la conversación a mano).
// A diferencia de NotifyByTelegramAsync (InboundMessageService), esto no depende de un Lead
// abierto ni de su AssignedToUserId: siempre le llega a quien lo dispara, para que esa persona
// pueda actuar (escribirle al prospecto, derivar) sin depender de la asignación de otro.
public interface IManualTelegramAlertService
{
    Task<Result<bool>> SendAsync(int prospectId, CancellationToken ct = default);
}

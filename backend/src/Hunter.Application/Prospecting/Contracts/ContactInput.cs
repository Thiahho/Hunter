using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record ContactInput(ProspectContactChannel Channel, string Value, bool IsPrimary = false);

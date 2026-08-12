using Hunter.Domain.Prospecting;

namespace Hunter.Application.Prospecting.Contracts;

public record ProspectQuery(
    string? Search = null,
    ProspectCategory? Category = null,
    string? City = null,
    string? Province = null,
    ProspectStatus? Status = null,
    string? Tag = null,
    ProspectSourceType? Source = null,
    BusinessSize? BusinessSize = null,
    // Filtra por antigüedad de alta: 1 = agregados hoy, 7 = agregados en los últimos 7 días
    // (calendario, incluyendo hoy), etc. Null = sin filtro de fecha.
    int? CreatedWithinDays = null,
    int Page = 1,
    int PageSize = 50);

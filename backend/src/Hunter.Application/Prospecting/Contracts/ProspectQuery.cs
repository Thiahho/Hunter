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
    int Page = 1,
    int PageSize = 50);

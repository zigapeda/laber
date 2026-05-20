namespace Laber.Shared.Dtos;

public sealed record ConnectionEventDto(
    DateTimeOffset OccurredAt,
    string Kind,
    string? Detail);

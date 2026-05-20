namespace Laber.Shared.Dtos;

public sealed record MessageDto(
    long Id,
    string Channel,
    string Sender,
    string Text,
    DateTimeOffset ReceivedAt);

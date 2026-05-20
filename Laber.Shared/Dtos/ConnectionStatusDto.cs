namespace Laber.Shared.Dtos;

public sealed record ConnectionStatusDto(
    bool IsConnected,
    string Server,
    int Port,
    string Nick,
    DateTimeOffset? LastConnectedAt,
    string? LastError);

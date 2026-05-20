namespace Laber.Shared.Models;

public sealed class IrcConnectionState
{
    public int Id { get; set; } = 1;

    public bool IsConnected { get; set; }

    public string Server { get; set; } = string.Empty;

    public int Port { get; set; }

    public string Nick { get; set; } = string.Empty;

    public DateTimeOffset? LastConnectedAt { get; set; }

    public string? LastError { get; set; }
}

namespace Laber.Shared.Models;

public sealed class IrcMessage
{
    public long Id { get; set; }

    public string Channel { get; set; } = string.Empty;

    public string Sender { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; set; }
}

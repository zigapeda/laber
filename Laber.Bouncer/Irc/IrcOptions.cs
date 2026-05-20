namespace Laber.Bouncer.Irc;

public sealed class IrcOptions
{
    public const string SectionName = "Irc";

    public string Server { get; set; } = "irc.libera.chat";

    public int Port { get; set; } = 6697;

    public bool UseTls { get; set; } = true;

    public string Nick { get; set; } = "laber";

    public string? Password { get; set; }

    public string RealName { get; set; } = "Laber IRC Bouncer";

    public string[] Channels { get; set; } = new[] { "#laber" };
}

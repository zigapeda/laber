namespace Laber.Irc;

internal static class IrcLineParser
{
    public static bool TryParsePrivmsg(string line, out string channel, out string sender, out string text)
    {
        channel = string.Empty;
        sender = string.Empty;
        text = string.Empty;

        if (!line.Contains(" PRIVMSG ", StringComparison.Ordinal))
        {
            return false;
        }

        var prefixEnd = line.StartsWith(":", StringComparison.Ordinal) ? line.IndexOf(' ') : -1;
        if (prefixEnd <= 1)
        {
            return false;
        }

        var nickEnd = line.IndexOf('!', 1, prefixEnd - 1);
        sender = nickEnd > 1 ? line[1..nickEnd] : line[1..prefixEnd];

        var privmsgIndex = line.IndexOf(" PRIVMSG ", StringComparison.Ordinal);
        var targetStart = privmsgIndex + 9;
        var targetEnd = line.IndexOf(' ', targetStart);
        if (targetEnd < 0)
        {
            return false;
        }

        channel = line[targetStart..targetEnd];
        var textStart = line.IndexOf(" :", targetEnd, StringComparison.Ordinal);
        if (textStart < 0)
        {
            return false;
        }

        text = line[(textStart + 2)..];
        return true;
    }

    public static bool IsPing(string line, out string token)
    {
        token = string.Empty;
        if (!line.StartsWith("PING ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = line[5..].Trim();
        return true;
    }
}

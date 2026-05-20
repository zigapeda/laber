namespace Laber.Shared.Storage;

public static class ChannelPath
{
    public static string NormalizeChannel(string channel)
    {
        var trimmed = channel.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return "#unknown";
        }

        return trimmed.StartsWith('#') ? trimmed : "#" + trimmed;
    }

    public static string ToDirectoryName(string channel)
    {
        var normalized = NormalizeChannel(channel);
        var name = normalized[1..];
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(name) ? "unknown" : name;
    }

    public static string FromDirectoryName(string directoryName) => "#" + directoryName;
}

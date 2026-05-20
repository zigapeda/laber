using System.Text;

namespace Laber.Shared.Storage;

public static class MessageLineFormat
{
    public static string Format(DateTimeOffset receivedAt, string sender, string text)
    {
        return string.Join(
            '\t',
            receivedAt.ToString("O"),
            sender,
            Escape(text));
    }

    public static bool TryParse(string line, out DateTimeOffset receivedAt, out string sender, out string text)
    {
        receivedAt = default;
        sender = string.Empty;
        text = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var firstTab = line.IndexOf('\t');
        if (firstTab <= 0)
        {
            return false;
        }

        var secondTab = line.IndexOf('\t', firstTab + 1);
        if (secondTab <= firstTab)
        {
            return false;
        }

        if (!DateTimeOffset.TryParse(line[..firstTab], out receivedAt))
        {
            return false;
        }

        sender = line[(firstTab + 1)..secondTab];
        text = Unescape(line[(secondTab + 1)..]);
        return true;
    }

    private static string Escape(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }

    private static string Unescape(string value)
    {
        var builder = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\' || i + 1 >= value.Length)
            {
                builder.Append(value[i]);
                continue;
            }

            i++;
            builder.Append(value[i] switch
            {
                't' => '\t',
                'n' => '\n',
                'r' => '\r',
                '\\' => '\\',
                _ => value[i]
            });
        }

        return builder.ToString();
    }
}

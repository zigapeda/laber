namespace Laber.Shared.Storage;

public static class MessageId
{
    private const long LineMultiplier = 10_000_000_000L;

    public static long Encode(int year, int lineNumber) =>
        (long)year * LineMultiplier + lineNumber;

    public static (int Year, int LineNumber) Decode(long id)
    {
        var year = (int)(id / LineMultiplier);
        var line = (int)(id % LineMultiplier);
        return (year, line);
    }
}

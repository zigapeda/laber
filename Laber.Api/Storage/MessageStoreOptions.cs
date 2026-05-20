namespace Laber.Api.Storage;

public sealed class MessageStoreOptions
{
    public const string SectionName = "MessageStore";

    public string DataDirectory { get; set; } = "data";
}

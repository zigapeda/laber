namespace Laber.Shared.Dtos;

public sealed record ChannelDto(
    string Name,
    int UnreadCount,
    DateTimeOffset? LastMessageAt);

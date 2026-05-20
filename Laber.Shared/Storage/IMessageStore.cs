using Laber.Shared.Dtos;

namespace Laber.Shared.Storage;

public interface IMessageStore
{
    Task<MessageDto> AppendAsync(
        string channel,
        string sender,
        string text,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageDto>> ReadAsync(
        string channel,
        int limit,
        long? afterId,
        long? beforeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MessageDto>> ReadAllNewAsync(
        long? afterId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChannelDto>> ListChannelsAsync(
        long? sinceId,
        CancellationToken cancellationToken = default);
}

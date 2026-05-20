using System.Threading.Channels;
using Laber.Shared.Dtos;

namespace Laber.Api.Streaming;

public sealed class MessageStreamService
{
    private readonly Channel<MessageDto> _channel = Channel.CreateUnbounded<MessageDto>(
        new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = true
        });

    public ChannelReader<MessageDto> Reader => _channel.Reader;

    public void Publish(MessageDto message) => _channel.Writer.TryWrite(message);
}

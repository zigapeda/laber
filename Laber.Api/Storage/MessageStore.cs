using System.Collections.Concurrent;
using Laber.Api.Streaming;
using Laber.Shared.Dtos;
using Laber.Shared.Storage;

namespace Laber.Api.Storage;

public sealed class MessageStore : IMessageStore
{
    private readonly string _dataDirectory;
    private readonly MessageStreamService _stream;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new();
    private readonly ConcurrentDictionary<string, int> _lineCounts = new();

    public MessageStore(MessageStoreOptions options, MessageStreamService stream)
    {
        _dataDirectory = Path.GetFullPath(options.DataDirectory);
        _stream = stream;
        Directory.CreateDirectory(_dataDirectory);
    }

    public async Task<MessageDto> AppendAsync(
        string channel,
        string sender,
        string text,
        CancellationToken cancellationToken = default)
    {
        var normalized = ChannelPath.NormalizeChannel(channel);
        var receivedAt = DateTimeOffset.UtcNow;
        var year = receivedAt.Year;
        var path = GetFilePath(normalized, year);
        var fileLock = GetFileLock(path);

        await fileLock.WaitAsync(cancellationToken);
        try
        {
            var lineNumber = await GetNextLineNumberAsync(path, normalized, year, cancellationToken);
            var line = MessageLineFormat.Format(receivedAt, sender, text);
            await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken);
            var id = MessageId.Encode(year, lineNumber);
            var message = new MessageDto(id, normalized, sender, text, receivedAt);
            _stream.Publish(message);
            return message;
        }
        finally
        {
            fileLock.Release();
        }
    }

    public Task<IReadOnlyList<MessageDto>> ReadAsync(
        string channel,
        int limit,
        long? afterId,
        long? beforeId,
        CancellationToken cancellationToken = default)
    {
        var normalized = ChannelPath.NormalizeChannel(channel);
        var take = Math.Clamp(limit, 1, 500);

        if (afterId is > 0)
        {
            return ReadAfterAsync(normalized, take, afterId.Value, cancellationToken);
        }

        if (beforeId is > 0)
        {
            return ReadBeforeAsync(normalized, take, beforeId.Value, cancellationToken);
        }

        return ReadLatestAsync(normalized, take, cancellationToken);
    }

    public Task<IReadOnlyList<MessageDto>> ReadAllNewAsync(
        long? afterId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 500);
        return Task.Run(async () =>
        {
            var channels = ListChannelDirectories();
            var messages = new List<MessageDto>();

            foreach (var channelDir in channels)
            {
                var channel = ChannelPath.FromDirectoryName(channelDir);
                var batch = await ReadAfterAsync(channel, take, afterId ?? 0, cancellationToken);
                messages.AddRange(batch);
            }

            return (IReadOnlyList<MessageDto>)messages
                .OrderBy(m => m.Id)
                .Take(take)
                .ToList();
        }, cancellationToken);
    }

    public Task<IReadOnlyList<ChannelDto>> ListChannelsAsync(
        long? sinceId,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            var hasSince = sinceId is > 0;
            var channels = new List<ChannelDto>();

            foreach (var channelDir in ListChannelDirectories())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var channel = ChannelPath.FromDirectoryName(channelDir);
                var messages = LoadAllMessages(channel);
                if (messages.Count == 0)
                {
                    continue;
                }

                var unread = hasSince
                    ? messages.Count(m => m.Id > sinceId)
                    : 0;
                var last = messages[^1];
                channels.Add(new ChannelDto(channel, unread, last.ReceivedAt));
            }

            return (IReadOnlyList<ChannelDto>)channels
                .OrderByDescending(c => c.LastMessageAt)
                .ToList();
        }, cancellationToken);
    }

    private async Task<IReadOnlyList<MessageDto>> ReadLatestAsync(
        string channel,
        int limit,
        CancellationToken cancellationToken)
    {
        var all = LoadAllMessages(channel);
        if (all.Count == 0)
        {
            return Array.Empty<MessageDto>();
        }

        return all
            .OrderBy(m => m.Id)
            .TakeLast(limit)
            .ToList();
    }

    private Task<IReadOnlyList<MessageDto>> ReadAfterAsync(
        string channel,
        int limit,
        long afterId,
        CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messages = LoadAllMessages(channel)
                .Where(m => m.Id > afterId)
                .OrderBy(m => m.Id)
                .Take(limit)
                .ToList();
            return (IReadOnlyList<MessageDto>)messages;
        }, cancellationToken);

    private Task<IReadOnlyList<MessageDto>> ReadBeforeAsync(
        string channel,
        int limit,
        long beforeId,
        CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messages = LoadAllMessages(channel)
                .Where(m => m.Id < beforeId)
                .OrderByDescending(m => m.Id)
                .Take(limit)
                .OrderBy(m => m.Id)
                .ToList();
            return (IReadOnlyList<MessageDto>)messages;
        }, cancellationToken);

    private List<MessageDto> LoadAllMessages(string channel)
    {
        var channelDir = Path.Combine(_dataDirectory, ChannelPath.ToDirectoryName(channel));
        if (!Directory.Exists(channelDir))
        {
            return new List<MessageDto>();
        }

        var messages = new List<MessageDto>();
        foreach (var file in Directory.GetFiles(channelDir, "*.txt").OrderBy(f => f))
        {
            if (!int.TryParse(Path.GetFileNameWithoutExtension(file), out var year))
            {
                continue;
            }

            var path = Path.Combine(channelDir, $"{year}.txt");
            var fileLock = GetFileLock(path);
            fileLock.Wait();
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var lineNumber = 0;
                foreach (var line in File.ReadLines(path))
                {
                    lineNumber++;
                    if (!MessageLineFormat.TryParse(line, out var receivedAt, out var sender, out var text))
                    {
                        continue;
                    }

                    messages.Add(new MessageDto(
                        MessageId.Encode(year, lineNumber),
                        channel,
                        sender,
                        text,
                        receivedAt));
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        return messages;
    }

    private IEnumerable<string> ListChannelDirectories()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            yield break;
        }

        foreach (var dir in Directory.GetDirectories(_dataDirectory))
        {
            yield return Path.GetFileName(dir);
        }
    }

    private string GetFilePath(string channel, int year)
    {
        var channelDir = Path.Combine(_dataDirectory, ChannelPath.ToDirectoryName(channel));
        Directory.CreateDirectory(channelDir);
        return Path.Combine(channelDir, $"{year}.txt");
    }

    private SemaphoreSlim GetFileLock(string path) =>
        _fileLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));

    private async Task<int> GetNextLineNumberAsync(
        string path,
        string channel,
        int year,
        CancellationToken cancellationToken)
    {
        var key = $"{ChannelPath.ToDirectoryName(channel)}:{year}";
        if (_lineCounts.TryGetValue(key, out var cached) && File.Exists(path))
        {
            var next = cached + 1;
            _lineCounts[key] = next;
            return next;
        }

        var count = 0;
        if (File.Exists(path))
        {
            await foreach (var _ in File.ReadLinesAsync(path, cancellationToken))
            {
                count++;
            }
        }

        var lineNumber = count + 1;
        _lineCounts[key] = lineNumber;
        return lineNumber;
    }
}

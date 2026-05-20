using System.Collections.Concurrent;
using Laber.Irc;
using Laber.Shared.Dtos;
using Microsoft.Extensions.Options;

namespace Laber.Services;

public sealed class ConnectionStatusService
{
    private readonly IrcOptions _ircOptions;
    private readonly ConcurrentQueue<ConnectionEventDto> _events = new();
    private readonly object _sync = new();
    private ConnectionStatusDto _status;

    public ConnectionStatusService(IOptions<IrcOptions> ircOptions)
    {
        _ircOptions = ircOptions.Value;
        _status = new ConnectionStatusDto(
            false,
            _ircOptions.Server,
            _ircOptions.Port,
            _ircOptions.Nick,
            null,
            null);
    }

    public ConnectionStatusDto GetStatus()
    {
        lock (_sync)
        {
            return _status;
        }
    }

    public IReadOnlyList<ConnectionEventDto> GetRecentEvents()
    {
        PruneExpired();
        return _events.ToArray();
    }

    public void SetConnected()
    {
        lock (_sync)
        {
            _status = _status with
            {
                IsConnected = true,
                LastConnectedAt = DateTimeOffset.UtcNow,
                LastError = null
            };
        }

        AddEvent("connected", null);
    }

    public void SetDisconnected(string? error)
    {
        lock (_sync)
        {
            _status = _status with
            {
                IsConnected = false,
                LastError = error
            };
        }

        AddEvent("disconnected", error);
    }

    private void AddEvent(string kind, string? detail)
    {
        PruneExpired();
        _events.Enqueue(new ConnectionEventDto(DateTimeOffset.UtcNow, kind, detail));

        while (_events.Count > 200)
        {
            _events.TryDequeue(out _);
        }
    }

    private void PruneExpired()
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-24);
        while (_events.TryPeek(out var oldest) && oldest.OccurredAt < cutoff)
        {
            _events.TryDequeue(out _);
        }
    }
}

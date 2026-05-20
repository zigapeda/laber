using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Laber.Bouncer.Irc;

public sealed class IrcClient : IAsyncDisposable
{
    private readonly IrcOptions _options;
    private readonly ILogger<IrcClient> _logger;
    private Stream? _stream;
    private TcpClient? _tcpClient;
    private CancellationTokenSource? _readCts;

    public IrcClient(IrcOptions options, ILogger<IrcClient> logger)
    {
        _options = options;
        _logger = logger;
    }

    public event Func<string, string, string, string, Task>? MessageReceived;

    public event Func<bool, string?, Task>? ConnectionChanged;

    public bool IsConnected => _tcpClient?.Connected == true;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await DisconnectAsync();

        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(_options.Server, _options.Port, cancellationToken);

        Stream stream = _tcpClient.GetStream();
        if (_options.UseTls)
        {
            var ssl = new SslStream(stream, leaveInnerStreamOpen: false);
            await ssl.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = _options.Server
                },
                cancellationToken);
            stream = ssl;
        }

        _stream = stream;
        _readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await SendAsync($"NICK {_options.Nick}", cancellationToken);
        await SendAsync($"USER {_options.Nick} 0 * :{_options.RealName}", cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Password))
        {
            await SendAsync($"PASS {_options.Password}", cancellationToken);
        }

        _ = Task.Run(() => ReadLoopAsync(_readCts.Token), CancellationToken.None);

        if (ConnectionChanged is not null)
        {
            await ConnectionChanged.Invoke(true, null);
        }
    }

    public async Task JoinChannelsAsync(CancellationToken cancellationToken)
    {
        foreach (var channel in _options.Channels)
        {
            if (!string.IsNullOrWhiteSpace(channel))
            {
                await SendAsync($"JOIN {channel.Trim()}", cancellationToken);
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_readCts is not null)
        {
            _readCts.Cancel();
            _readCts.Dispose();
            _readCts = null;
        }

        if (_stream is not null)
        {
            await _stream.DisposeAsync();
            _stream = null;
        }

        if (_tcpClient is not null)
        {
            _tcpClient.Dispose();
            _tcpClient = null;
        }

        if (ConnectionChanged is not null)
        {
            await ConnectionChanged.Invoke(false, null);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        using var reader = new StreamReader(_stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                await HandleLineAsync(line, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IRC read loop failed");
            if (ConnectionChanged is not null)
            {
                await ConnectionChanged.Invoke(false, ex.Message);
            }
        }
    }

    private async Task HandleLineAsync(string line, CancellationToken cancellationToken)
    {
        if (IrcLineParser.IsPing(line, out var token))
        {
            await SendAsync($"PONG {token}", cancellationToken);
            return;
        }

        if (IrcLineParser.TryParsePrivmsg(line, out var channel, out var sender, out var text)
            && MessageReceived is not null)
        {
            await MessageReceived.Invoke(_options.Server, channel, sender, text);
        }
    }

    private async Task SendAsync(string command, CancellationToken cancellationToken)
    {
        if (_stream is null)
        {
            return;
        }

        var payload = Encoding.UTF8.GetBytes(command + "\r\n");
        await _stream.WriteAsync(payload, cancellationToken);
    }
}

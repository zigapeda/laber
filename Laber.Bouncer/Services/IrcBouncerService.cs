using Laber.Bouncer.Irc;
using Laber.Shared.Data;
using Laber.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Laber.Bouncer.Services;

public sealed class IrcBouncerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IrcOptions _options;
    private readonly ILogger<IrcBouncerService> _logger;

    public IrcBouncerService(
        IServiceProvider serviceProvider,
        IOptions<IrcOptions> options,
        ILogger<IrcBouncerService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await EnsureDatabaseAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IrcClient>>();
            await using var client = new IrcClient(_options, logger);

            client.MessageReceived += OnMessageReceivedAsync;
            client.ConnectionChanged += OnConnectionChangedAsync;

            try
            {
                _logger.LogInformation(
                    "Connecting to {Server}:{Port} as {Nick}",
                    _options.Server,
                    _options.Port,
                    _options.Nick);

                await client.ConnectAsync(stoppingToken);
                await client.JoinChannelsAsync(stoppingToken);

                while (!stoppingToken.IsCancellationRequested && client.IsConnected)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IRC bouncer connection failed");
                await UpdateConnectionStateAsync(false, ex.Message, stoppingToken);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private Task OnMessageReceivedAsync(string server, string channel, string sender, string text)
    {
        return PersistMessageAsync(channel, sender, text);
    }

    private async Task PersistMessageAsync(string channel, string sender, string text)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LaberDbContext>();

        db.Messages.Add(new IrcMessage
        {
            Channel = channel,
            Sender = sender,
            Text = text,
            ReceivedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(CancellationToken.None);
        _logger.LogDebug("Stored message in {Channel} from {Sender}", channel, sender);
    }

    private Task OnConnectionChangedAsync(bool isConnected, string? error)
    {
        return UpdateConnectionStateAsync(isConnected, error, CancellationToken.None);
    }

    private async Task UpdateConnectionStateAsync(
        bool isConnected,
        string? error,
        CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LaberDbContext>();
        var state = await db.ConnectionState.FirstOrDefaultAsync(s => s.Id == 1, cancellationToken);

        if (state is null)
        {
            state = new IrcConnectionState { Id = 1 };
            db.ConnectionState.Add(state);
        }

        state.IsConnected = isConnected;
        state.Server = _options.Server;
        state.Port = _options.Port;
        state.Nick = _options.Nick;
        state.LastError = error;
        if (isConnected)
        {
            state.LastConnectedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDatabaseAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LaberDbContext>();
        await db.Database.EnsureCreatedAsync(cancellationToken);

        if (!await db.ConnectionState.AnyAsync(cancellationToken))
        {
            db.ConnectionState.Add(new IrcConnectionState
            {
                Id = 1,
                Server = _options.Server,
                Port = _options.Port,
                Nick = _options.Nick
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

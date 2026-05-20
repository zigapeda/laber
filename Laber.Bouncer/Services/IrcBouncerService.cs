using Laber.Bouncer.Irc;
using Laber.Shared.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var status = scope.ServiceProvider.GetRequiredService<ConnectionStatusService>();
            var store = scope.ServiceProvider.GetRequiredService<IMessageStore>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<IrcClient>>();
            await using var client = new IrcClient(_options, logger);

            client.MessageReceived += (_, channel, sender, text) =>
                store.AppendAsync(channel, sender, text, stoppingToken);

            client.ConnectionChanged += (isConnected, error) =>
            {
                if (isConnected)
                {
                    status.SetConnected();
                }
                else
                {
                    status.SetDisconnected(error);
                }

                return Task.CompletedTask;
            };

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
                _logger.LogError(ex, "IRC connection failed");
                status.SetDisconnected(ex.Message);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}

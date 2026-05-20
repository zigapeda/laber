using Laber.Bouncer.Irc;
using Laber.Bouncer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Laber.Bouncer;

public static class BouncerServiceCollectionExtensions
{
    public static IHostApplicationBuilder AddLaberBouncer(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<IrcOptions>(
            builder.Configuration.GetSection(IrcOptions.SectionName));
        builder.Services.AddSingleton<ConnectionStatusService>();
        builder.Services.AddHostedService<IrcBouncerService>();
        return builder;
    }
}

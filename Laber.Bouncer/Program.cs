using Laber.Bouncer.Irc;
using Laber.Bouncer.Services;
using Laber.Shared.Data;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<IrcOptions>(
    builder.Configuration.GetSection(IrcOptions.SectionName));

builder.Services.AddDbContext<LaberDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("laber")
        ?? "Data Source=laber.db"));

builder.Services.AddHostedService<IrcBouncerService>();

var host = builder.Build();
host.Run();

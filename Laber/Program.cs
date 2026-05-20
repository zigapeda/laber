using Laber.Irc;
using Laber.Services;
using Laber.Storage;
using Laber.Shared.Dtos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<IrcOptions>(builder.Configuration.GetSection(IrcOptions.SectionName));
builder.Services.Configure<MessageStoreOptions>(builder.Configuration.GetSection(MessageStoreOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageStoreOptions>>().Value;
    return new MessageStore(options);
});
builder.Services.AddSingleton<ConnectionStatusService>();
builder.Services.AddHostedService<IrcBouncerService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials());
});

var app = builder.Build();

app.UseCors();

var api = app.MapGroup("/api");

api.MapGet("/status", (ConnectionStatusService status) =>
    Results.Ok(status.GetStatus()));

api.MapGet("/events", (ConnectionStatusService status) =>
    Results.Ok(status.GetRecentEvents()));

api.MapGet("/channels", async (MessageStore store, long? sinceId, CancellationToken ct) =>
    Results.Ok(await store.ListChannelsAsync(sinceId, ct)));

api.MapGet("/messages/{channel}", async (
    string channel,
    MessageStore store,
    long? afterId,
    long? beforeId,
    int? limit,
    CancellationToken ct) =>
{
    var messages = await store.ReadAsync(
        channel,
        limit ?? 100,
        afterId,
        beforeId,
        ct);
    return Results.Ok(messages);
});

api.MapGet("/messages", async (MessageStore store, long? afterId, int? limit, CancellationToken ct) =>
    Results.Ok(await store.ReadAllNewAsync(afterId, limit ?? 100, ct)));

app.Run();

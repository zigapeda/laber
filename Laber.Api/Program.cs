using System.Text.Json;
using Laber.Api.Storage;
using Laber.Api.Streaming;
using Laber.Bouncer;
using Laber.Bouncer.Services;
using Laber.Shared.Dtos;
using Laber.Shared.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddLaberBouncer();

builder.Services.Configure<MessageStoreOptions>(
    builder.Configuration.GetSection(MessageStoreOptions.SectionName));
builder.Services.AddSingleton<MessageStreamService>();
builder.Services.AddSingleton<MessageStore>();
builder.Services.AddSingleton<IMessageStore>(sp => sp.GetRequiredService<MessageStore>());

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials());
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseCors();

var api = app.MapGroup("/api");

api.MapGet("/status", (ConnectionStatusService status) =>
    Results.Ok(status.GetStatus()));

api.MapGet("/events", (ConnectionStatusService status) =>
    Results.Ok(status.GetRecentEvents()));

api.MapGet("/channels", async (IMessageStore store, long? sinceId, CancellationToken ct) =>
    Results.Ok(await store.ListChannelsAsync(sinceId, ct)));

api.MapGet("/messages/{channel}", async (
    string channel,
    IMessageStore store,
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

api.MapGet("/messages", async (IMessageStore store, long? afterId, int? limit, CancellationToken ct) =>
    Results.Ok(await store.ReadAllNewAsync(afterId, limit ?? 100, ct)));

api.MapGet("/messages/stream", async (
    HttpContext context,
    MessageStreamService stream,
    string? channel,
    CancellationToken ct) =>
{
    var filter = string.IsNullOrWhiteSpace(channel)
        ? null
        : ChannelPath.NormalizeChannel(channel);

    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers.Connection = "keep-alive";
    context.Response.ContentType = "text/event-stream";

    var reader = stream.Reader;
    using var heartbeat = new PeriodicTimer(TimeSpan.FromSeconds(15));

    while (!ct.IsCancellationRequested)
    {
        var waitRead = reader.WaitToReadAsync(ct).AsTask();
        var waitHeartbeat = heartbeat.WaitForNextTickAsync(ct).AsTask();
        var completed = await Task.WhenAny(waitRead, waitHeartbeat);

        if (completed == waitHeartbeat)
        {
            await context.Response.WriteAsync(": ping\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
            continue;
        }

        if (!await waitRead)
        {
            break;
        }

        while (reader.TryRead(out var message))
        {
            if (filter is not null && message.Channel != filter)
            {
                continue;
            }

            var json = JsonSerializer.Serialize(message);
            await context.Response.WriteAsync($"data: {json}\n\n", ct);
            await context.Response.Body.FlushAsync(ct);
        }
    }
});

app.Run();

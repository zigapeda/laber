using Laber.Shared.Data;
using Laber.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<LaberDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("laber")
        ?? "Data Source=laber.db"));

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LaberDbContext>();
    await db.Database.EnsureCreatedAsync();
}

var api = app.MapGroup("/api");

api.MapGet("/status", async (LaberDbContext db, CancellationToken ct) =>
{
    var state = await db.ConnectionState.AsNoTracking()
        .FirstOrDefaultAsync(s => s.Id == 1, ct);

    if (state is null)
    {
        return Results.Ok(new ConnectionStatusDto(false, string.Empty, 0, string.Empty, null, null));
    }

    return Results.Ok(new ConnectionStatusDto(
        state.IsConnected,
        state.Server,
        state.Port,
        state.Nick,
        state.LastConnectedAt,
        state.LastError));
});

api.MapGet("/channels", async (LaberDbContext db, long? sinceId, CancellationToken ct) =>
{
    var hasSince = sinceId.HasValue && sinceId.Value > 0;
    var query = db.Messages.AsNoTracking();
    if (hasSince)
    {
        query = query.Where(m => m.Id > sinceId);
    }

    var channels = await query
        .GroupBy(m => m.Channel)
        .Select(g => new ChannelDto(
            g.Key,
            hasSince ? g.Count() : 0,
            g.Max(m => m.ReceivedAt)))
        .OrderByDescending(c => c.LastMessageAt)
        .ToListAsync(ct);

    return Results.Ok(channels);
});

api.MapGet("/messages/{channel}", async (
    string channel,
    LaberDbContext db,
    long? afterId,
    int? limit,
    CancellationToken ct) =>
{
    var normalized = channel.StartsWith('#') ? channel : "#" + channel;
    var take = Math.Clamp(limit ?? 100, 1, 500);

    var query = db.Messages.AsNoTracking()
        .Where(m => m.Channel == normalized || m.Channel == channel);

    if (afterId is > 0)
    {
        query = query.Where(m => m.Id > afterId);
    }

    var messages = await query
        .OrderBy(m => m.Id)
        .Take(take)
        .Select(m => new MessageDto(m.Id, m.Channel, m.Sender, m.Text, m.ReceivedAt))
        .ToListAsync(ct);

    return Results.Ok(messages);
});

api.MapGet("/messages", async (LaberDbContext db, long? afterId, int? limit, CancellationToken ct) =>
{
    var take = Math.Clamp(limit ?? 100, 1, 500);
    var query = db.Messages.AsNoTracking();

    if (afterId is > 0)
    {
        query = query.Where(m => m.Id > afterId);
    }

    var messages = await query
        .OrderBy(m => m.Id)
        .Take(take)
        .Select(m => new MessageDto(m.Id, m.Channel, m.Sender, m.Text, m.ReceivedAt))
        .ToListAsync(ct);

    return Results.Ok(messages);
});

app.Run();

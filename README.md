# Laber

IRC-Bouncer und REST-API: **Laber.Api** hostet HTTP + SSE, **Laber.Bouncer** enthält nur die IRC-Logik (Class Library, eingebunden in die Api). Entwicklung startet alles über **Aspire AppHost**.

Nachrichten: Plain-Text pro Kanal und Jahr unter `data/<kanal>/<jahr>.txt`. Connect/Disconnect nur im RAM.

## Struktur

```
Laber.slnx
Laber.AppHost/         # Aspire: Api + Frontend
Laber.Api/             # eine Executable (HTTP, SSE, speichert Nachrichten)
Laber.Bouncer/         # IRC-Logik (kein Program.cs)
Laber.Shared/          # DTOs, IMessageStore, Dateiformat
Laber.ServiceDefaults/ # Aspire-Defaults
Laber.Frontend/        # Ionic PWA
data/                  # Nachrichten-Logfiles
```

## Start (Entwicklung)

```bash
dotnet run --project Laber.AppHost
```

Nur Backend:

```bash
dotnet run --project Laber.Api
```

## Echtzeit-Nachrichten (SSE)

`GET /api/messages/stream` — alle Kanäle  
`GET /api/messages/stream?channel=laber` — ein Kanal

Frontend nutzt `EventSource` statt Polling.

## IRC (`Laber.Api/appsettings.json`)

```json
"Irc": {
  "Server": "irc.libera.chat",
  "Port": 6697,
  "UseTls": true,
  "Nick": "laber",
  "Channels": [ "#dein-kanal" ]
}
```

## API (Auszug)

| Endpoint | Beschreibung |
|----------|--------------|
| `GET /api/messages/stream` | SSE: neue Nachrichten sofort |
| `GET /api/messages/{channel}?limit=` | Verlauf (Endless Scroll: `beforeId`) |
| `GET /api/status` | Verbindungsstatus (RAM) |
| `GET /api/channels` | Kanalliste aus `data/` |

## Build

```bash
dotnet build Laber.slnx
cd Laber.Frontend && npm run build
```

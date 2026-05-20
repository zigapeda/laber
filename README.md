# Laber

IRC-Bouncer und REST-API in **einer** .NET-Anwendung. Nachrichten werden als Plain-Text pro Kanal und Jahr gespeichert (`data/<kanal>/<jahr>.txt`). Connect/Disconnect-Ereignisse bleiben nur kurz im RAM.

**Frontend:** Ionic 8 + Angular 21 PWA

## Struktur

```
Laber.slnx
Laber/                 # eine Executable: IRC-Bouncer + REST-API
Laber.Shared/          # DTOs, Dateiformat, Kanal-Pfade
Laber.Frontend/        # Ionic PWA
data/                  # Nachrichten-Logfiles (nicht in Git)
```

## Dateiformat

Pro Zeile eine Nachricht (Tab-getrennt):

```
2025-05-20T14:30:45.1234567+00:00\tsender\ttext
```

Sonderzeichen im Text werden escaped (`\n`, `\t`, `\\`).

Beispiel-Pfad: `data/laber/2025.txt` für Kanal `#laber`.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/) (nur für Frontend)

## Start

```bash
dotnet run --project Laber
```

Standard-URL: `http://localhost:5292`

### IRC (`Laber/appsettings.json`)

```json
"Irc": {
  "Server": "irc.libera.chat",
  "Port": 6697,
  "UseTls": true,
  "Nick": "laber",
  "Channels": [ "#dein-kanal" ]
}
```

### Frontend

```bash
cd Laber.Frontend
npm install
npm start
```

`proxy.conf.json` leitet `/api` an `http://localhost:5292` weiter.

## API

| Endpoint | Beschreibung |
|----------|--------------|
| `GET /api/status` | aktueller IRC-Verbindungsstatus (RAM) |
| `GET /api/events` | Connect/Disconnect der letzten 24h (RAM, max. 200) |
| `GET /api/channels?sinceId=` | Kanäle aus dem `data/`-Verzeichnis |
| `GET /api/messages/{channel}?limit=` | neueste Nachrichten (Standard 100) |
| `GET /api/messages/{channel}?beforeId=` | ältere Nachrichten (Endless Scroll) |
| `GET /api/messages/{channel}?afterId=` | neuere Nachrichten seit ID |
| `GET /api/messages?afterId=` | neue Nachrichten aller Kanäle |

## Build

```bash
dotnet build Laber.slnx
cd Laber.Frontend && npm run build
```

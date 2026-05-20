# Laber

IRC-Client mit **Backend-Bouncer** (immer online, sammelt Nachrichten) und **Ionic/Angular-PWA** (asynchroner Abruf, kein permanenter Client-Betrieb nötig). Orchestriert mit **.NET Aspire**.

## Struktur

```
Laber.slnx
Laber.AppHost/        # Aspire-Orchestrierung
Laber.Api/            # REST-API für Sync
Laber.Bouncer/        # IRC-Bouncer (Worker)
Laber.ServiceDefaults/
Laber.Shared/         # EF Core + DTOs
Laber.Frontend/       # Ionic 8 + Angular 21 PWA
data/                 # gemeinsame SQLite-DB (lokal, via AppHost)
```

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)

## Start mit Aspire

```bash
dotnet run --project Laber.AppHost
```

Das Aspire-Dashboard zeigt die Endpunkte für **api**, **bouncer** und **frontend**.

Die SQLite-Datenbank liegt unter `data/laber.db` (beide Backend-Services teilen sich die Datei).

## IRC-Konfiguration (Bouncer)

`Laber.Bouncer/appsettings.json`:

```json
"Irc": {
  "Server": "irc.libera.chat",
  "Port": 6697,
  "UseTls": true,
  "Nick": "laber",
  "Channels": [ "#dein-kanal" ]
}
```

> Eindeutigen Nick wählen; für Libera ggf. SASL/Passwort ergänzen.

## Frontend

- **Ionic 8** + **Angular 21**
- **Dark / Light / System**-Theme (Einstellungen)
- **PWA** installierbar (`ng build` mit Service Worker)
- Pollt `/api/messages` – letzte Message-ID wird lokal gespeichert

### Nur Frontend (mit API-Proxy)

```bash
cd Laber.Frontend
npm install
npm start
```

API-URL in den Einstellungen oder `src/environments/environment.ts` anpassen. `proxy.conf.json` leitet `/api` an `http://localhost:5292` weiter.

## API (Auszug)

| Endpoint | Beschreibung |
|----------|--------------|
| `GET /api/status` | Bouncer-Verbindungsstatus |
| `GET /api/channels?sinceId=` | Kanäle (+ optional ungelesen) |
| `GET /api/messages/{channel}?afterId=` | Nachrichten eines Kanals |
| `GET /api/messages?afterId=` | Alle neuen Nachrichten |

## Sprache

Anwendungscode (Api, Bouncer, Shared): **C# 10** (`LangVersion` 10). AppHost nutzt das Aspire-SDK (generierter Code benötigt neuere Sprachfeatures).

## Build

```bash
dotnet build Laber.slnx
cd Laber.Frontend && npm run build
```

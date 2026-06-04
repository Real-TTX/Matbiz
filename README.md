# Matbiz

Modulares ERP-System (Greenfield). Stack: **.NET 10 · ASP.NET Core Razor Pages + htmx · PostgreSQL · Docker**.

## Inhalt

- [Schnellstart Lokal](#schnellstart-lokal)
- [Deployment im Homelab](#deployment-im-homelab)
- [Lokale Entwicklung ohne Docker](#lokale-entwicklung-ohne-docker)
- [Projektstruktur](#projektstruktur)
- [Features](#features)
- [Remote-Zugriff — Sicherheitsmodell](#remote-zugriff--sicherheitsmodell)
- [Konfiguration](#konfiguration)
- [CI / Container Images](#ci--container-images)

## Schnellstart Lokal

```bash
cp .env.example .env       # Passwörter / Port anpassen
docker compose up -d --build
```

→ http://localhost:8040 — Login: `admin@matbiz.local` / `ChangeMe!2026`

Beim ersten Start werden Datenbank-Migrationen automatisch angewendet und der Admin
angelegt. `MATBIZ_SEED_SAMPLE_DATA=true` in `.env` füllt die Demo-Kunden / -Firmen / -Aufgaben.

## Deployment im Homelab

Eigene Compose-Datei [`docker-compose.homelab.yml`](docker-compose.homelab.yml) zieht
das vorgebaute Image aus GHCR — kein lokaler Build, kein .NET-SDK auf dem Server nötig.

```bash
# 1) Konfig vorbereiten
cp .env.example .env
# WICHTIG: in .env die Passwörter ändern! POSTGRES_PASSWORD und MATBIZ_ADMIN_PASSWORD
#          haben kein Default — Compose schlägt fehl wenn die nicht gesetzt sind.
# Außerdem MATBIZ_SEED_SAMPLE_DATA=false setzen für Produktivumgebung.

# 2) Starten
docker compose -f docker-compose.homelab.yml up -d

# 3) Updates ziehen
docker compose -f docker-compose.homelab.yml pull
docker compose -f docker-compose.homelab.yml up -d
```

Unterschiede zur Entwickler-Compose:

- Postgres-Port ist **nicht** nach außen freigegeben
- `restart: always` auf beiden Containern
- Image kommt aus `ghcr.io/real-ttx/matbiz:${MATBIZ_IMAGE_TAG:-latest}`
- Health-Check auf der Web-App
- Beispiel-Traefik-Labels im Compose auskommentiert — bei Bedarf einkommentieren

Versions-Pinning: in `.env` `MATBIZ_IMAGE_TAG=sha-abc1234` oder `v1.0.0` setzen, dann
zieht `docker compose pull` nur diese eine Version.

## Lokale Entwicklung ohne Docker

```bash
docker compose up -d db                # nur Postgres im Container
dotnet run --project src/Matbiz.Web    # App läuft auf https://localhost:5001
```

EF-Migration erstellen:

```bash
dotnet ef migrations add <Name> --project src/Matbiz.Web --output-dir Data/Migrations
```

## Projektstruktur

```
src/Matbiz.Web/
├── Pages/                       Razor Pages (eine pro Route)
│   ├── Shared/                  _Layout, NavMenu, Picker, …
│   ├── Account/                 Login / Logout / Manage
│   ├── Customers/               Kontakte (Liste, Detail, Gruppen, Felder)
│   ├── Companies/               Firmen
│   ├── Tasks/                   Aufgaben
│   ├── Teams/                   Teams
│   ├── Departments/             Abteilungen
│   ├── Users/                   Benutzerverwaltung
│   └── System/                  Branding / Aussehen
├── Modules/                     Domänen-Logik (Models + Services)
│   ├── Customers/               Customer, Company, Tag, CustomerGroup, …
│   ├── Tasks/                   TaskItem + History
│   ├── Teams/                   Team, Department
│   ├── Files/                   AttachedFile (polymorph)
│   ├── SystemSettings/          Branding
│   └── Dashboard/               DashboardConfig
├── Data/                        DbContext, Identity, Migrationen, SampleDataSeeder
├── Impersonation/               Server-seitige Remote-Zugriff-Logik
├── Shared/                      ICurrentUserAccessor, ListPreferences
├── Resources/                   .resx-Localization-Files
├── wwwroot/css/site.css         Globales Stylesheet
└── Program.cs                   Startup + Service-Registrierung
```

Module greifen **nicht** direkt aufeinander zu — wenn Modul A Daten von Modul B
braucht, läuft das über einen Service-Vertrag (siehe `Shared/`).

## Features

- **Kontakte** (vorher „Kunden") mit Stammdaten, Tags, eigenen Feldern (inkl. Datei-Typ),
  Historie mit Suche/Sort/Attachments, Dateiablage, Aufgaben-Verknüpfung
- **Firmen** als eigene Entität mit Adresse, Tags, Historie + Datei-Ablage. Kontakte
  können entweder eine strukturierte Firma referenzieren oder Freitext-Name behalten
- **Gruppen** (Kontakte ODER Firmen, statisch oder dynamisch mit Regel-Builder)
- **Aufgaben** mit Status / Priorität / Fälligkeit, Zuweisung an User oder Team,
  optionaler Kontakt-Verknüpfung, Auto-Eintrag in Kontakt-Historie bei Abschluss,
  eigener Audit-Historie pro Aufgabe
- **Benutzer / Rollen / Teams / Abteilungen** mit Hierarchie
- **Remote-Zugriff (Impersonation)**: Admin kann im Namen eines Users arbeiten,
  serverseitig erzwungen, rotes Banner über volle Seitenbreite, Audit-Trail
- **Personalisiertes Dashboard** mit konfigurierbaren Widgets (Reihenfolge, Sichtbarkeit, Anzahl)
- **Branding**: App-Name, Primärfarbe + 2 Akzentfarben, Logo-Upload, persönliches Theme
  (Hell / Dunkel / System) pro User
- **Spalten-Konfiguration**: pro Liste auswählbar, welche Spalten sichtbar sind — gespeichert
  pro Benutzer
- Authentifizierung über **ASP.NET Core Identity** (Cookie-Auth)
- i18n-ready (`.resx`)

## Remote-Zugriff — Sicherheitsmodell

Server-seitig erzwungen, kein Client-Switch:

1. Admin klickt „Remote-Zugriff" → Form-POST an `/impersonation/start`
2. `ImpersonationService` schreibt DB-Eintrag `ImpersonationSession`
3. `ImpersonationClaimsTransformation` (IClaimsTransformation) baut bei jedem
   Folge-Request den `ClaimsPrincipal` als Ziel-User auf, plus Marker-Claims
   `matbiz:impersonator_id/name` für Banner und Audit
4. „Trennen" via POST `/impersonation/end` setzt `EndedAt`

Audit-Spalte `OnBehalfOfAdminId` wird in History-Tabellen mitgeschrieben.

## Konfiguration

| ENV | Default | Beschreibung |
|---|---|---|
| `MATBIZ_PORT` | `8040` | Host-Port der Web-App |
| `POSTGRES_DB` / `_USER` / `_PASSWORD` | `matbiz` | Postgres-Credentials |
| `MATBIZ_ADMIN_EMAIL` | `admin@matbiz.local` | Initial-Admin (nur erste Inbetriebnahme) |
| `MATBIZ_ADMIN_PASSWORD` | `ChangeMe!2026` | Initial-Admin-Passwort |
| `MATBIZ_SEED_SAMPLE_DATA` | `false` | Demo-Daten beim Start anlegen |
| `MATBIZ_IMAGE_TAG` | `latest` | Image-Tag aus GHCR (nur Homelab-Compose) |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Standard ASP.NET Env |

## CI / Container Images

GitHub Actions [`.github/workflows/ci.yml`](.github/workflows/ci.yml):

- Build + Test auf jedem Push / PR
- Push auf `main` oder Tag `v*` → Image-Publish nach
  `ghcr.io/real-ttx/matbiz:latest` und `:sha-<short>` / `:v<version>`

Image manuell ziehen:

```bash
docker pull ghcr.io/real-ttx/matbiz:latest
```

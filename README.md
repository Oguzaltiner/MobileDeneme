# MobileDeneme

English vocabulary learning platform: React Native mobile client, ASP.NET Core API, and PostgreSQL.

## Current foundation

- `mobile/`: Expo + TypeScript shell, React Navigation, TanStack Query, Zustand, NativeWind/Tailwind tokens, and typed API client.
- `backend/`: .NET 10 modular solution with API, Application, Domain, and Infrastructure projects.
- `docker-compose.yml`: local PostgreSQL 16 service.
- `backend/Dockerfile`: production-style multi-stage ASP.NET container (non-root runtime).
- `.github/workflows/ci.yml`: backend/mobile CI and dependency vulnerability audit.
- `docs/OVERNIGHT_PROGRESS.md`: product, security, release, and verification summary.
- `docs/PROJECT_ANALYSIS.md`: product and architecture baseline.

## Prerequisites

- Node.js 24 LTS (or the current Expo-supported LTS)
- .NET SDK 10.0.401 or newer 10.0.x SDK
- Docker Desktop with Compose

## PostgreSQL

PostgreSQL 15 is already installed as the local Windows service `postgresql-x64-15` and listens on `localhost:5432`. The default development connection string uses that service. Create the `english_learning` database and configure the local password through user secrets or a local `.env` file; never commit the real password.

If a disposable Docker database is preferred instead, use the optional service below. It maps container PostgreSQL to host port `5433` so it does not conflict with the installed service.

```powershell
docker compose up -d postgres
```

API and PostgreSQL together:

```powershell
docker compose up --build
```

The API is exposed at `http://localhost:5057` in local Compose. Set `JWT_SIGNING_KEY` in a local `.env` before using Compose beyond disposable development.

Production override (requires a strong secret and a separate migration step):

```powershell
$env:JWT_SIGNING_KEY = "replace-with-a-strong-secret-at-least-32-chars"
docker compose -f docker-compose.yml -f docker-compose.production.yml up --build -d
```

## Run the API

```powershell
$env:DOTNET_CLI_HOME = "$PWD/.dotnet-home"
# Replace the username/password with the credentials configured in pgAdmin.
$env:ConnectionStrings__Postgres = "Host=localhost;Port=5432;Database=english_learning;Username=<user>;Password=<password>"
dotnet run --project backend/src/EnglishLearning.Api
```

Health and API info endpoints:

- `GET http://localhost:5000/health`
- `GET http://localhost:5000/api/v1/info`

## Run the mobile app

```powershell
cd mobile
npm install
npm start
```

For device builds, use the EAS profiles in `mobile/eas.json`:

```powershell
npx eas build --profile development --platform android
npx eas build --profile development --platform ios
```

Set `EXPO_PUBLIC_API_BASE_URL` in a local `.env` when the API is not reachable at the default URL. Never commit real secrets; `.env.example` is the template.

## Branches

- `dev`: integration and active development.
- `master`: release/mainline branch.

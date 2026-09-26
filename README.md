# MobileDeneme

English vocabulary learning platform: React Native mobile client, ASP.NET Core API, and PostgreSQL.

## Current foundation

- `mobile/`: Expo + TypeScript shell, React Navigation, TanStack Query, Zustand, NativeWind/Tailwind tokens, and typed API client.
- `backend/`: .NET 10 modular solution with API, Application, Domain, and Infrastructure projects.
- `docker-compose.yml`: local PostgreSQL 16 service.
- `docs/PROJECT_ANALYSIS.md`: product and architecture baseline.

## Prerequisites

- Node.js 24 LTS (or the current Expo-supported LTS)
- .NET SDK 10.0.401 or newer 10.0.x SDK
- Docker Desktop with Compose

## Run local PostgreSQL

```powershell
docker compose up -d postgres
```

## Run the API

```powershell
$env:DOTNET_CLI_HOME = "$PWD/.dotnet-home"
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

Set `EXPO_PUBLIC_API_BASE_URL` in a local `.env` when the API is not reachable at the default URL. Never commit real secrets; `.env.example` is the template.

## Branches

- `dev`: integration and active development.
- `master`: release/mainline branch.


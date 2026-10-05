# Opportunity Lifecycle CRM

Production-grade CRM for a Presales & Bids department: receive → qualify → propose → approve → submit → contract.

Two projects in this repository:

| Project | Stack | Folder |
|---|---|---|
| Backend REST API | .NET 8, EF Core 8, SQL Server, MediatR | `/backend` |
| Frontend SPA | Angular 19, Material, ngx-translate (EN/AR + RTL) | `/frontend` |

## Prerequisites

- .NET 8 SDK
- Node.js 20+
- SQL Server 2022 **or** Docker Desktop (recommended)
- Angular CLI is optional (`npx ng` is enough)

## Two-command local startup (Docker)

```bash
docker compose up --build
```

Then open:

- App: http://localhost:8088
- API / Swagger: http://localhost:5088/swagger

Seeded demo users (password for all: `Passw0rd!`):

| Email | Roles |
|---|---|
| `admin@crm.local` | ADMIN |
| `am.sara@crm.local` | AM |
| `bids.presales@crm.local` | BIDS_PRESALES |
| `bids.mgmt@crm.local` | BIDS_MGMT |
| `presales.omar@crm.local` | PRESALES |
| `sl.noura@crm.local` | SL |
| `mgmt.fahad@crm.local` | MGMT |
| `am.khalid@crm.local` | AM |
| `sl.yousef@crm.local` | SL + PRESALES |
| `bids.layla@crm.local` | BIDS_PRESALES + BIDS_MGMT |

## Local startup without Docker

### 1. SQL Server

Start SQL Server (or `docker compose up sqlserver`) and confirm `ConnectionStrings:DefaultConnection` in `backend/src/Crm.Api/appsettings.json`.

Default:

```
Server=localhost,1433;Database=CrmDb;User Id=sa;Password=Your_password123;TrustServerCertificate=true;MultipleActiveResultSets=true
```

### 2. Backend

**Requires SQL Server** (LocalDB, Express, or Docker). The default connection uses **LocalDB**:
`(localdb)\mssqllocaldb`

If you use Docker SQL instead, start it first:
```bash
docker compose up -d sqlserver
```
Then run the API with the `docker-sql` profile:
```bash
cd backend
dotnet run --project src/Crm.Api --launch-profile docker-sql
```

For LocalDB / SQL Express (default):
```bash
cd backend
dotnet run --project src/Crm.Api --launch-profile http
```

The API listens on **http://localhost:5088**. On first run `DatabaseSeeder` applies migrations and seeds lookups, demo users, 15 opportunities, notes, comments, and audit history.

### 3. Frontend

```bash
cd frontend
npm install
npm start
```

The SPA listens on **http://localhost:4200** and proxies `/api` to the API.

## How to run migrations

```bash
cd backend
dotnet ef migrations add <Name> --project src/Crm.Infrastructure --startup-project src/Crm.Api
dotnet ef database update --project src/Crm.Infrastructure --startup-project src/Crm.Api
```

Install the tool once: `dotnet tool install --global dotnet-ef`.

## How to seed

Seeding is **idempotent** and runs automatically on API startup via `DatabaseSeeder`. It is a no-op when roles/opportunities already exist. To re-seed, drop the database and restart the API.

## How to run tests

```bash
cd backend
dotnet test

cd ../frontend
npm test -- --watch=false
```

## Environment variables

| Variable | Purpose | Default |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server | see appsettings |
| `Authentication__Jwt__Key` | JWT signing key (≥ 32 chars) | dev key |
| `Authentication__Jwt__Issuer` | JWT issuer | `crm` |
| `Authentication__Jwt__Audience` | JWT audience | `crm` |
| `Authentication__EntraId__Enabled` | Enable Entra ID scheme | `false` |
| `Cors__Origins__0` | Allowed SPA origin | `http://localhost:4200` |
| `Email__SendingEnabled` | Wire real SMTP (`false` → NullEmailSender) | `false` |
| `Storage__Root` | Attachment disk root | `storage` |

## Enabling SSO later

SSO is **scaffolded but disabled**. Do not enable it until JWT is verified in production-like conditions. Search the repo for `TODO(SSO)`:

| Location | What to do later |
|---|---|
| `backend/src/Crm.Api/Program.cs` | Register Entra ID / OpenID Connect JWT bearer **only when** `Authentication:EntraId:Enabled` is true |
| `backend/src/Crm.Api/Controllers/AuthController.cs` | `GET /api/v1/auth/sso/config` and `GET /api/v1/auth/sso/challenge` (currently 501) |
| `backend/src/Crm.Api/appsettings.json` → `Authentication:EntraId` | Fill `TenantId`, `ClientId`, `Authority` |
| Frontend login page | SSO button is rendered but disabled with tooltip "Coming soon" |
| User provisioning | Stub: create local user on first Entra login and map group claims → role codes |

Activation is a configuration change plus filling the stubs — not a rewrite. Email sending is the same pattern: `Email:SendingEnabled` defaults to `false` and uses `NullEmailSender`.

## Workflow notes

The Visio source had three defects. The engine implements the **corrected** flow (see `CRM_Build_Prompt.md` §7.2) with code comments at each correction in `WorkflowEngine`.

Bid bond is a **parallel side-track**. It never terminates the main flow. Submission is blocked (HTTP 422) while a required bid bond is not `Issued`.

# AI Trip Planner

A full-stack AI travel planning application.

- **Frontend:** Angular 20 (TypeScript, Angular Router, Reactive Forms)
- **Backend:** ASP.NET Core Web API (.NET 8, C#)
- **Data:** MongoDB Atlas (MongoDB.Driver)
- **AI/Automation:** n8n workflows (planned; backend uses dedicated `n8n` services, no direct Angular-to-n8n calls)

## Architecture

```
Angular --> ASP.NET Core Web API --> MongoDB Atlas

ASP.NET Core Web API --> n8n Webhook --> AI Agent --> (travel APIs) --> n8n --> ASP.NET Core --> Angular
```

## Project layout

```
backend/
  TripPlanner.sln
  src/
    TripPlanner.Api/            ASP.NET Core Web API (controllers, middleware, Program.cs)
    TripPlanner.Application/    DTOs, interfaces, business services
    TripPlanner.Domain/         Entities, enums
    TripPlanner.Infrastructure/ MongoDB context, collections, indexes/seed initializer,
                                repositories and external services (incl. n8n)
frontend/
  src/app/
    core/        services, guards, interceptors, models
    features/    auth | dashboard | trips | itinerary | chatbot | admin | landing
    shared/      shared components, models
```

## Prerequisites

- .NET SDK 8
- Node.js 22+ and npm
- A MongoDB Atlas cluster (free tier is enough)

## 1. Database (MongoDB Atlas)

1. Create a free cluster at <https://cloud.mongodb.com>.
2. Create a database user and allow your IP under **Network Access**.
3. Copy the connection string (**Connect > Drivers**), e.g.
   `mongodb+srv://<user>:<password>@<cluster>.mongodb.net/?retryWrites=true&w=majority`.

No schema/migration step is required. On startup the API creates the needed indexes
and seeds the `User` / `Admin` roles (idempotent).

Collections used: `users`, `roles`, `user_roles`, `trips`.

## 2. Backend

Configure the connection string in
`backend/src/TripPlanner.Api/appsettings.Development.json` (gitignored):

```json
{
  "MongoDb": {
    "ConnectionString": "mongodb+srv://<user>:<password>@<cluster>.mongodb.net/?retryWrites=true&w=majority",
    "DatabaseName": "ai_trip_planner"
  }
}
```

For production use the environment variables `MongoDb__ConnectionString` and
`MongoDb__DatabaseName` instead. Never commit real credentials.

Run the API:

```bash
cd backend
dotnet run --project src/TripPlanner.Api
# API: http://localhost:5047  Swagger: /swagger  Health: /api/health
```

Verify the API connects to the database:

```bash
curl http://localhost:5047/api/health
# -> {"status":"healthy","database":"connected","provider":"mongodb"}
```

## 3. Frontend

```bash
cd frontend
npm install
npm start        # http://localhost:4200  (proxies /api to http://localhost:5047)
```

Use a `.env`/environment technique for API URLs in later phases; dev proxying is already
configured in `frontend/proxy.conf.json`.

## 4. Authentication

Accounts are required: `/chat` and `POST /api/chat` are protected. Unauthenticated visitors
are redirected to `/login`, and the API answers `401` without a valid token.

Endpoints:

| Method | Route | Auth | Purpose |
| --- | --- | --- | --- |
| `POST` | `/api/auth/register` | anonymous | Create an account, returns a JWT |
| `POST` | `/api/auth/login` | anonymous | Sign in with email/username + password |
| `GET` | `/api/auth/me` | bearer | Current user profile |
| `POST` | `/api/chat` | bearer | Talk to the n8n agent |

Configure the signing key in `appsettings.Development.json` (gitignored) — it must be at
least 32 characters:

```json
{
  "Jwt": {
    "Issuer": "TripPlanner.Api",
    "Audience": "TripPlanner.Client",
    "Key": "<random string of 32+ characters>",
    "ExpiryMinutes": 1440
  }
}
```

For production use `Jwt__Key` (or a secret store). Passwords are hashed with PBKDF2-SHA256
(100,000 iterations, per-user salt); tokens are standard HS256 JWTs. New users get the
`User` role automatically.

## 5. AI assistant (n8n)

The Angular chat page calls our API (`POST /api/chat`), and the API calls your n8n agent.
Configure the webhook in `backend/src/TripPlanner.Api/appsettings.Development.json`:

```json
{
  "N8n": {
    "WebhookUrl": "https://<your-n8n-host>/webhook/<workflow-id>",
    "ApiKeyHeader": "X-N8N-Api-Key",
    "ApiKey": "",
    "TimeoutSeconds": 60
  }
}
```

- Use the **Production URL** of the workflow's Chat Trigger / Webhook node.
- If the workflow uses header authentication, set `ApiKey` (sent with `ApiKeyHeader`).
- The API sends `{ "action": "sendMessage", "sessionId": "...", "chatInput": "...", "message": "..." }`
  and extracts the reply from `reply`, `output`, `text`, `answer`, `response`, `message` or `content`.
- The chat UI lives at `/chat` (nav link: **AI Assistant**).

## Current status

- [x] .NET 8 solution with layered architecture (Api / Application / Domain / Infrastructure)
- [x] Angular 20 app with feature-based folder structure
- [x] MongoDB Atlas connection via `MongoDB.Driver`
- [x] Initial entities: `User`, `Role`, `UserRole`, `Trip` (+ `TripStatus` enum)
- [x] Index creation + `User` / `Admin` role seeding on startup
- [x] Health endpoint verifying DB connectivity
- [x] AI chat UI (`/chat`) + `POST /api/chat` proxy to n8n agent
- [x] Authentication & JWT: register / login / me, guarded `/chat`, PBKDF2 password hashing (Phase 2)
- [ ] Trip CRUD + dashboard           (Phase 3)
- [ ] Itinerary model + UI            (Phase 4)
- [ ] n8n integration / AI planning   (Phase 5)
- [ ] AI chatbot + chat history       (Phase 6)
- [ ] Admin dashboard                 (Phase 7)
- [ ] Hardening, error handling, tests (Phase 8)

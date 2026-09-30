# Helpdesk Ticketing System

Full-stack helpdesk: ticket intake and workflow, knowledge base, AI triage,
SLA tracking, CSAT/NPS, canned replies, real-time updates, and role-based access.

## Live demo

- App: `https://helpdeskapp.netlify.app` (placeholder, fill in after first deploy)
- API: `https://helpdesk-api.onrender.com` (placeholder, fill in after first deploy)

Verify on the live URL before announcing it: register a user, create a
ticket, assign it as an agent, then confirm the ticket appears in the queue
and an overdue ticket surfaces in the SLA breached list.

## Stack

- Backend: ASP.NET Core 10, SignalR, EF Core (SQL Server when configured, in-memory demo otherwise), Dapper-ready data layer
- Frontend: React 18 + TypeScript, react-router, SignalR client, Testing Library
- Auth: 15-minute HMAC JWT with issuer/audience checks plus 7-day rotating refresh tokens, PBKDF2 hashing
- Tests and CI: xUnit backend suite, React Testing Library suite, GitHub Actions

## Repo layout

- `backend/HelpdeskAPI/` — the API. `Controllers/`, `Data/` (repository + `HelpdeskDbContext`), `Services/` (workflow, SLA, AI triage, sanitizer, tokens, notifications), `Hubs/TicketHub`, `DTOs/`, `Middleware/`
- `backend/HelpdeskAPI.Tests/` — xUnit suite for workflow, SLA, auth-adjacent rules
- `frontend/src/` — `api/`, `types.ts`, `hooks/`, `components/`, `pages/`, thin `App.tsx` router

Deleted: the `HelpdeskApp.Server` WeatherForecast scaffold and the `helpdeskapp.client` Vite template. The solution (`HelpdeskApp.slnx`) now references the real projects only.

## Run locally

Backend (needs .NET 10 SDK):

```bash
dotnet restore backend/HelpdeskAPI/HelpdeskAPI.csproj
dotnet run --project backend/HelpdeskAPI
```

API listens on `https://localhost:7049` (see `Properties/launchSettings.json`).
Swagger in Development at `/swagger`. SignalR hub at `/hubs/tickets`.

Optional persistence: set `ConnectionStrings:Helpdesk` to a SQL Server string and
run `dotnet ef migrations add Initial --project backend/HelpdeskAPI`. Without it,
the app runs on the seeded in-memory store. Production also requires
`Auth:JwtSecret` (32+ chars) or `HELPDESK_JWT`; boot fails fast otherwise.

Frontend (needs Node 18+):

```bash
cd frontend
npm install
npm start
```

Point at the API with `REACT_APP_API_BASE` (defaults to `https://localhost:7049`).

Demo logins: `admin / Adminpass123`, `agent1 / Agentpass123`, `john.doe / Userpass123`.

## Features

- Tickets numbered `TKT-YYYYMMDD-XXXX`; workflow New to Open to InProgress to Pending to Resolved to Closed, enforced in `WorkflowService`
- Public replies plus agent-only internal notes with full conversation history; reopen within 7 days; 14-day idle auto-close worker
- Search across number, subject, and text; filters by status, priority, category, assignee, date; saved views including My Open Tickets
- File attachments up to 10 MB with type allowlist and permission-checked download
  (`POST /api/tickets/{id}/attachments`, `GET /api/tickets/attachments/{id}/download`,
  `TicketRepository` attachment store, upload UI in `pages/TicketDetail.tsx`;
  demo-scale local storage, virus scanning noted as a follow-up)
- Knowledge base with categories, versioning, view counts, helpful votes
- AI triage: category and priority guess with confidence, daily per-user cap, prompt-injection refusal, KB suggestions before submit (`POST /api/ai/suggest-for-ticket`)
- SLA policies per priority; 5-minute sweeper flags breaches, warns under 30 minutes, escalates to team lead; breached list and per-ticket status endpoint
- CSAT (1-5) and NPS (0-10) surveys with aggregate reporting plus per-category volume
- Agent workload view: unassigned queue, per-agent load, round-robin auto-assign; admin audit log; CSV export (Admin)
- Real-time: ticket create, status, reply, and assign events to ticket and agent groups; client reconnects with backoff and polling fallback
- Security: HSTS, CSRF tokens, `__Host-` secure cookies, locked CORS, tiered rate limits (auth, password reset, ticket creation, API), CSP headers, global exception envelope, server-derived identity via `RequestUser`, sanitization before storage

## Tests and CI

```bash
dotnet test backend/HelpdeskAPI.Tests/HelpdeskAPI.Tests.csproj
cd frontend && npm test -- --watchAll=false
```

CI (`.github/workflows/ci.yml`) builds the API, runs the xUnit suite
(`WorkflowTests`, `SlaTests`, `SecurityRuleTests`), then installs the
frontend and runs both test files (`App.test.tsx`, `components/ui.test.tsx`)
before building it.

## Deploy

Deployed surface is Docker plus static hosting. No Azure resources exist, so
nothing here claims Azure.

- API: multi-stage `backend/HelpdeskAPI/Dockerfile` (SDK build to ASP.NET runtime).
  `render.yaml` runs it as a Docker web service with a health check on
  `/api/sla/policies`. Set `HELPDESK_JWT` (32+ chars, required),
  `ConnectionStrings__Helpdesk` (omit for the in-memory demo), and
  `Cors__AllowedOrigins__0` (the frontend URL) in the host dashboard.
- Frontend: `netlify.toml` builds `frontend/` and ships `build/` with an SPA
  fallback so `/tickets/42` deep links resolve. `render.yaml` also defines a
  static site alternative. Set `REACT_APP_API_BASE` to the API URL.

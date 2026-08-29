# Online Assessment Platform — API

ASP.NET Core 8 Web API for a role-based quiz platform with an AI question-authoring and study-help assistant.

## What it does

**Instructors**
- Create quizzes (subject, time limit, description)
- Add questions by hand, **or draft them with the AI helper** from a topic + difficulty, review, then save
- Publish / unpublish
- See results: average score, per-question accuracy, per-topic breakdown, every attempt

**Students**
- Browse published quizzes, take timed attempts, get auto-graded on submit
- Review any past attempt answer-by-answer (attempts are kept permanently)
- On any question, ask the AI for a **step-by-step solution**, the **underlying theory**, or a **study tip**
- Get an AI **study plan** for a finished attempt, based on which topics were weak

## Tech

| Area | Choice |
|------|--------|
| Framework | ASP.NET Core 8 Web API (controllers, feature folders) |
| Data | Entity Framework Core 8 + PostgreSQL (Npgsql) |
| Auth | JWT bearer, role-based (`Student` / `Instructor`), BCrypt password hashing |
| AI | Google Gemini via a swappable `IAiService` (structured JSON output, retries, response caching) |
| Docs | Swagger UI at `/swagger` |

The AI layer is behind one interface (`Ai/IAiService.cs`). `GeminiAiService` is the real
implementation; `StubAiService` returns canned content so the whole app runs with no API key.
Switching providers is one line in `Program.cs`.

## Database — Neon (Lakebase Postgres)

This project is linked to a Neon project via the Neon CLI. The link lives in `backend/.neon`
(git-ignored: org / project / branch IDs), and `neon link` / `neon checkout` pull the branch's
credentials into `backend/.env.local` (also git-ignored) as:

| Variable | Endpoint | Used for |
|----------|----------|----------|
| `DATABASE_URL` | pooled (`-pooler` host) | normal app query traffic |
| `DATABASE_URL_UNPOOLED` | direct | EF Core migrations + the startup schema check |

`Program.cs` loads `.env.local` on boot (see `Common/DotEnv.cs`), so no `dotnet user-secrets`
setup is required. The app auto-migrates and seeds on first run.

### Run it locally

```bash
cd backend
# one-time: link the Neon project (writes .neon, pulls .env.local)
neon link --project-id <project-id> -y
# then fill in the app secrets in backend/.env.local (see .env.example): JWT__KEY, AI__APIKEY

dotnet run --project AssessmentPlatform.Api
```

- API + Swagger: <http://localhost:5109/swagger>
- Seeded demo accounts:

| Role | Email | Password |
|------|-------|----------|
| Instructor | `instructor@demo.com` | `Password123!` |
| Student | `student@demo.com` | `Password123!` |

To switch Neon branches (e.g. a throwaway branch for a risky migration):
`neon checkout dev-my-feature` — it creates the branch and refreshes `.env.local`.

## Configuration reference

| Key | Env var | Notes |
|-----|---------|-------|
| `ConnectionStrings:Default` | `DATABASE_URL` | Pooled Postgres URL for app traffic. Accepts a `postgres://…` URL or a key=value string |
| — | `DATABASE_URL_UNPOOLED` | Direct Postgres URL; used only for migrations |
| `Jwt:Key` | `Jwt__Key` | 32+ chars. **Required** in Production |
| `Ai:Provider` | `Ai__Provider` | `Gemini` or `Stub` (offline, no key) |
| `Ai:ApiKey` | `Ai__ApiKey` | Gemini API key from aistudio.google.com |
| `Ai:Model` | `Ai__Model` | Default `gemini-3.6-flash` |
| `Cors:Origins:0` | `Cors__Origins__0` | Frontend origin(s), e.g. `https://your-app.pages.dev` |

## Migrations

`Data/DesignTimeDbContextFactory.cs` points `dotnet ef` at `DATABASE_URL_UNPOOLED` (from
`.env.local`), so migrations always run over the direct, non-pooled endpoint.

```bash
cd backend
dotnet ef migrations add <Name> -o Data/Migrations --project AssessmentPlatform.Api
dotnet ef database update --project AssessmentPlatform.Api
```

The app also calls `Database.MigrateAsync()` on startup, so a deploy applies pending migrations
automatically.

## Docker

```bash
docker build -t assessment-api ./backend
docker run -p 8080:8080 \
  -e DATABASE_URL="postgres://…-pooler…/neondb?sslmode=require" \
  -e DATABASE_URL_UNPOOLED="postgres://…/neondb?sslmode=require" \
  -e Jwt__Key="a-32+-char-secret" \
  -e Ai__ApiKey="your-gemini-key" \
  -e Cors__Origins__0="https://your-frontend.pages.dev" \
  assessment-api
```

The `Dockerfile` is a two-stage build (SDK to compile, ASP.NET runtime to run) and binds to
`$PORT` when the host sets one (Render, Railway, Fly). You don't need Docker installed locally —
Render builds the image from this `Dockerfile` on its own servers.

## Project layout

```
AssessmentPlatform.Api/
  Domain/           entities + enums
  Data/             AppDbContext, migrations, seeder
  Common/           JWT current-user, error handling, postgres URL parsing
  Ai/               IAiService, GeminiAiService, StubAiService
  Features/
    Auth/           register / login
    Quizzes/        instructor CRUD + AI generation + results
    Attempts/       take quiz, grade, history, AI help + study guidance
```

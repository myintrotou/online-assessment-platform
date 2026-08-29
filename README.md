# Online Assessment Platform

A role-based quiz platform where instructors build assessments — drafting questions with an AI
assistant — and students take timed, auto-graded attempts and use the same AI to understand
what they missed.

| Part | Stack | Folder |
|------|-------|--------|
| API | ASP.NET Core 8, EF Core, PostgreSQL (Neon), JWT, Google Gemini | [`backend/`](backend) |
| Web | Angular 19, signals, Chart.js | [`frontend/`](frontend) |

## Features

**Instructors** — create quizzes; add questions by hand **or generate them from a topic with
AI** (review and adjust before saving); publish/unpublish; see average score, accuracy by
question and by topic, and every attempt.

**Students** — take timed quizzes (auto-submit on timeout), get graded instantly, and revise
any past attempt: correct vs. your answer, explanations, plus on demand an **AI step-by-step
solution / theory explainer / study tip per question** and an **AI study plan** for the whole
attempt based on which topics were weak. Attempts are kept permanently.

**AI** is behind one swappable interface (`backend/AssessmentPlatform.Api/Ai/IAiService.cs`) —
Gemini with structured JSON output, retries, and response caching; a `Stub` provider runs the
whole app with no key.

## Run locally

```bash
# 1. API  (needs a Neon DB + Gemini key — see backend/README.md)
cd backend && dotnet run --project AssessmentPlatform.Api      # http://localhost:5109

# 2. Web
cd frontend && npm install && npm start                        # http://localhost:4200
```

Demo logins: `instructor@demo.com` / `student@demo.com`, password `Password123!`.

## Deploy (all free tier)

- **Database** — Neon (already provisioned)
- **API** — Render, from `backend/Dockerfile`
- **Web** — Cloudflare Pages, build `npm run build`, output `dist/frontend/browser`

See each folder's `README.md` for the details.

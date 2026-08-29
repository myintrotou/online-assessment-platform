# Online Assessment Platform — Web

Angular 19 SPA for the [Assessment Platform API](../backend). Standalone components, signals,
lazy-loaded routes, JWT auth with a route-guarded shell.

## Screens

| Route | Who | What |
|-------|-----|------|
| `/login`, `/register` | anyone | JWT auth; demo logins pre-filled |
| `/` | any signed-in | role-aware landing |
| `/quizzes` | student | browse published quizzes |
| `/quizzes/:id/take` | student | timed attempt with a live countdown; auto-submits on timeout |
| `/attempts/:id` | student | graded review — correct/your answer, explanations, **AI step-by-step / theory / study-tip per question**, and an **AI study plan** for the whole attempt |
| `/history` | student | every past attempt, re-openable |
| `/teach` | instructor | my quizzes |
| `/teach/new`, `/teach/:id` | instructor | create a quiz; add questions manually **or draft them with AI** (review + edit the correct answer before saving); publish / unpublish |
| `/teach/:id/results` | instructor | average score, accuracy-by-question and accuracy-by-topic bar charts, per-attempt table |

## Stack

- **Angular 19** standalone components, `@if`/`@for` control flow, signals, `withComponentInputBinding()`
- **Auth** — `AuthService` (signal-backed), functional `authInterceptor` (attaches `Bearer`, redirects on 401), `authGuard` / `roleGuard(role)`
- **HTTP** — one typed `ApiService` over the whole API
- **Charts** — Chart.js (lazy — only in the results chunk)
- **Markdown** — `marked`, rendered through Angular's sanitizer via `[innerHTML]`
- No component library — hand-rolled design system in `src/styles.scss` (Figtree, one indigo accent)

## Run it

```bash
npm install
npm start          # ng serve on http://localhost:4200
```

The API must be running on `http://localhost:5109` (see [../backend](../backend)). The dev
API URL lives in `src/environments/environment.ts`.

Demo logins: `instructor@demo.com` / `student@demo.com`, password `Password123!`.

## Build

```bash
npm run build      # production build -> dist/frontend/browser
```

Production uses `src/environments/environment.production.ts` (swapped in via `fileReplacements`
in `angular.json`). **Set `apiBaseUrl` there to your deployed API origin before building**, or
the built app will call a placeholder host.

Initial bundle ≈ 82 kB gzipped; the Chart.js-heavy results page is a separate lazy chunk.

## Deploy (Cloudflare Pages / Netlify / any static host)

- Build command: `npm run build`
- Output directory: `dist/frontend/browser`
- SPA fallback: redirect all paths to `/index.html` (Cloudflare Pages: add a `_redirects` file
  with `/*  /index.html  200`)
- Set the API origin in `environment.production.ts` and make sure that origin is in the API's
  `Cors__Origins__*` list.

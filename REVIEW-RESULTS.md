# Pathly — Review Response & Verification

Date: 2026-09-29
Scope: backend `PathlyAI_API` (.NET 10 API) and frontend `pathlyAPI` (Angular 21).

## Summary

A significant part of the reviewer's feedback was already addressed by two remediation commits
present in both repos:

- Backend: `3d7c5bd Address review feedback: per-user persistence, POPIA, auth hardening, validation`
- Frontend: `7000fbf Address review feedback: per-user data scoping, persistence, auth and POPIA pages`

The reviewer's symptoms (results leaking across users on a shared device, no persistence, `napping`
treated as a subject) match the **pre-fix** behaviour, which strongly suggests the **live deployment
was stale**. The first action when shipping this work is to confirm the deployed frontend and API
build from the current `master`.

On top of the existing fixes, the genuine remaining gaps were closed: a deterministic career-path
coherence guard, the quiz now feeding the standard report, durable assessment reports, cross-upload
progression, bot protection, refresh-token rotation, upload content validation, dead-code removal,
a broken Dockerfile path, and much more test coverage.

## Feedback → status

| # | Feedback | Before | Now |
|---|----------|--------|-----|
| 1 | Previous user's results visible after re-login on a shared device | Per-account storage namespacing + full purge already in code (likely not deployed) | Kept; added regression tests (`src/app/services/app-storage.spec.ts`). Report cache is per-account; logout revokes the refresh token. |
| 2 | No persistence / lost on cache clear / not on another device | Server history endpoints already existed | Kept; the dashboard loads the latest stored report and history. Reopened reports now also retain prediction/progression (previously dropped because they were set after serialization). |
| 3 | Weak upload validation (`napping`, duplicate subjects) | Deterministic sanitizer/validator already existed | Kept + tested; added frontend type check and server-side **magic-byte** content validation so a renamed/disguised file is rejected. |
| 4 | Incoherent career path (UCT robotics + UP mech-eng) | Prompt-only (Rule 15) | **New deterministic `CareerPathCoherenceValidator`** runs after the LLM in both analysis paths: it aligns roadmap steps to the recommended institution, ensures the institution is listed, and flags the report when it cannot reconcile. Tests reproduce the reviewer's scenario. |
| 5 | Quiz does not influence dashboard results | Free `/analysis` ignored the quiz; combined was Pro-only | **Auto-fold**: the standard analysis now loads the learner's stored RIASEC profile (when present) into the evidence engine, prompt and cache key. |
| 6 | Vague assessment conclusions | Detailed report computed client-side only | Detailed report is now **persisted server-side** (`PsychometricAssessment.ReportJson`, new migration) so it is durable and available on any device. |
| 7 | No history; new upload drops previous data | History list + APS trend existed | **New `ProgressionService`** merges every upload into a per-subject term/year series, feeds the prompt, and is shown as "Progress Over Time" on the dashboard. |
| 8 | No forgot-password; no bot protection | Forgot-password existed; only rate limiting | **Cloudflare Turnstile** on registration (disabled-safe) + **disposable-email blocking** + **rotating refresh tokens** with an httpOnly cookie and server-side revocation. |
| 9 | POPIA / terms / privacy | Pages + consent + export/delete existed | Added cookies/local-storage disclosure and an Information Officer + Information Regulator contact. |
| 10 | Unfinished/empty classes | Several dead files remained | Removed dead interfaces/repositories (`IAcademicRecordRepository`, misnamed `IAcademicRecordRepositoryInterface`, `IAcedemicServiceInterface`, self-referential `AcademicRecord`), fixed the **Dockerfile project path** (`PathlyInterfaces` → `Pathly_Interfaces`), removed the unused `Steeltoe.Common` package and the weatherforecast stub. |

## Tests & builds

- Backend: `dotnet build PathlyAI_API.slnx` → 0 warnings/errors; `dotnet test` → **106 passing** (was 88).
- Frontend: `npm run build` succeeds; `npx ng test --watch=false` → **19 passing** (was 16).
- New CI workflows added (`Backend CI`, `Frontend CI`) build + test both repos on every push/PR to `master`.

## New configuration

- `Auth:{AccessTokenMinutes, RefreshTokenDays, UseRefreshTokenCookie}`
- `Turnstile:{SiteKey, SecretKey, Enabled}` — set `Enabled=true` and real keys (secret via
  `Turnstile__SecretKey`) in production. Disabled = verification skipped.
- `Smtp:*` still needs real values for email delivery; `Auth:RequireConfirmedEmail` can then be
  switched on once SMTP is live.

## Manual verification checklist

1. **Shared device**: sign in as A → generate a report → sign out → sign in as B → B sees no A data;
   A's local caches are gone.
2. **Persistence**: generate on device A → open on device B (or after clearing cache) → report is
   present via Analysis History.
3. **Validation**: upload the reviewer's "napping" document → the row is dropped and a warning shown;
   upload Maths Term 1 + Term 2 → both terms kept but only the driver term feeds APS.
4. **Coherence**: run the reviewer's matric example → the roadmap names the same institution as the
   top career.
5. **Quiz → dashboard**: complete the assessment, then upload results → the report reflects the
   RIASEC profile.
6. **Forgot password / bots**: reset flow works; registration without a valid Turnstile token is
   rejected when `Turnstile:Enabled=true`; disposable emails are blocked.
7. **POPIA**: register captures consent; export downloads all data; delete erases reports +
   assessment.
8. **Deploy**: confirm the live frontend and API run the current `master` and apply the new EF
   migrations.

## Residual / future work

- Enforcing email confirmation (`Auth:RequireConfirmedEmail=true`) once SMTP is configured.
- A real payment gateway is still simulated on the frontend (Paystack is wired server-side).
- Legacy EF entities (`AcademicRecords`, `SubjectResults`) remain for migration history; they can be
  dropped in a dedicated cleanup migration.

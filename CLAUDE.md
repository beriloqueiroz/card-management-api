# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Technical challenge (prova técnica) for a **.NET Pleno developer position** — Berilo is a candidate. The deliverable is a REST API for credit card management, to be built from scratch in this directory. The tech lead's framing: "we want to understand how you would implement this scenario", so **documented technical decisions matter as much as working code**.

Source-of-truth files (read before implementing anything):
- `prova_tecnica_dev_dotnet_api.pdf` — the official challenge spec (pt-BR). Requirements below are summarized from it; the PDF wins on any conflict.
- `prova_cartoes_seed_postgresql.sql` — provided seed: `users` and `credit_cards` tables + 3 users (Mariana Alves has 12 cards specifically to exercise pagination). The spec allows evolving this model via migration if documented in the README.
- `requisitos.md` — Berilo's own planning notes: open decisions and the intended architecture. Keep it updated as decisions are made.
- `todo.md` — current task notes.

## Challenge requirements (summary)

- CRUD of `/api/cards` (GET list, GET by id, POST, PUT, PATCH, DELETE) scoped strictly to the authenticated user — cross-user access must be impossible on every operation.
- Auth strategy is the candidate's choice but tokens must expire in **30 minutes** with a documented rotation/renewal flow that replaces the old token.
- Listing: fixed pages of **10 items**, ordered by `created_at` DESC, with pagination metadata (`page`, `pageSize`, `totalItems`, `totalPages`, `items`), and an expiration-date period filter applied **before materialization** (i.e., in the SQL query, not in memory).
- Card number and PIN are highly sensitive: never in responses, logs, error messages, or listings. Responses show the number masked as `5321 **** **** 5336` (seed stores only `first_four_digits`/`last_four_digits`).
- PIN must be retrievable in **original form** via a dedicated, extra-protected endpoint, but must not be stored in plain text → implies reversible encryption (not hashing); requires evolving the seed model (e.g., an encrypted PIN column), documented in the README.
- DELETE must hide the card from normal queries while preserving domain traceability → soft delete.
- `status` ∈ {ACTIVE, BLOCKED, CANCELLED}; `credit_limit >= 0`; standardized errors with coherent HTTP status codes.
- Deliverables: source code, README (run instructions + technical decisions + trade-offs), SQL/migrations used.

## Commands

- `docker compose up --build` — full stack: API (:8000), app Postgres, ZITADEL (:8080, alias `auth.localhost`) + one-shot bootstrap that provisions IdP project/app/users.
- `dotnet build` / `dotnet run --project src/Cards.Api` — build/run locally (needs `127.0.0.1 auth.localhost` in /etc/hosts and the infra services up).
- `dotnet test` — all 68 tests; `tests/Cards.UnitTests` is pure in-memory, `tests/Cards.IntegrationTests` needs Docker (one shared Testcontainers Postgres, one database per test class via `PostgresContainerFixture`). Single test: `dotnet test --filter FullyQualifiedName~TestName`.
- `DOTNET_ROOT=/root/.dotnet dotnet ef migrations add <Name> -p src/Cards.Infrastructure` — migrations live in `src/Cards.Infrastructure/Migrations` (design-time factory included, no DB needed).

## Architecture (implemented)

- Layers: `Cards.Domain` (rich entities `CreditCard`/`User`, value objects `CardNumber`/`Pin` — full PAN never stored, only first4/last4) → `Cards.Application` (framework-free: `CardsService`, DTOs, ports `ICreditCardRepository`/`IPinCipher`/`ICardAuditLogger`) → `Cards.Infrastructure` (EF Core/Npgsql, AES-256-GCM PIN cipher, idempotent seeder with fixed GUIDs) and `Cards.Api` (thin controllers, JWT bearer, RFC 7807 via `GlobalExceptionHandler`, Swagger OAuth PKCE).
- Auth: ZITADEL issues 30-min JWTs; rotation = OIDC refresh grant. User resolved by `email` claim (fallback: userinfo endpoint) against seeded `users` table. Cross-user access is always 404.
- Soft delete via `deleted_at` + EF global query filter. Seed cards all have PIN `1234` (encrypted); seed user password is `Cards@2026!` (bootstrap).
- The provided SQL seed was converted to the EF initial migration + `DatabaseSeeder`; keep them in sync if the model evolves.

## Conventions

- README and user-facing docs in **pt-BR**; code identifiers and comments in English (workspace-wide rule).
- Never log or echo card numbers or PINs — including in exception messages, EF logging, and test output.
- Don't `git init`, commit, or push without an explicit ask.

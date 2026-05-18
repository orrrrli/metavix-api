# Architecture

This document captures the architectural decisions for **Metavix**. It is a living document — update it whenever a layer, package, or infrastructure choice changes.

---

## 1. Architectural Style

Metavix follows **Clean Architecture** with **CQRS** (Command Query Responsibility Segregation) via MediatR.

Dependencies always point **inward**: outer layers know about inner layers, never the other way around. This isolates business logic from frameworks, databases, and delivery mechanisms.

```
API ──► Infrastructure ──► Application ──► Domain
                                │
                                ▼
                            Contracts
```

---

## 2. Layer Organization

### 2.1 Domain (`src/Domain`)

The core of the system. Contains entities, value objects, enums, and domain errors.

- **Knows about:** nothing.
- **Direct references:** none.
- **Rule:** must remain free of any framework, ORM, or infrastructure concern. If Domain ever needs to import from another layer, the design is wrong.

### 2.2 Contracts (`src/Contracts`)

Data Transfer Objects (DTOs) used at the API boundary — request and response shapes that travel "over the wire".

- **Knows about:** Domain (optional, only if it reuses enums).
- **Direct references:** none today. Will reference `Domain` when DTOs start using domain enums such as `DiabetesType` or `RecordSource`.
- **Rule:** contains no behavior. Pure data shapes plus JSON converters.

### 2.3 Application (`src/Application`)

Use cases. Commands, queries, handlers, validators, and **interfaces** for everything Infrastructure must implement (repositories, security services, external integrations).

- **Knows about:** Domain, Contracts.
- **Direct references:** `Domain`, `Contracts`.
- **Rule:** defines *what* the system does, never *how*. No EF Core, no HTTP clients, no I/O.

### 2.4 Infrastructure (`src/Infrastructure`)

Concrete implementations of the interfaces declared in Application: EF Core `DbContext`, repositories, JWT providers, email senders, third-party API clients.

- **Knows about:** Application (and Domain, Contracts via transitive references).
- **Direct references:** `Application`.
- **Transitive references:** `Domain`, `Contracts`.
- **Rule:** this is the only place ORM and external SDKs are allowed.

### 2.5 API (`src/API`)

The composition root and delivery mechanism. ASP.NET Core minimal API / controllers, middleware, dependency injection wiring, configuration.

- **Knows about:** Infrastructure, Contracts.
- **Direct references:** `Infrastructure`, `Contracts`.
- **Transitive references:** `Application`, `Domain` (via Infrastructure).
- **Rule:** thin. Delegates work to MediatR handlers and returns mapped responses.

---

## 3. Project Reference Matrix

| Project        | Direct References          |
|----------------|----------------------------|
| Domain         | —                          |
| Contracts      | — *(Domain when needed)*   |
| Application    | Domain, Contracts          |
| Infrastructure | Application                |
| API            | Infrastructure, Contracts  |

**Direct vs transitive:** a direct reference is one declared in the `.csproj`. A transitive reference is inherited through the chain. If a layer uses a type *directly*, prefer a direct reference for clarity, even when it would also come transitively.

---

## 4. NuGet Packages

### 4.1 Application

| Package          | Purpose                                                         |
|------------------|-----------------------------------------------------------------|
| *(none yet)*     | Will add MediatR, FluentValidation, and ErrorOr as needed.      |

### 4.2 Infrastructure

| Package                                  | Version | Purpose                                                                                                |
|------------------------------------------|---------|--------------------------------------------------------------------------------------------------------|
| `Microsoft.EntityFrameworkCore`          | 9.0.0   | EF Core runtime — `DbContext`, change tracking, LINQ translation.                                      |
| `Microsoft.EntityFrameworkCore.Design`   | 9.0.0   | Design-time tooling required by `dotnet ef` to generate migrations. Marked `PrivateAssets=all` so it is not propagated to consumers. |
| `Npgsql.EntityFrameworkCore.PostgreSQL`  | 9.0.4   | PostgreSQL provider for EF Core. Translates LINQ into Postgres SQL.                                    |

### 4.3 API

| Package                          | Version | Purpose                                                                                  |
|----------------------------------|---------|------------------------------------------------------------------------------------------|
| `Microsoft.AspNetCore.OpenApi`   | 9.0.14  | OpenAPI document generation for the HTTP surface.                                        |
| `ErrorOr`                        | 2.1.1   | Result type used to model success/failure without exceptions in business logic.          |
| `Serilog.AspNetCore`             | 10.0.0  | Structured logging. Replaces the default Microsoft logger with Serilog.                  |

> `Microsoft.EntityFrameworkCore.Design` lives only in Infrastructure. The API project does not need it — migrations are generated by pointing `dotnet ef` at Infrastructure as the migrations project.

---

## 5. Persistence

- **ORM:** Entity Framework Core 9 (code-first).
- **Database engine:** PostgreSQL.
- **Migrations location:** `src/Infrastructure/Common/Persistence/Migrations`.
- **Migrations assembly:** `Infrastructure` — configured via `npgsql.MigrationsAssembly(...)` in `Program.cs`.

### Hosting strategy

| Phase            | Host                                  | Why                                                                                   |
|------------------|---------------------------------------|---------------------------------------------------------------------------------------|
| Alpha (current)  | **Neon** (serverless Postgres)        | Zero infrastructure setup, free tier covers alpha needs, database branching available. |
| Production       | **Self-hosted Postgres on VPS**       | API and database will live on the same VPS for lower latency and simpler ops.          |

Connection strings are stored in **.NET user-secrets** during development, never in `appsettings.*.json` checked into the repo.

---

## 6. Migrations Workflow

```bash
# Generate a new migration
dotnet ef migrations add <Name> \
  --project src/Infrastructure \
  --startup-project src/API \
  --output-dir Common/Persistence/Migrations

# Apply pending migrations to the configured database
dotnet ef database update \
  --project src/Infrastructure \
  --startup-project src/API
```

- `--project` points at the project that **owns** the migrations (Infrastructure).
- `--startup-project` points at the project that provides DI and configuration (API).

---

## 7. Conventions

- **Language:** all code, comments, and documentation are written in English.
- **Errors:** business errors use `ErrorOr<T>`. Exceptions are reserved for truly exceptional conditions.
- **Validation:** FluentValidation. Validators live alongside the command or query they validate.
- **Naming:** `PascalCase` for types and methods, `_camelCase` for private fields, `I` prefix for interfaces.

---

## 8. Open Items

Tracked separately. Items currently in flight:

- VPS provisioning and OS choice (leaning Ubuntu 24.04).
- Docker Compose setup for local Postgres (deferred — Neon covers alpha).
- MediatR, FluentValidation, and ErrorOr wiring in Application and API.
- Authentication strategy (JWT vs session) — not yet decided.

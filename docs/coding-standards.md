# Coding Standards

## Language

All code, comments, and documentation are written in **English**.

## Naming Conventions

- Types and methods: `PascalCase`
- Private fields: `_camelCase`
- Interfaces: `I` prefix
- Features: grouped by domain (`Auth/`, `Doctor/`, `Patient/`, `DailyRecord/`, etc.)

## Clean Architecture & CQRS

- Dependencies point **inward** only: `API → Infrastructure → Application → Domain`.
- The `Domain` project must remain free of any framework, ORM, or infrastructure concern.
- The `Application` project defines *what* the system does via commands, queries, handlers, validators, and interfaces.
- The `Infrastructure` project is the only place for EF Core, HTTP clients, external SDKs, and concrete implementations.
- The `API` project is a thin composition root that delegates work to MediatR handlers.

## CQRS Structure

For each feature, separate components into their respective subdirectories:

```
Features/<FeatureName>/
├── Commands/
├── Handlers/
├── Queries/
├── Validators/
└── Common/
```

Avoid monolithic files containing multiple CQRS components.

## Error Handling

- Business errors use `ErrorOr<T>`.
- Exceptions are reserved for truly exceptional conditions.
- Domain errors live in `Application/Common/Errors/` as static classes (e.g., `DoctorErrors.InvalidLicense`).

## Validation

- Use **FluentValidation**.
- Validators live alongside the command or query they validate.
- Validation runs automatically through a MediatR `IPipelineBehavior<TRequest, TResponse>`.

## Persistence

- Use EF Core 9 (code-first).
- Configure entities with `IEntityTypeConfiguration<T>` and Fluent API.
- Keep `DbContext` queries with `AsNoTracking()` by default for read paths.
- Migrations live in `src/Infrastructure/Migrations`.
- Always generate migrations with `--output-dir Migrations`:

```bash
dotnet ef migrations add <Name> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj \
  --output-dir Migrations
```

## Auth & Security

- JWT access tokens (15 min) and refresh tokens (7 days) are delivered via `HttpOnly; Secure; SameSite=None` cookies.
- Refresh token path is restricted to `/api/auth`.
- Google OAuth users cannot authenticate with a password.
- Login anti-enumeration: never distinguish "email not found" from "wrong password".
- Login lockout: 5 failed attempts trigger a 15-minute block.

## API Conventions

- All routes live under `/api/v1/`.
- Use Carter modules for Minimal API endpoint grouping.
- Return consistent error shapes via `ErrorOr<T>` mapping.
- Include correlation ID middleware on every request.

## Logging

- Use Serilog with structured logging to PostgreSQL.
- Every request carries a `correlation_id` returned in the `X-Correlation-ID` header.

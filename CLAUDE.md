# metavix-api — Claude Context

## Architecture

Clean Architecture + CQRS via MediatR. Read `ARCHITECTURE.md` for the full layer reference before making any structural changes.

## Critical: EF Core Migrations

**Always** use `--output-dir Migrations` when adding a migration:

```bash
dotnet ef migrations add <Name> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj \
  --output-dir Migrations
```

**Why:** `AppDbContext` lives in the `Infrastructure.Common.Persistence` namespace. Without `--output-dir`, EF derives the folder from that namespace and places migrations in `src/Infrastructure/Common/Persistence/Migrations/` instead of the correct `src/Infrastructure/Migrations/`, breaking the migration chain.

## Commands

```bash
# Run API
dotnet run --project src/API/API.csproj

# Add migration
dotnet ef migrations add <Name> \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj \
  --output-dir Migrations

# Apply migrations
dotnet ef database update \
  --project src/Infrastructure/Infrastructure.csproj \
  --startup-project src/API/API.csproj

# User secrets (run from src/API/)
dotnet user-secrets list
dotnet user-secrets set "<Key>" "<Value>"
```

## Layer Rules

- **Domain** — no framework references. Entities, enums, value objects only.
- **Application** — no `IConfiguration`, no EF Core, no HTTP. Define interfaces, commands, queries, handlers.
- **Infrastructure** — only place where EF Core, HttpClient, and external SDKs are allowed.
- **API** — thin composition root. No business logic.

## Patterns

- One command/query per file in `Commands/` or `Queries/`.
- Handler in a separate file in `Handlers/`.
- All handlers return `ErrorOr<T>`. Never throw exceptions for business logic.
- Domain errors defined as static classes in `Application/Common/Errors/`.
- New services: declare interface in `Application/Common/Interfaces/Services/`, implement in `Infrastructure/Services/`, register in `Infrastructure/DependencyInjection.cs`.
- New repositories: declare interface in `Application/Common/Interfaces/Persistence/`, implement in `Infrastructure/Persistence/`, auto-registered by the `AddRepositories()` convention in DI.

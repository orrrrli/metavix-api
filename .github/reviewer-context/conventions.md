# Conventions the reviewer must know

Distilled from internal engineering standards. Treat these as ground truth for this repo — don't
suggest patterns that contradict them.

## CQRS structure
Every use case lives under `src/Application/UseCases/<Feature>/` with this exact layout:
```
Commands/
Handlers/
Queries/
Validators/
Common/
Mappers/        # optional — only when result-mapping is shared across ≥2 handlers
```
No monolithic files mixing multiple CQRS components. Don't suggest extracting a mapper for a
one-off — mapper extraction only happens once a *second* handler needs the same shape.

## Domain Factory Pattern
Entities with clinical/business invariants are built exclusively via static `Create(...)` factories
returning `ErrorOr<T>`. The factory is the only place those invariants are enforced.

- Signature takes **primitives only** — never Application-layer records (inverts dependency direction).
- The factory owns ID generation (`Guid.NewGuid()`) and timestamps (`now` passed in by the caller).
- `new Entity { ... }` outside the factory is a code smell — flag it.

## Error handling
- All handlers return `ErrorOr<T>`. Never throw for business logic.
- Two error folders, split by ownership:
  - `src/Domain/Common/Errors/` — entity-internal invariants, triggered by Domain factories.
  - `src/Application/Common/Errors/` — auth/lookup/cross-aggregate rules, triggered by handlers/services.
- Quick test: "this entity cannot exist in this state" → Domain. "This command/caller cannot do this" → Application.

## Handler Authorship Pattern
A handler is a thin orchestrator: authorization → aggregate loading with ownership check → domain
mutation/read → persistence. It does NOT contain business rules, infrastructure work (no
`Guid.NewGuid()`, no `DateTime.UtcNow`, no manual JSON shaping), or inlined ownership checks.

- Ownership + existence must resolve in **one** repository call (`GetOwnedXxxAsync` /
  `GetByUserIdAsync`), not a separate lookup-then-compare. `null` from the repo → `Forbidden`,
  never `NotFound` — returning `NotFound` on a bad id is an enumeration oracle.
- See `handler-authorship-context.md` for the incident that motivated this.

## Namespace aliases
When a use-case folder shares a name with a Domain entity (e.g. `UseCases.DailyRecord` vs
`Domain.Models.DailyRecord`), the file aliases the Domain type:
```csharp
using DomainDailyRecord = Domain.Models.DailyRecord;
```
This is intentional, not an oversight — don't flag it as "confusing naming."

## API conventions
- All routes under `/api/v1/`.
- Carter modules, one per resource.
- MediatR pinned to 11.1.0 deliberately (last Apache-2.0 release before the license change in v13).
  Don't suggest bumping the MediatR version.

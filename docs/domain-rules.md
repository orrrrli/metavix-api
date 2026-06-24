# metavix — Local Context

> **Instruction for the AI:** This file defines the Domain, Architecture, and Business Rules exclusive to this project. Read it carefully before proposing architectural changes or adding new entities.

## 1. Business Overview
- **Purpose:** REST API for a diabetes management platform connecting patients with doctors. Patients track daily health data (vital signs, glucose readings, lab results, insulin dosing) while doctors monitor linked patients and evaluate clinical goals against ADA (American Diabetes Association) standards.
- **Core Users:**
  - **Patients** — record daily vitals, glucose, lab results, insulin logs; request doctor links; evaluate own clinical goals
  - **Doctors** — monitor linked patients' data; accept/reject link requests; evaluate patient clinical goals; verified against Mexico's SEP cédula profesional registry
  - **Admins** — view system logs, trace requests by correlation ID
- **Success Metrics:** Idempotent mutations, <200ms response times, zero data inconsistency in patient-doctor relationships, full request traceability via correlation IDs.

## 2. Domain Model (Ubiquitous Language)

### Core Identity
- **User** — Base auth entity. Has `Email`, `PasswordHash`, `Role` (Patient/Doctor/Admin). A User is EITHER a Patient OR a Doctor (discriminated by Role), never both.
- **Patient** — Person tracking their health. Has demographics (`FirstName`, `LastName`, `MedicalRecordNumber`, `DateOfBirth`, `Gender`, `IsPregnant`), clinical profile (`HeightCm`, `DiabetesType`), and a `PrimaryDoctor` (0..1, set via link request acceptance).
- **Doctor** — Medical professional. Uses Mexican naming convention (`FirstName`, `MiddleName?`, `PaternalLastName`, `MaternalLastName`). Has `LicenseNumber`, `Speciality`, `IsVerified` (set by cédula scraper). Government IDs: `Curp?`, `IneNumber?`.

### Clinical Data
- **DailyRecord** — A single day's vital signs: `SystolicPressure?`, `DiastolicPressure?`, `HeartRate?`, `WeightKg?`, `WaistCm?`, `RecordDate`. Contains multiple `GlucoseReading`s.
- **GlucoseReading** — A blood glucose measurement. Has `ReadingType` (Fasting, PostBreakfast, PreLunch, PostLunch, PreDinner, PostDinner, Snack, Overnight), `ValueMgDl` (20-600 range), `Time?`, `Foods?` (meal description).
- **LabResult** — Periodic lab work: HbA1c, lipid panel (TotalCholesterol, LDL, HDL, Triglycerides), kidney markers (Creatinine, BUN), urinalysis (EgoProteins, EgoGlucose).
- **InsulinDm1Profile** — 1:1 with Patient. Type 1 diabetes therapy parameters: `InsulinName`, `Ric` (insulin-to-carb ratio), `SensitivityFactor`, `TargetGlucose`, doctor contact info.
- **InsulinDm1Record** — Patient-reported insulin dose log: glucose before/after, total carbs, dose applied, meal description, subjective feeling.

### Relationships & Workflows
- **PatientDoctorRequest** — Manages the patient-doctor linking protocol. Status lifecycle: `Pending` → `Accepted` / `Rejected` / `Revoked` / `Unlinked`. A patient can only have ONE accepted link at a time.
- **Admission** — Hospital admission linking Patient to Doctor. Has `IdempotencyKey` for safe retries. `IsActive` computed from null `DischargedAt`.
- **ClinicalGoal** — A target threshold for a clinical parameter (e.g., HbA1c < 7.0). Set per-patient by a doctor (`DoctorId` FK, no navigation property).
- **GoalEvaluation** — A snapshot evaluation run. Triggered by `EvaluationTrigger` (Patient or Doctor). Contains `GoalEvaluationItem`s, each capturing the parameter, measured value, the goal threshold AT EVALUATION TIME (`GoalUsed` — immutable snapshot), and status.
- **GoalEvaluationItem** — Per-parameter result: `ParameterId`, `ValueUsed?`, `GoalUsed`, `Status` (InRange/AtRisk/OutOfRange/NoData).

### Auth & Infrastructure
- **RefreshToken** — JWT refresh token stored in DB. Raw token, `ExpiresAt`, `IsRevoked`. Rotated on every refresh.
- **PasswordResetToken** — Hashed token for password reset. `UsedAt?` enforces single-use.
- **ToolResult** — Generic container for clinical calculator outputs (e.g., cardiovascular risk score). Has `ToolName` and serialized `Result`.
- **LogEntry** — Serilog-structured log in PostgreSQL. Columns: `level`, `message`, `endpoint`, `correlation_id`, `user_id`, `role`, `http_method`.

## 3. Strict Business Rules

*Rules the code must never break under any circumstances.*

1. **One primary doctor per patient.** Enforced at `AcceptLinkRequestCommand`: checks `patient.PrimaryDoctorId` is null before linking.
2. **Doctor cédula verification.** `RegisterDoctorCommand` calls `ICedulaVerificationService` against Mexico's SEP registry. Name cross-checked with Unicode normalization (NFD diacritic stripping).
3. **No exceptions for business logic.** All handlers return `ErrorOr<T>`. Domain errors in `Application/Common/Errors/`.
4. **Glucose range: 20-600 mg/dL.** Enforced in `AddDailyRecordCommandValidator` and `AddInsulinRecordCommandValidator`.
5. **Password policy: min 12 chars, uppercase + digit + special character.** Enforced in `RegisterPatientCommandValidator` and `RegisterDoctorCommandValidator`.
6. **Login anti-enumeration.** Login never distinguishes "email not found" from "wrong password". Forgot-password always returns success.
7. **Login lockout.** 5 failed attempts = 15-minute block (`ILoginAttemptTracker`).
8. **Refresh token rotation.** Every `/auth/refresh` revokes the old token and issues a new pair.
9. **Goal evaluation is an immutable snapshot.** `GoalEvaluationItem.GoalUsed` captures the threshold at evaluation time; historical evaluations are never recalculated.
10. **ADA clinical constants.** HbA1c < 7.0%, fasting glucose 80-130 mg/dL, systolic BP < 130 mmHg, LDL < 100 mg/dL, BMI 18.5-24.9. Values at ≥90% of goal flagged `AtRisk`.
11. **JWT delivered via HttpOnly cookies.** Never in response body (except during Postman testing phase). `SameSite=None, Secure, HttpOnly`.
12. **Google OAuth users cannot login with password.** Enforced by `string.IsNullOrEmpty(user.PasswordHash)` check in `LoginCommandHandler`.
13. **Idempotency on mutations.** `Admission.IdempotencyKey` enables safe retries without duplicate admissions.

## 4. Architecture & Stack

- **Style:** Clean Architecture + CQRS via MediatR. Dependencies point inward.
- **API Framework:** ASP.NET Core 9, Minimal APIs via Carter modules. All routes under `/api/v1/`.
- **ORM:** EF Core 9 (code-first). `IEntityTypeConfiguration<T>` for Fluent API. No tracking by default.
- **Database:** PostgreSQL (Neon serverless in alpha → self-hosted VPS for production).
- **Auth:** JWT (HS256, 15-min access + 7-day refresh) via HttpOnly cookies. Google OAuth 2.0 SSO.
- **Validation:** FluentValidation pipeline behavior. Validators auto-registered from assembly.
- **Error Handling:** `ErrorOr<T>` pattern. Domain errors as static classes.
- **Mapping:** Mapster (`IMapper` from `Mapster.DependencyInjection`).
- **Email:** Brevo (Sendinblue) for password reset.
- **Logging:** Serilog → PostgreSQL (`Logs` table, managed by Serilog, excluded from EF migrations).
- **API Docs:** Scalar (replaces Swagger UI).
- **Compression:** Brotli + Gzip response compression.
- **Rate Limiting:** Login 10/min, Register 5/min (per IP).
- **Tracing:** Correlation ID middleware on every request.
- **Health:** `/api/health` endpoint with database health check.

## 5. Project Reference Matrix

| Project        | Direct References          | Rules |
|----------------|----------------------------|-------|
| Domain         | —                          | No framework references. Entities, enums, value objects only. |
| Contracts      | — *(Domain when needed)*   | Pure data shapes. No behavior. |
| Application    | Domain, Contracts          | Defines interfaces. No EF Core, no HTTP, no I/O. |
| Infrastructure | Application                | Only place for EF Core, HttpClient, external SDKs. |
| API            | Infrastructure, Contracts  | Thin composition root. Delegates to MediatR. |

## 6. Directory Structure

- `src/Domain/` — Entities (`Models/`), enums (`Enums/`). Zero external dependencies.
- `src/Contracts/` — DTOs, request/response records, JSON converters.
- `src/Application/` — Use cases organized by feature (`Auth/`, `Doctor/`, `Patient/`, `DailyRecord/`, `LinkRequest/`, `InsulinDm1/`, `LabResult/`, `Goals/`, `Admin/`). Each has `Commands/`, `Handlers/`, `Validators/`, `Queries/`, `Common/` subdirectories. Interfaces in `Common/Interfaces/Persistence/`, `Services/`, `Security/`.
- `src/Infrastructure/` — `Persistence/` (EF Core DbContext, configurations, repositories), `Security/` (JWT, password hasher), `Services/` (Brevo email, Google OAuth, cédula scraper, login tracker), `DependencyInjection.cs` (all DI registration).
- `src/API/` — `Program.cs`, `Modules/` (Carter endpoint groups), `Extensions/` (middleware setup, CORS, rate limiting, Swagger/Scalar).
- `docs/brain/` — ADRs, bugs, learnings, planning documents (Obsidian-style knowledge base).
- `tests/` — Unit tests (xUnit + NSubstitute + FluentAssertions) and integration tests (WebApplicationFactory + Testcontainers).

## 7. Decisions and Documentation

- Architectural decisions and sprint-level ADRs are documented in `docs/brain/ADR/`.
- For bugs, learnings, and planning documents, see `docs/brain/Index.md`.
- The project README (`README.md`) covers tech stack, setup, and key technical decisions.
- Full API endpoint reference lives in `docs/api-guidelines.md` (39 endpoints).

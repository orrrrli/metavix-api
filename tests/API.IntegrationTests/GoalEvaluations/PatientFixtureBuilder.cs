using Domain.Enums;
using Domain.Models;

namespace API.IntegrationTests.GoalEvaluations;

// Builds the four patient archetypes QA-METAS-1's acceptance criteria require, plus the
// lower-level seeds they sit on top of. One class so T3 can call these from its own
// assertion files without re-pasting the seed code.
//
// Every value below is picked from inside its band (strict inequalities on the high
// side, where the catalog uses inclusive high edges) so ParameterSpec.Classify can't
// accidentally place a value on a band edge. The handler's existing 16-RF catalog-drift
// grid uses the same convention.
//
// Dates are pinned to "today" so NoDataWindow (7d BP / 14d fasting / 30d BMI /
// 90d HbA1c / 365d LDL) is satisfied regardless of when CI runs the test.
// Integration tests can't inject a FakeTimeProvider without a WebApplicationFactory
// rewrite, so we use the real wall clock and seed records dated the same day as the
// engine's "now" — every value is fresh by construction.
internal static class PatientFixtureBuilder
{
    internal static readonly DateOnly FixedEvaluationDate = DateOnly.FromDateTime(DateTime.UtcNow);

    // Scenario 1 — Type2 (ConDiabetes) patient with all 16 evaluated parameters InRange.
    //
    // ConDiabetes is required, not SinDiabetes, because the postprandial catalog rows
    // only exist for ConDiabetes / EmbarazadaDMG. A SinDiabetes patient would have
    // postprandial_1h / postprandial_2h omitted from the result and fail the Story's
    // "all 16 items return Status=InRange" criterion. HasAscvd=false so LDL resolves
    // to ldl_primary, the id the Story's 16-item count assumes.
    internal static async Task<(Guid UserId, Guid PatientId)> SeedConDiabetesInRangePatientAsync(
        CustomWebApplicationFactory factory)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        await using var db = CreateDbContext(factory);

        db.Users.Add(NewUser(userId));
        db.Patients.Add(new Patient
        {
            Id                  = patientId,
            UserId              = userId,
            FirstName           = "InRange",
            LastName            = "Patient",
            MedicalRecordNumber = Guid.NewGuid().ToString("N")[..8],
            DateOfBirth         = new DateOnly(1990, 1, 1),
            HeightCm            = 170m,
            Gender              = Gender.Female,         // resolves gender-keyed specs (HDL, Cr, waist)
            IsPregnant          = false,
            HasAscvd            = false,                // → ldl_primary
            DiabetesType        = DiabetesType.Type2,    // → ConDiabetes (gives postprandial rows)
            CreatedAt           = DateTime.UtcNow,
        });

        db.LabResults.Add(new LabResult
        {
            Id                = Guid.NewGuid(),
            PatientId         = patientId,
            SampleDate        = FixedEvaluationDate,
            // Lab: every value inside its ConDiabetes / Universal / Female InRange band.
            Hba1c             = 5.0m,    // ConDiabetes: InRange < 7.0
            Ldl               = 60m,     // ldl_primary ConDiabetes: InRange < 70
            Hdl               = 60m,     // Female Universal: InRange ≥ 50
            TotalCholesterol  = 180m,    // Universal: InRange < 200
            Triglycerides     = 100m,    // Universal: InRange < 150
            Creatinine        = 1.0m,    // Female Universal: 0.5 ≤ 1.0 < 1.2 → InRange
            Bun               = 15m,     // Universal: 7 ≤ 15 < 21 → InRange
            CreatedAt         = DateTime.UtcNow,
        });

        var recordId = Guid.NewGuid();
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = FixedEvaluationDate,
            // Vitals: SBP / DBP in SinDiabetes InRange (< 120 / < 80); HR 70 is Universal InRange
            // (60–100). Weight 60 kg @ 170 cm → BMI 20.76, Universal InRange (18.5–24.9).
            // Waist 70 is Female Universal InRange (< 80).
            SystolicPressure = 110,
            DiastolicPressure = 70,
            HeartRate        = 70,
            WeightKg         = 60m,
            WaistCm          = 70,
            CreatedAt        = DateTime.UtcNow,
        });

        // Glucose readings: one fasting, one 1h postprandial, one 2h postprandial, all
        // with the window marker the handler needs to route them to postprandial_1h/2h.
        db.GlucoseReadings.AddRange(
            new GlucoseReading
            {
                Id                 = Guid.NewGuid(),
                DailyRecordId      = recordId,
                ReadingType        = GlucoseReadingType.Fasting,
                ValueMgDl          = 90,                 // ConDiabetes fasting: 70 ≤ 90 < 131 → InRange
            },
            new GlucoseReading
            {
                Id                 = Guid.NewGuid(),
                DailyRecordId      = recordId,
                ReadingType        = GlucoseReadingType.PostBreakfast,
                ValueMgDl          = 150,                // ConDiabetes postprandial: InRange < 180
                PostprandialWindow = PostprandialWindow.OneHour,
            },
            new GlucoseReading
            {
                Id                 = Guid.NewGuid(),
                DailyRecordId      = recordId,
                ReadingType        = GlucoseReadingType.PostBreakfast,
                ValueMgDl          = 150,                // same threshold as 1h
                PostprandialWindow = PostprandialWindow.TwoHour,
            });

        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    // Scenario 2 — SinDiabetes patient with every parameter pushed to OutOfRange.
    //
    // Picked to lock in the high-side boundary for every parameter that has one. Where
    // a band is "lower-is-worse" (HDL, creatinine, eGFR, BUN lower bound), the value
    // is set below the OutOfRangeLow edge instead. Boundary picks use *strict* values
    // above the AtRisk/OutOfRange edge to avoid sitting on a band edge — same rule as
    // the handler test grid.
    internal static async Task<(Guid UserId, Guid PatientId)> SeedSinDiabetesOutOfRangePatientAsync(
        CustomWebApplicationFactory factory)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        await using var db = CreateDbContext(factory);

        db.Users.Add(NewUser(userId));
        db.Patients.Add(new Patient
        {
            Id                  = patientId,
            UserId              = userId,
            FirstName           = "OutOfRange",
            LastName            = "Patient",
            MedicalRecordNumber = Guid.NewGuid().ToString("N")[..8],
            DateOfBirth         = new DateOnly(1990, 1, 1),
            HeightCm            = 170m,
            Gender              = Gender.Female,
            IsPregnant          = false,
            HasAscvd            = false,                // → ldl_primary SinDiabetes
            DiabetesType        = DiabetesType.None,    // → SinDiabetes
            CreatedAt           = DateTime.UtcNow,
        });

        db.LabResults.Add(new LabResult
        {
            Id                = Guid.NewGuid(),
            PatientId         = patientId,
            SampleDate        = FixedEvaluationDate,
            // SinDiabetes bands: HbA1c OutOfRange ≥ 6.5; ldl_primary OutOfRange ≥ 160; HDL
            // Female OutOfRange < 40; TC OutOfRange ≥ 240; TG OutOfRange ≥ 500; creatinine
            // Female OutOfRange > 1.4; BUN OutOfRange > 40.
            Hba1c             = 7.0m,
            Ldl               = 170m,
            Hdl               = 35m,                   // Female OutOfRangeLow=40
            TotalCholesterol  = 250m,
            Triglycerides     = 550m,                  // pancreatitis threshold
            Creatinine        = 1.5m,                  // Female OutOfRange > 1.4 → eGFR drops into CKD
            Bun               = 45m,                   // OutOfRange > 40
            CreatedAt         = DateTime.UtcNow,
        });

        var recordId = Guid.NewGuid();
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = FixedEvaluationDate,
            // SinDiabetes BP: SBP OutOfRange ≥ 130, DBP OutOfRange ≥ 90. HR Universal
            // OutOfRange > 110. Weight → BMI 30+ → Universal OutOfRange. Waist Female
            // OutOfRange > 88.
            SystolicPressure = 135,
            DiastolicPressure = 95,
            HeartRate        = 120,
            WeightKg         = 95m,                    // 95 / 1.7² ≈ 32.9 → OutOfRange
            WaistCm          = 90,
            CreatedAt        = DateTime.UtcNow,
        });

        db.GlucoseReadings.AddRange(
            new GlucoseReading
            {
                Id            = Guid.NewGuid(),
                DailyRecordId = recordId,
                ReadingType   = GlucoseReadingType.Fasting,
                ValueMgDl     = 130,                    // SinDiabetes fasting OutOfRange ≥ 126
            });

        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    // Scenario 3 — mixed SinDiabetes / ConDiabetes patient exercising a category switch.
    //
    // HbA1c and LDL move into the stricter ConDiabetes bands (HbA1c 7.5 → OutOfRange
    // against ConDiabetes 8.0 but AtRisk against SinDiabetes 6.5; LDL 90 → AtRisk
    // against ldl_primary ConDiabetes 70/100 but InRange against SinDiabetes 130).
    // The rest of the parameters stay in SinDiabetes InRange so the test can assert
    // the category resolution, not just "everything is OK".
    internal static async Task<(Guid UserId, Guid PatientId)> SeedMixedCategoryPatientAsync(
        CustomWebApplicationFactory factory)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        await using var db = CreateDbContext(factory);

        db.Users.Add(NewUser(userId));
        db.Patients.Add(new Patient
        {
            Id                  = patientId,
            UserId              = userId,
            FirstName           = "Mixed",
            LastName            = "Patient",
            MedicalRecordNumber = Guid.NewGuid().ToString("N")[..8],
            DateOfBirth         = new DateOnly(1990, 1, 1),
            HeightCm            = 170m,
            Gender              = Gender.Female,
            IsPregnant          = false,
            HasAscvd            = false,
            DiabetesType        = DiabetesType.Type2,    // → ConDiabetes
            CreatedAt           = DateTime.UtcNow,
        });

        db.LabResults.Add(new LabResult
        {
            Id                = Guid.NewGuid(),
            PatientId         = patientId,
            SampleDate        = FixedEvaluationDate,
            // HbA1c 7.5: ConDiabetes OutOfRangeHigh=8.0 → AtRisk band [7.0, 8.0).
            // LDL 90: ldl_primary ConDiabetes AtRiskHigh=70 → AtRisk band [70, 100).
            Hba1c             = 7.5m,
            Ldl               = 90m,
            Hdl               = 60m,                    // Universal Female: InRange
            TotalCholesterol  = 180m,
            Triglycerides     = 100m,
            Creatinine        = 1.0m,
            Bun               = 15m,
            CreatedAt         = DateTime.UtcNow,
        });

        var recordId = Guid.NewGuid();
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = FixedEvaluationDate,
            // ConDiabetes SBP / DBP: InRange < 130 / < 80. HR / weight / waist InRange.
            SystolicPressure = 125,
            DiastolicPressure = 75,
            HeartRate        = 70,
            WeightKg         = 60m,
            WaistCm          = 70,
            CreatedAt        = DateTime.UtcNow,
        });

        db.GlucoseReadings.Add(new GlucoseReading
        {
            Id            = Guid.NewGuid(),
            DailyRecordId = recordId,
            ReadingType   = GlucoseReadingType.Fasting,
            ValueMgDl     = 135,                        // ConDiabetes fasting AtRisk band [131, 180)
        });

        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    // Scenario 4 — pregnant Type2 patient (EmbarazadaDM). The Story's third acceptance
    // criterion asks for: (a) pregnancy-specific thresholds apply, and (b) NoData for
    // parameters with AppliesInPregnancy=false. This fixture hits both shapes:
    //
    //   - HbA1c 6.5 → EmbarazadaDM band [6.0, 7.0) → AtRisk (pregnancy spec applied).
    //   - LDL 85  → EmbarazadaDM ldl_primary [70, 100) → AtRisk (pregnancy spec applied).
    //   - SBP 128 → no EmbarazadaDM row → NoData "requires-specialist-evaluation".
    //   - BMI 24.2 (W=70, H=170) → Universal spec, AppliesInPregnancy=false
    //     → NoData "not-evaluated-in-pregnancy".
    //   - postprandial_1h 145 → no EmbarazadaDMG row for non-gestational
    //     → NoData "requires-specialist-evaluation".
    internal static async Task<(Guid UserId, Guid PatientId)> SeedPregnantEmbarazadaDMPatientAsync(
        CustomWebApplicationFactory factory)
    {
        var userId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        await using var db = CreateDbContext(factory);

        db.Users.Add(NewUser(userId));
        db.Patients.Add(new Patient
        {
            Id                  = patientId,
            UserId              = userId,
            FirstName           = "Pregnant",
            LastName            = "Patient",
            MedicalRecordNumber = Guid.NewGuid().ToString("N")[..8],
            DateOfBirth         = new DateOnly(1990, 1, 1),
            HeightCm            = 170m,
            Gender              = Gender.Female,
            IsPregnant          = true,
            HasAscvd            = false,
            DiabetesType        = DiabetesType.Type2,    // pre-existing, not gestational
            CreatedAt           = DateTime.UtcNow,
        });

        db.LabResults.Add(new LabResult
        {
            Id                = Guid.NewGuid(),
            PatientId         = patientId,
            SampleDate        = FixedEvaluationDate,
            Hba1c             = 6.5m,                    // EmbarazadaDM [6.0, 7.0) → AtRisk
            Ldl               = 85m,                    // EmbarazadaDM ldl_primary [70, 100) → AtRisk
            Hdl               = 60m,                    // Universal Female: InRange (still evaluated in pregnancy)
            TotalCholesterol  = 180m,
            Triglycerides     = 100m,
            Creatinine        = 1.0m,                   // Universal Female InRange
            Bun               = 15m,                    // Universal InRange
            CreatedAt         = DateTime.UtcNow,
        });

        var recordId = Guid.NewGuid();
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = FixedEvaluationDate,
            SystolicPressure = 128,                     // no EmbarazadaDM spec → NoData "requires-specialist-evaluation"
            DiastolicPressure = 80,
            HeartRate        = 70,
            WeightKg         = 70m,                     // 70 / 1.7² ≈ 24.2 → Universal spec exists but AppliesInPregnancy=false → NoData "not-evaluated-in-pregnancy"
            WaistCm          = 75,
            CreatedAt        = DateTime.UtcNow,
        });

        db.GlucoseReadings.Add(new GlucoseReading
        {
            Id                 = Guid.NewGuid(),
            DailyRecordId      = recordId,
            ReadingType        = GlucoseReadingType.PostBreakfast,
            ValueMgDl          = 145,
            PostprandialWindow = PostprandialWindow.OneHour,   // EmbarazadaDMG is gestational-only → NoData
        });

        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    // --- Low-level helpers shared by the four scenarios above ---

    private static User NewUser(Guid userId) => new()
    {
        Id           = userId,
        Email        = $"{userId}@test.com",
        PasswordHash = "not-used",
        Role         = UserRole.Patient,
        CreatedAt    = DateTime.UtcNow,
    };

    private static AppDbContext CreateDbContext(CustomWebApplicationFactory factory) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(factory.ConnectionString)
            .Options);
}

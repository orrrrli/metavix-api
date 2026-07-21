using System.Net.Http.Json;

namespace API.IntegrationTests.GoalEvaluations;

public class GoalEvaluationsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public GoalEvaluationsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // T3: seeded patient with full health data → 201 + all InRange statuses
    [Fact]
    public async Task PostGoalEvaluations_WhenPatientHasFullHealthData_Returns201WithInRangeStatuses()
    {
        // Arrange
        var (userId, patientId) = await SeedPatientAsync(heightCm: 170m);
        await SeedHealthDataAsync(patientId);

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Items.Should().HaveCount(5);
        body.Data.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(GoalStatus.InRange));
    }

    // Story AC1: Type2 patient with all 16 evaluated parameters in their InRange band → 201
    // + every item Status=InRange. ConDiabetes is required (not SinDiabetes) so the
    // postprandial catalog rows resolve — see PatientFixtureBuilder for the full rationale.
    [Fact]
    public async Task PostGoalEvaluations_WhenConDiabetesPatientHasAll16InRange_Returns201WithAllInRange()
    {
        // Arrange
        var (userId, patientId) = await PatientFixtureBuilder.SeedConDiabetesInRangePatientAsync(_factory);
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Items.Should().HaveCount(AdaGoalConstants.EvaluatedParameterIds.Count);
        body.Data.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(GoalStatus.InRange));
    }

    // Story AC1.1 (eGFR CkdStage wire format): for the ConDiabetes InRange fixture, creatinine
    // is 1.0 mg/dL on a female adult, which the calculator maps to an eGFR in the G1-G2 band.
    // The wire assertion pins that CkdStage flows through the JSON response shape unchanged
    // (camelCase, nullable string) and is non-null for eGFR items with a value.
    [Fact]
    public async Task PostGoalEvaluations_WhenEgfrIsComputed_CkdStageIsPopulatedOnWire()
    {
        // Arrange
        var (userId, patientId) = await PatientFixtureBuilder.SeedConDiabetesInRangePatientAsync(_factory);
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();

        var egfrItem = body!.Data.Items.First(i => i.ParameterId == AdaGoalConstants.Egfr);
        egfrItem.ValueUsed.Should().NotBeNull();
        // Female, ~age 50, creatinine 1.0 → eGFR well above 60 → G2.
        egfrItem.CkdStage.Should().Be(AdaGoalConstants.CkdStageG2);

        // Other items never carry a CkdStage — it's an eGFR-only field.
        var nonEgfrItems = body.Data.Items.Where(i => i.ParameterId != AdaGoalConstants.Egfr);
        nonEgfrItems.Should().AllSatisfy(item => item.CkdStage.Should().BeNull());
    }

    // Story AC2 (high-edge boundary): SinDiabetes patient with every parameter pushed to
    // OutOfRange → 201 + per-parameter OutOfRange. Postprandial 1h/2h are omitted for a
    // non-pregnant patient because the catalog has no SinDiabetes row for them; the
    // assertion counts 14 items, not 16, and matches them by ParameterId rather than index.
    [Fact]
    public async Task PostGoalEvaluations_WhenSinDiabetesPatientHasAllParamsOutOfRange_Returns201WithPerParameterOutOfRange()
    {
        // Arrange
        var (userId, patientId) = await PatientFixtureBuilder.SeedSinDiabetesOutOfRangePatientAsync(_factory);
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();

        // 14 items, not 16: SinDiabetes has no postprandial catalog row, so postprandial_1h
        // and postprandial_2h are dropped by BuildItem's non-pregnant "omit" branch.
        body!.Data.Items.Should().HaveCount(14);

        // One assertion per parameter the catalog actually evaluates for a SinDiabetes patient,
        // driven by ParameterId so a future reorder / insert can't silently break the test.
        GoalStatus StatusOf(string id) => body.Data.Items.First(i => i.ParameterId == id).Status;
        StatusOf(AdaGoalConstants.HbA1c).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.SystolicBp).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.DiastolicBp).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.HeartRate).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.LdlPrimary).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Bmi).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Hdl).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.TotalCholesterol).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Triglycerides).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Creatinine).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Egfr).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.Bun).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.WaistCircumference).Should().Be(GoalStatus.OutOfRange);
        StatusOf(AdaGoalConstants.FastingGlucose).Should().Be(GoalStatus.OutOfRange);
    }

    // Story AC2 (category switch): Type2 patient whose HbA1c and LDL fall into the stricter
    // ConDiabetes AtRisk bands while everything else stays InRange. Locks in that the
    // HTTP layer preserves the same category resolution the handler unit tests cover.
    [Fact]
    public async Task PostGoalEvaluations_WhenMixedCategoryPatient_HitsConDiabetesAtRiskBands()
    {
        // Arrange
        var (userId, patientId) = await PatientFixtureBuilder.SeedMixedCategoryPatientAsync(_factory);
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();

        // ConDiabetes resolves postprandial rows; everything that has a value is present.
        body!.Data.Items.Should().HaveCount(AdaGoalConstants.EvaluatedParameterIds.Count);

        GoalStatus StatusOf(string id) => body.Data.Items.First(i => i.ParameterId == id).Status;

        // The two boundary-picks the fixture was designed for — both fall into the
        // ConDiabetes AtRisk band (HbA1c [7.0, 8.0), ldl_primary [70, 100)).
        StatusOf(AdaGoalConstants.HbA1c).Should().Be(GoalStatus.AtRisk);
        StatusOf(AdaGoalConstants.LdlPrimary).Should().Be(GoalStatus.AtRisk);

        // Fasting glucose 100 sits in the ConDiabetes AtRisk band [80, 131) as well.
        StatusOf(AdaGoalConstants.FastingGlucose).Should().Be(GoalStatus.AtRisk);

        // Everything else is in-range.
        StatusOf(AdaGoalConstants.SystolicBp).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.DiastolicBp).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.HeartRate).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.Bmi).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.Hdl).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.TotalCholesterol).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.Triglycerides).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.Creatinine).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.Egfr).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.Bun).Should().Be(GoalStatus.InRange);
        StatusOf(AdaGoalConstants.WaistCircumference).Should().Be(GoalStatus.InRange);
        // postprandial 1h/2h are absent from the fixture (no readings seeded) → NoData.
        StatusOf(AdaGoalConstants.Postprandial1h).Should().Be(GoalStatus.NoData);
        StatusOf(AdaGoalConstants.Postprandial2h).Should().Be(GoalStatus.NoData);
    }

    // Story AC3: pregnant Type2 (EmbarazadaDM) patient. Two acceptance shapes in one fixture:
    //   1. Pregnancy-specific thresholds apply — HbA1c and LDL hit the EmbarazadaDM AtRisk bands.
    //   2. NoData for parameters with AppliesInPregnancy=false (or no spec) — SBP and postprandial
    //      1h get "requires-specialist-evaluation", BMI gets "not-evaluated-in-pregnancy".
    [Fact]
    public async Task PostGoalEvaluations_WhenPregnantEmbarazadaDMPatient_AppliesPregnancySpecAndEmitsNoDataForExcludedParams()
    {
        // Arrange
        var (userId, patientId) = await PatientFixtureBuilder.SeedPregnantEmbarazadaDMPatientAsync(_factory);
        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();

        // 15 items: every EvaluatedParameterId except ldl_secondary (HasAscvd=false → ldl_primary).
        body!.Data.Items.Should().HaveCount(AdaGoalConstants.EvaluatedParameterIds.Count - 1);

        GoalEvaluationItemResult ItemOf(string id) => body.Data.Items.First(i => i.ParameterId == id);

        // 1. Pregnancy-specific thresholds apply.
        // HbA1c 6.5 → EmbarazadaDM band [6.0, 7.0) → AtRisk.
        var hba1c = ItemOf(AdaGoalConstants.HbA1c);
        hba1c.Status.Should().Be(GoalStatus.AtRisk);
        hba1c.ThresholdUsed.Should().Be(6.0m);

        // LDL 85 → EmbarazadaDM ldl_primary band [70, 100) → AtRisk.
        var ldl = ItemOf(AdaGoalConstants.LdlPrimary);
        ldl.Status.Should().Be(GoalStatus.AtRisk);
        ldl.ThresholdUsed.Should().Be(70m);

        // 2. NoData reasons for parameters pregnancy excludes.
        // SBP: no EmbarazadaDM spec → "requires-specialist-evaluation".
        var sbp = ItemOf(AdaGoalConstants.SystolicBp);
        sbp.Status.Should().Be(GoalStatus.NoData);
        sbp.Reason.Should().Be(AdaGoalConstants.RequiresSpecialistEvaluationReason);

        // BMI: Universal spec exists but AppliesInPregnancy=false → "not-evaluated-in-pregnancy".
        var bmi = ItemOf(AdaGoalConstants.Bmi);
        bmi.Status.Should().Be(GoalStatus.NoData);
        bmi.Reason.Should().Be(AdaGoalConstants.NotEvaluatedInPregnancyReason);

        // postprandial_1h: no EmbarazadaDMG row for non-gestational diabetes → specialist.
        var post1h = ItemOf(AdaGoalConstants.Postprandial1h);
        post1h.Status.Should().Be(GoalStatus.NoData);
        post1h.Reason.Should().Be(AdaGoalConstants.RequiresSpecialistEvaluationReason);

        // postprandial_2h: no reading seeded and no EmbarazadaDM spec → same
        // "requires-specialist-evaluation" path as SBP (BuildItem line ~248).
        var post2h = ItemOf(AdaGoalConstants.Postprandial2h);
        post2h.Status.Should().Be(GoalStatus.NoData);
        post2h.Reason.Should().Be(AdaGoalConstants.RequiresSpecialistEvaluationReason);

        // Pregnancy-agnostic parameters still evaluate against the catalog and stay InRange.
        ItemOf(AdaGoalConstants.DiastolicBp).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.HeartRate).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.Hdl).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.TotalCholesterol).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.Triglycerides).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.Creatinine).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.Egfr).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.Bun).Status.Should().Be(GoalStatus.InRange);
        ItemOf(AdaGoalConstants.WaistCircumference).Status.Should().Be(GoalStatus.InRange);
    }

    // T4: patient with no health records → 201 + all NoData statuses
    [Fact]
    public async Task PostGoalEvaluations_WhenPatientHasNoHealthRecords_Returns201WithAllNoData()
    {
        // Arrange
        var (userId, patientId) = await SeedPatientAsync(heightCm: null);

        var client = CreateAuthenticatedClient(userId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Items.Should().HaveCount(5);
        body.Data.Items.Should().AllSatisfy(item =>
            item.Status.Should().Be(GoalStatus.NoData));
    }

    // T5: JWT belongs to a different patient → 403 Forbidden
    [Fact]
    public async Task PostGoalEvaluations_WhenCallerIsNotThePatient_Returns403()
    {
        // Arrange
        var (_, targetPatientId) = await SeedPatientAsync(heightCm: null);
        var (otherUserId, _)     = await SeedPatientAsync(heightCm: null);

        var client = CreateAuthenticatedClient(otherUserId);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{targetPatientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // SEC-METAS-1-T1a: no JWT provided → 401 Unauthorized
    // The [Authorize] policy on the patient group rejects anonymous requests before the handler runs.
    [Fact]
    public async Task PostGoalEvaluations_WhenNoJwtIsProvided_Returns401()
    {
        // Arrange
        var (_, patientId) = await SeedPatientAsync(heightCm: null);

        var client = _factory.CreateClient(); // No Authorization header

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // SEC-METAS-1-T1b: caller is a doctor with no link to the patient → 403 Forbidden
    // The role check on the patient group rejects Doctor-role tokens before the handler runs,
    // regardless of any link state.
    [Fact]
    public async Task PostGoalEvaluations_WhenCallerIsADoctorWithoutLink_Returns403()
    {
        // Arrange
        var (_, patientId) = await SeedPatientAsync(heightCm: null);
        var (doctorUserId, _) = await SeedDoctorAsync();

        var client = CreateAuthenticatedClient(doctorUserId, role: "Doctor");

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // SEC-METAS-1-T1c: caller is a doctor WITH an accepted link to the patient → 403 Forbidden
    // The role check fires even when the doctor is the patient's primary doctor. Evaluation is a
    // patient-initiated event (EvaluationTrigger.Patient); a doctor must not be able to trigger it
    // on the patient's behalf through this route, even with full record access.
    [Fact]
    public async Task PostGoalEvaluations_WhenCallerIsADoctorWithAcceptedLink_Returns403()
    {
        // Arrange
        var (_, patientId) = await SeedPatientAsync(heightCm: null);
        var (doctorUserId, doctorId) = await SeedDoctorAsync();
        await SeedAcceptedLinkAsync(patientId, doctorId);

        var client = CreateAuthenticatedClient(doctorUserId, role: "Doctor");

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientId}/goal-evaluations", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // SEC-METAS-1-T2: persisted GoalEvaluation.PatientId matches the route, not the JWT.
    // Asserts the data-isolation property directly: after Patient A triggers an evaluation on
    // /api/v1/patient/{A}/goal-evaluations, the row written to the DB must belong to A, and the
    // items must reflect A's measurements, not any other patient's. Pinned to a fixed evaluation
    // date (2026-06-21) so NoDataWindow checks are deterministic across CI runs.
    [Fact]
    public async Task PostGoalEvaluations_PersistsGoalEvaluationWithRoutePatientId()
    {
        // Arrange — pinned evaluation date (matches the deterministic anchor used in
        // feat/QA-METAS-1 so NoDataWindow-driven outcomes stay stable).
        var evaluationDate = new DateOnly(2026, 6, 21);
        var labDate        = evaluationDate;          // sample taken on the evaluation day
        var recordDate     = evaluationDate;

        // Patient A: healthy numbers (InRange)
        var (userIdA, patientIdA) = await SeedPatientWithFixedHeightAsync(heightCm: 170m);
        var labIdA = await SeedLabResultAsync(patientIdA, labDate, hba1c: 5.5m, ldl: 80m);
        var recordIdA = await SeedDailyRecordWithFastingAsync(
            patientIdA, recordDate, sbp: 110, weightKg: 60m, fastingGlucose: 100);

        // Patient B: deliberately out-of-range numbers — if the handler ever bled data across
        // patients, the persisted GoalEvaluation would carry B's values under A's PatientId.
        var (userIdB, patientIdB) = await SeedPatientWithFixedHeightAsync(heightCm: 170m);
        await SeedLabResultAsync(patientIdB, labDate, hba1c: 12.0m, ldl: 200m);
        await SeedDailyRecordWithFastingAsync(
            patientIdB, recordDate, sbp: 180, weightKg: 100m, fastingGlucose: 300);

        var client = CreateAuthenticatedClient(userIdA);

        // Act
        var response = await client.PostAsync($"/api/v1/patient/{patientIdA}/goal-evaluations", null);

        // Assert — HTTP layer
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<EvaluateGoalsResult>>(JsonOptions);
        body.Should().NotBeNull();

        // Response items reflect A's values (Hba1c 5.5 → InRange, not B's 12.0 → OutOfRange).
        body!.Data.Items.Should().Contain(i =>
            i.ParameterId == AdaGoalConstants.HbA1c && i.ValueUsed == 5.5m);
        body.Data.Items.Should().NotContain(i =>
            i.ParameterId == AdaGoalConstants.HbA1c && i.ValueUsed == 12.0m);

        // Assert — persistence layer (the real data-isolation proof).
        await using var db = CreateDbContext();
        var persisted = await db.GoalEvaluations
            .Include(e => e.Items)
            .SingleAsync(e => e.Id == body.Data.EvaluationId);

        persisted.PatientId.Should().Be(patientIdA, "the route's PatientId, not the JWT, is the scope");
        persisted.PatientId.Should().NotBe(patientIdB);

        // The persisted evaluation links back to A's measurements through its items' ValueUsed;
        // if the handler ever substituted B's data, ValueUsed would carry 12.0/200/300, not A's.
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.HbA1c    && i.ValueUsed == 5.5m);
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.LdlPrimary && i.ValueUsed == 80m);
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.SystolicBp && i.ValueUsed == 110m);
        persisted.Items.Should().Contain(i => i.ParameterId == AdaGoalConstants.FastingGlucose && i.ValueUsed == 100m);
    }

    // --- Helpers ---

    private async Task<(Guid UserId, Guid PatientId)> SeedPatientAsync(decimal? heightCm)
    {
        var userId    = Guid.NewGuid();
        var patientId = Guid.NewGuid();

        await using var db = CreateDbContext();

        db.Users.Add(new User
        {
            Id           = userId,
            Email        = $"{userId}@test.com",
            PasswordHash = "not-used",
            Role         = UserRole.Patient,
            CreatedAt    = DateTime.UtcNow,
        });

        db.Patients.Add(new Patient
        {
            Id                  = patientId,
            UserId              = userId,
            FirstName           = "Test",
            LastName            = "Patient",
            MedicalRecordNumber = Guid.NewGuid().ToString("N")[..8],
            DateOfBirth         = new DateOnly(1990, 1, 1),
            HeightCm            = heightCm,
            CreatedAt           = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    private async Task SeedHealthDataAsync(Guid patientId)
    {
        await using var db = CreateDbContext();

        // HbA1c = 5.5 (InRange: < 7.0 * 0.9 = 6.3), LDL = 80 (InRange: < 100 * 0.9 = 90)
        db.LabResults.Add(new LabResult
        {
            Id          = Guid.NewGuid(),
            PatientId   = patientId,
            SampleDate  = DateOnly.FromDateTime(DateTime.UtcNow),
            Hba1c       = 5.5m,
            Ldl         = 80m,
            CreatedAt   = DateTime.UtcNow,
        });

        var recordId = Guid.NewGuid();

        // SBP = 110 (InRange: < 130 * 0.9 = 117), Weight 60 kg → BMI 20.76 (InRange: < 24.9 * 0.9 = 22.41)
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = DateOnly.FromDateTime(DateTime.UtcNow),
            SystolicPressure = 110,
            WeightKg         = 60m,
            CreatedAt        = DateTime.UtcNow,
        });

        // Fasting glucose = 100 (InRange: 80 ≤ 100 < 130 * 0.9 = 117)
        db.GlucoseReadings.Add(new GlucoseReading
        {
            Id            = Guid.NewGuid(),
            DailyRecordId = recordId,
            ReadingType   = GlucoseReadingType.Fasting,
            ValueMgDl     = 100,
        });

        await db.SaveChangesAsync();
    }

    private async Task<(Guid UserId, Guid DoctorId)> SeedDoctorAsync()
    {
        var userId   = Guid.NewGuid();
        var doctorId = Guid.NewGuid();

        await using var db = CreateDbContext();

        db.Users.Add(new User
        {
            Id           = userId,
            Email        = $"{userId}@test.com",
            PasswordHash = "not-used",
            Role         = UserRole.Doctor,
            CreatedAt    = DateTime.UtcNow,
        });

        db.Doctors.Add(new Doctor
        {
            Id                 = doctorId,
            UserId             = userId,
            FirstName          = "Test",
            PaternalLastName   = "Doctor",
            MaternalLastName   = "Test",
            LicenseNumber      = Guid.NewGuid().ToString("N")[..8],
            Speciality         = "Endocrinología",
            Email              = $"{doctorId}@test.com",
            CreatedAt          = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        return (userId, doctorId);
    }

    private async Task SeedAcceptedLinkAsync(Guid patientId, Guid doctorId)
    {
        await using var db = CreateDbContext();

        db.PatientDoctorRequests.Add(new PatientDoctorRequest
        {
            Id          = Guid.NewGuid(),
            PatientId   = patientId,
            DoctorId    = doctorId,
            Status      = RequestStatus.Accepted,
            CreatedAt   = DateTime.UtcNow,
            ResolvedAt  = DateTime.UtcNow,
        });

        // Reflect the accepted link in the Patient's PrimaryDoctorId, matching the
        // AcceptLinkRequestCommand invariant.
        var patient = await db.Patients.FindAsync(patientId);
        patient!.PrimaryDoctorId = doctorId;
        patient.UpdatedAt        = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    private async Task<Guid> SeedLabResultAsync(
        Guid patientId, DateOnly sampleDate, decimal hba1c, decimal ldl)
    {
        var id = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.LabResults.Add(new LabResult
        {
            Id         = id,
            PatientId  = patientId,
            SampleDate = sampleDate,
            Hba1c      = hba1c,
            Ldl        = ldl,
            CreatedAt  = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedDailyRecordWithFastingAsync(
        Guid patientId, DateOnly recordDate, int sbp, decimal weightKg, int fastingGlucose)
    {
        var recordId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.DailyRecords.Add(new DailyRecord
        {
            Id               = recordId,
            PatientId        = patientId,
            RecordDate       = recordDate,
            SystolicPressure = sbp,
            WeightKg         = weightKg,
            CreatedAt        = DateTime.UtcNow,
        });
        db.GlucoseReadings.Add(new GlucoseReading
        {
            Id            = Guid.NewGuid(),
            DailyRecordId = recordId,
            ReadingType   = GlucoseReadingType.Fasting,
            ValueMgDl     = fastingGlucose,
        });
        await db.SaveChangesAsync();
        return recordId;
    }

    private async Task<(Guid UserId, Guid PatientId)> SeedPatientWithFixedHeightAsync(decimal? heightCm)
    {
        var userId    = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        await using var db = CreateDbContext();
        db.Users.Add(new User
        {
            Id           = userId,
            Email        = $"{userId}@test.com",
            PasswordHash = "not-used",
            Role         = UserRole.Patient,
            CreatedAt    = DateTime.UtcNow,
        });
        db.Patients.Add(new Patient
        {
            Id                  = patientId,
            UserId              = userId,
            FirstName           = "Test",
            LastName            = "Patient",
            MedicalRecordNumber = Guid.NewGuid().ToString("N")[..8],
            DateOfBirth         = new DateOnly(1990, 1, 1),
            HeightCm            = heightCm,
            CreatedAt           = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        return (userId, patientId);
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_factory.ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
        => CreateAuthenticatedClient(userId, "Patient");

    private HttpClient CreateAuthenticatedClient(Guid userId, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.GenerateToken(userId, role));
        return client;
    }
}
